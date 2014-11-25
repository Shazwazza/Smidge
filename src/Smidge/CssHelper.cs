using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Smidge
{
    public sealed class CssHelper : IMinifier
    {
        private static readonly Regex ImportCssRegex = new Regex(@"@import url\((.+?)\);?", RegexOptions.Compiled);
        private static readonly Regex CssUrlRegex = new Regex(@"url\(((?![""']?data:).+?)\)", RegexOptions.Compiled);

        /// <summary>
        /// Returns the paths for the import statements and the resultant original css without the import statements
        /// </summary>
        /// <param name="content">The original css contents</param>
        /// <param name="importedPaths"></param>
        /// <returns></returns>
        public static string ParseImportStatements(string content, out IEnumerable<string> importedPaths)
        {
            var pathsFound = new List<string>();
            var matches = ImportCssRegex.Matches(content);
            foreach (Match match in matches)
            {
                //Ignore external imports
                var urlMatch = CssUrlRegex.Match(match.Value);
                if (urlMatch.Success && urlMatch.Groups.Count >= 2)
                {
                    var path = urlMatch.Groups[1].Value.Trim('\'', '"');
                    if ((path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("//", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                }

                //Strip the import statement                
                content = content.ReplaceFirst(match.Value, "");

                //write import css content
                var filePath = match.Groups[1].Value.Trim('\'', '"');
                pathsFound.Add(filePath);
            }

            importedPaths = pathsFound;
            return content.Trim();
        }

        /// <summary>
        /// Returns the CSS file with all of the url's formatted to be absolute locations
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="url"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string ReplaceUrlsWithAbsolutePaths(string fileContents, string url, HttpRequest req)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            fileContents = CssHelper.ReplaceUrlsWithAbsolutePaths(fileContents, uri.MakeAbsoluteUri(req));
            return fileContents;
        }

        /// <summary>
        /// Returns the CSS file with all of the url's formatted to be absolute locations
        /// </summary>
        /// <param name="fileContent">content of the css file</param>
        /// <param name="cssLocation">the uri location of the css file</param>
        /// <returns></returns>
        public static string ReplaceUrlsWithAbsolutePaths(string fileContent, Uri cssLocation)
        {
            var str = CssUrlRegex.Replace(fileContent, m =>
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
        
        /// <summary>
        /// Minifies Css
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string Minify(string input)
        {
            input = Regex.Replace(input, @"[\n\r]+\s*", string.Empty);
            input = Regex.Replace(input, @"\s+", " ");
            input = Regex.Replace(input, @"\s?([:,;{}])\s?", "$1");
            input = Regex.Replace(input, @"([\s:]0)(px|pt|%|em)", "$1");
            input = Regex.Replace(input, @"/\*[\d\D]*?\*/", string.Empty);
            return input;
        }
    }
}