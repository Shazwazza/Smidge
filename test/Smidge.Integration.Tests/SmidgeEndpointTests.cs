using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Smidge.Integration.Tests
{
    /// <summary>
    /// End to end tests exercising the real Smidge request pipeline over a self-hosted Kestrel server. These mirror
    /// the scenarios that are normally verified by hand with the Smidge.Web sample (Views/Home): named bundles in
    /// production and debug, dynamically required composite files, source maps, conditional requests, compression,
    /// and the graceful 404 handling for missing/stale/spoofed files.
    ///
    /// The whole suite runs twice: once against the in-memory cache and once against the physical file cache.
    /// </summary>
    public abstract class SmidgeEndpointTestsBase
    {
        private readonly SmidgeTestApp _app;

        protected SmidgeEndpointTestsBase(SmidgeAppFixture fixture) => _app = fixture.App;

        [Fact]
        public async Task Js_Bundle_Production_Returns_Minified_Combined_With_Caching_Headers()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/js/test-bundle-1");

            // A production bundle collapses to a single combined URL
            Assert.Single(urls);

            using var response = await client.GetAsync(urls[0]);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("javascript", response.Content.Headers.ContentType!.MediaType);

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(body);
            // Minification strips the source comments
            Assert.DoesNotContain("// a1.js", body);

            // Caching headers are applied for production requests
            Assert.NotNull(response.Headers.ETag);
            Assert.NotNull(response.Headers.CacheControl);
        }

        [Fact]
        public async Task Js_Bundle_Debug_Returns_Individual_Files()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/js/test-bundle-1?debug=true");

            // In debug the files are not combined
            Assert.True(urls.Length >= 2, $"Expected multiple debug URLs but got {urls.Length}");

            foreach (var url in urls)
            {
                using var response = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task Css_Bundle_Production_Returns_Minified()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/css/test-bundle-css");
            Assert.Single(urls);

            using var response = await client.GetAsync(urls[0]);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("css", response.Content.Headers.ContentType!.MediaType);

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(body);
            Assert.DoesNotContain("/* a1.css */", body);
        }

        [Fact]
        public async Task Dynamic_Composite_Js_Returns_Combined_Content()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/dynamic-js");
            Assert.NotEmpty(urls);
            Assert.All(urls, u => Assert.Contains("/sc/", u));

            foreach (var url in urls)
            {
                using var response = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotEmpty(await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task SourceMap_For_Minified_Js_Bundle_Is_Served()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/js/test-bundle-1");
            var bundleUrl = urls.Single();

            // Fetching the bundle triggers processing and writes the external source map
            var js = await client.GetStringAsync(bundleUrl);

            var match = Regex.Match(js, @"sourceMappingURL=(?<url>\S+)");
            Assert.True(match.Success, "Expected an external sourceMappingURL comment in the minified bundle output");

            var mapUrl = match.Groups["url"].Value;

            using var mapResponse = await client.GetAsync(mapUrl);
            Assert.Equal(HttpStatusCode.OK, mapResponse.StatusCode);
            Assert.Contains("json", mapResponse.Content.Headers.ContentType!.MediaType);

            var mapBody = await mapResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"version\"", mapBody);
        }

        [Fact]
        public async Task SourceMap_For_Bundle_Without_Map_Returns_404_Not_500()
        {
            using var client = _app.CreateClient();

            // This bundle is built from an already-minified .min.css file, so no source map is ever generated.
            // Build the bundle first, then request its (non-existent) source map. Prior to the fix this threw a
            // FileNotFoundException that surfaced as a 500 - a repeatable DoS vector (issues #199 / #185).
            var urls = await GetUrlsAsync(client, "/urls/css/notfound-map-css-bundle");
            var bundleUrl = urls.Single();
            (await client.GetAsync(bundleUrl)).Dispose();

            var mapUrl = ToSourceMapUrl(bundleUrl);

            using var response = await client.GetAsync(mapUrl);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Spoofed_Composite_File_Returns_404_Not_500()
        {
            using var client = _app.CreateClient();

            // Get a real composite URL (with the current, valid cache buster) then swap the file hashes for a bogus
            // one so the referenced cache file does not exist. This is exactly the DoS scenario from issue #199:
            // it must be a graceful 404, not an unhandled 500.
            var urls = await GetUrlsAsync(client, "/urls/dynamic-js");
            var compositeUrl = urls.First();

            var spoofed = SpoofCompositeFileName(compositeUrl);

            using var response = await client.GetAsync(spoofed);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task No_Files_Bundle_Returns_404()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/js/no-files");

            // Either no URL is produced, or the produced URL yields a 404 (there is nothing to serve)
            foreach (var url in urls)
            {
                using var response = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task Conditional_Request_With_Matching_ETag_Returns_304()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/js/test-bundle-1");
            var bundleUrl = urls.Single();

            using var first = await client.GetAsync(bundleUrl);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var etag = first.Headers.ETag;
            Assert.NotNull(etag);

            using var conditional = new HttpRequestMessage(HttpMethod.Get, bundleUrl);
            conditional.Headers.IfNoneMatch.Add(etag);

            using var second = await client.SendAsync(conditional);
            Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        }

        [Fact]
        public async Task Compressed_Request_Returns_Gzip_Encoded_Body()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/dynamic-js");
            var compositeUrl = urls.First();

            using var request = new HttpRequestMessage(HttpMethod.Get, compositeUrl);
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("gzip", response.Content.Headers.ContentEncoding);

            // Body is really gzip and decompresses to non-empty content
            var bytes = await response.Content.ReadAsByteArrayAsync();
            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            var decompressed = await reader.ReadToEndAsync();
            Assert.NotEmpty(decompressed);
        }

        [Fact]
        public async Task Uncompressed_Request_Has_No_Content_Encoding()
        {
            using var client = _app.CreateClient();

            var urls = await GetUrlsAsync(client, "/urls/dynamic-js");
            var compositeUrl = urls.First();

            using var request = new HttpRequestMessage(HttpMethod.Get, compositeUrl);
            request.Headers.AcceptEncoding.Clear();

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Content.Headers.ContentEncoding);
        }

        private static async Task<string[]> GetUrlsAsync(HttpClient client, string path)
        {
            var content = await client.GetStringAsync(path);
            return content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }

        /// <summary>
        /// Turns a bundle URL like <c>/sb/name.css.vABC</c> into its source-map URL <c>/sb/nmap/name.css.vABC</c>.
        /// </summary>
        private static string ToSourceMapUrl(string bundleUrl)
        {
            var lastSlash = bundleUrl.LastIndexOf('/');
            return bundleUrl.Substring(0, lastSlash) + "/nmap" + bundleUrl.Substring(lastSlash);
        }

        /// <summary>
        /// Replaces the file-hash portion of a composite URL (e.g. <c>/sc/a.b.js.vABC</c>) with a bogus value while
        /// preserving the extension and the (valid) cache buster value, producing a request that references a file
        /// that does not exist.
        /// </summary>
        private static string SpoofCompositeFileName(string compositeUrl)
        {
            var lastSlash = compositeUrl.LastIndexOf('/');
            var prefix = compositeUrl.Substring(0, lastSlash + 1);
            var segment = compositeUrl.Substring(lastSlash + 1);

            var match = Regex.Match(segment, @"\.(js|css)\.[vd].+$");
            Assert.True(match.Success, $"Unexpected composite URL format: {compositeUrl}");

            return prefix + "deadbeefdeadbeef" + match.Value;
        }
    }
}
