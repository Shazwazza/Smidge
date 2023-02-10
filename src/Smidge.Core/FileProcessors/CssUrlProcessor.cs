﻿using System;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Changes all of the relative urls (i.e. like image paths) to absolute ones
    /// </summary>
    public class CssUrlProcessor : IPreProcessor
    {
        private readonly IWebsiteInfo _siteInfo;
        private readonly IRequestHelper _requestHelper;

        public CssUrlProcessor(IWebsiteInfo siteInfo, IRequestHelper requestHelper)
        {
            _siteInfo = siteInfo ?? throw new ArgumentNullException(nameof(siteInfo));
            _requestHelper = requestHelper;
        }

        public Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            //ensure the Urls in the css are changed to absolute            
            var contentPath = _requestHelper.Content(fileProcessContext.WebFile);
            var parsedUrls = ReplaceUrlsWithAbsolutePaths(fileProcessContext.FileContent, contentPath);

            fileProcessContext.Update(parsedUrls);
            return next(fileProcessContext);
        }

        /// <summary>
        /// Returns the CSS file with all of the url's formatted to be absolute locations
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        internal string ReplaceUrlsWithAbsolutePaths(string fileContents, string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            fileContents = ReplaceUrlsWithAbsolutePaths(fileContents, uri.MakeAbsoluteUri(_siteInfo.GetBaseUrl()));
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