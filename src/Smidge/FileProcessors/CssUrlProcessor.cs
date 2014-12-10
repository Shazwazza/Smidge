using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Changes all of the relative urls (i.e. like image paths) to absolute ones
    /// </summary>
    public class CssUrlProcessor : IPreProcessor
    {
        public CssUrlProcessor(FileSystemHelper fileSystemHelper, IContextAccessor<HttpContext> http)
        {
            _fileSystemHelper = fileSystemHelper;
            _http = http;
        }


        private IContextAccessor<HttpContext> _http;
        private FileSystemHelper _fileSystemHelper;

        public Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            //ensure the Urls in the css are changed to absolute
            var parsedUrls = ReplaceUrlsWithAbsolutePaths(fileProcessContext.FileContent, fileProcessContext.WebFile.FilePath, _http.Value.Request);

            return Task.FromResult(parsedUrls);
        }

        /// <summary>
        /// Returns the CSS file with all of the url's formatted to be absolute locations
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="url"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        internal static string ReplaceUrlsWithAbsolutePaths(string fileContents, string url, HttpRequest req)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            fileContents = ReplaceUrlsWithAbsolutePaths(fileContents, uri.MakeAbsoluteUri(req));
            return fileContents;
        }

        /// <summary>
        /// Returns the CSS file with all of the url's formatted to be absolute locations
        /// </summary>
        /// <param name="fileContent">content of the css file</param>
        /// <param name="cssLocation">the uri location of the css file</param>
        /// <returns></returns>
        internal static string ReplaceUrlsWithAbsolutePaths(string fileContent, Uri cssLocation)
        {
            var str = RegexStatements.CssUrlRegex.Replace(fileContent, m =>
            {
                if (m.Groups.Count == 2)
                {
                    var match = m.Groups[1].Value.Trim('\'', '"');
                    var hashSplit = match.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                    return string.Format(@"url(""{0}{1}"")",
                                         (match.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                         || match.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                                         || match.StartsWith("//", StringComparison.OrdinalIgnoreCase)) ? match : new Uri(cssLocation, match).PathAndQuery,
                                         hashSplit.Length > 1 ? ("#" + hashSplit[1]) : "");
                }
                return m.Value;
            });

            return str;
        }
    }
}