using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Smidge;
using Smidge.Cache;
using Smidge.InMemory;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.Integration.Tests
{
    /// <summary>
    /// Spins up a real, self-hosted Kestrel server running Smidge end to end. This mirrors the manual testing
    /// that is normally done with the Smidge.Web sample project (the Views/Home scenarios): a set of bundles are
    /// configured and small helper endpoints render the bundle URLs via <see cref="SmidgeHelper"/> exactly like a
    /// Razor view would. Tests then make real HTTP requests to those URLs and assert on the responses.
    /// </summary>
    public sealed class SmidgeTestApp : IAsyncDisposable
    {
        private readonly IHost _host;

        private SmidgeTestApp(IHost host, string baseAddress)
        {
            _host = host;
            BaseAddress = baseAddress;
        }

        public string BaseAddress { get; }

        /// <summary>
        /// A client that leaves compression untouched so tests can assert on Content-Encoding and decompress manually.
        /// </summary>
        public HttpClient CreateClient()
            => new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.None })
            {
                BaseAddress = new Uri(BaseAddress)
            };

        public static async Task<SmidgeTestApp> StartAsync(bool inMemory)
        {
            var contentRoot = AppContext.BaseDirectory;
            var webRoot = Path.Combine(contentRoot, "wwwroot");

            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel();
                    webBuilder.UseContentRoot(contentRoot);
                    webBuilder.UseWebRoot(webRoot);
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddSmidge(new ConfigurationBuilder().Build());

                        if (inMemory)
                        {
                            services.AddSmidgeInMemory();
                        }

                        services.Configure<SmidgeOptions>(options =>
                        {
                            // A cache buster that is stable for the lifetime of the process, matching the Smidge.Web sample.
                            options.DefaultBundleOptions.DebugOptions.SetCacheBusterType<AppDomainLifetimeCacheBuster>();
                            options.DefaultBundleOptions.ProductionOptions.SetCacheBusterType<AppDomainLifetimeCacheBuster>();
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseStaticFiles();
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            // Render the URLs Smidge would emit for a named JS bundle, newline separated.
                            endpoints.MapGet("/urls/js/{bundle}", async (HttpContext ctx, string bundle) =>
                            {
                                var smidge = ctx.RequestServices.GetRequiredService<SmidgeHelper>();
                                var debug = IsDebug(ctx);
                                var urls = await smidge.GenerateJsUrlsAsync(bundle, debug);
                                return UrlResult(urls);
                            });

                            // Render the URLs Smidge would emit for a named CSS bundle, newline separated.
                            endpoints.MapGet("/urls/css/{bundle}", async (HttpContext ctx, string bundle) =>
                            {
                                var smidge = ctx.RequestServices.GetRequiredService<SmidgeHelper>();
                                var debug = IsDebug(ctx);
                                var urls = await smidge.GenerateCssUrlsAsync(bundle, debug);
                                return UrlResult(urls);
                            });

                            // Dynamically require a folder of JS files (no named bundle) which produces composite URLs.
                            endpoints.MapGet("/urls/dynamic-js", async (HttpContext ctx) =>
                            {
                                var smidge = ctx.RequestServices.GetRequiredService<SmidgeHelper>();
                                smidge.RequiresJs("~/Js/Folder/*.js");
                                var urls = await smidge.GenerateJsUrlsAsync(debug: IsDebug(ctx));
                                return UrlResult(urls);
                            });

                            // Dynamically require a folder of CSS files (no named bundle) which produces composite URLs.
                            endpoints.MapGet("/urls/dynamic-css", async (HttpContext ctx) =>
                            {
                                var smidge = ctx.RequestServices.GetRequiredService<SmidgeHelper>();
                                smidge.RequiresCss("~/Css/Folder/*.css");
                                var urls = await smidge.GenerateCssUrlsAsync(debug: IsDebug(ctx));
                                return UrlResult(urls);
                            });
                        });

                        app.UseSmidge(bundles =>
                        {
                            // JS bundle with a couple of real files plus one already-minified file (min removed by convention).
                            bundles.Create("test-bundle-1",
                                new JavaScriptFile("~/Js/Bundle1/a1.js"),
                                new JavaScriptFile("~/Js/Bundle1/a2.js"),
                                new JavaScriptFile("~/Js/Bundle1/a3.min.js"));

                            // CSS bundle.
                            bundles.CreateCss("test-bundle-css",
                                "~/Css/Bundle1/a1.css",
                                "~/Css/Bundle1/a2.css");

                            // A bundle whose glob matches nothing.
                            bundles.CreateJs("no-files", "~/Js/not-found/*.js");

                            // A CSS bundle built from an already-minified file. No source map is ever generated for it,
                            // so requesting its /nmap URL must return 404 (previously threw a 500). See issues #199 / #185.
                            bundles.CreateCss("notfound-map-css-bundle", "~/Css/notFoundMap.min.css");
                        });
                    });
                });

            var host = await builder.StartAsync();

            var address = host.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .First();

            return new SmidgeTestApp(host, address);
        }

        private static bool IsDebug(HttpContext ctx)
            => string.Equals(ctx.Request.Query["debug"], "true", StringComparison.OrdinalIgnoreCase);

        private static IResult UrlResult(System.Collections.Generic.IEnumerable<string> urls)
            => Results.Text(string.Join("\n", urls), "text/plain");

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
