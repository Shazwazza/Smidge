using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Processes @import statements in css and includes the imported content in the result
    /// </summary>
    public class CssImportProcessor : IPreProcessor
    {
        public CssImportProcessor(FileSystemHelper fileSystemHelper, IContextAccessor<HttpContext> http)
        {
            _fileSystemHelper = fileSystemHelper;
            _http = http;
        }

        
        private IContextAccessor<HttpContext> _http;
        private FileSystemHelper _fileSystemHelper;

        public async Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            var sb = new StringBuilder();

            IEnumerable<string> importedPaths;
            var removedImports = ParseImportStatements(fileProcessContext.FileContent, out importedPaths);

            //need to write the imported sheets first since these theoretically should *always* be at the top for browser to support them
            foreach (var importPath in importedPaths)
            {
                var path = _fileSystemHelper.NormalizeWebPath(importPath, _http.Value.Request);
                //is it external?
                if (path.Contains(Uri.SchemeDelimiter))
                {
                    //Pretty sure we just leave the external refs in there
                    //TODO: Look in CDF, we have tests for this, pretty sure the ParseImportStatements removes that
                }
                else
                {
                    //it's internal (in theory)
                    var filePath = _fileSystemHelper.MapPath(string.Format("~/{0}", path));
                    if (System.IO.File.Exists(filePath))
                    {
                        var content = await _fileSystemHelper.ReadContentsAsync(filePath);
                        //TODO: This needs to be put back through the whole pre-processor pipeline before being added!
                        // we need to add a ctor reference to that pipeline engine when we make it

                        sb.Append(content);
                    }
                    else
                    {
                        //TODO: Need to log this
                    }
                }

            }

            sb.Append(removedImports);

            return sb.ToString();
        }

        /// <summary>
        /// Returns the paths for the import statements and the resultant original css without the import statements
        /// </summary>
        /// <param name="content">The original css contents</param>
        /// <param name="importedPaths"></param>
        /// <returns></returns>
        internal static string ParseImportStatements(string content, out IEnumerable<string> importedPaths)
        {
            var pathsFound = new List<string>();
            var matches = RegexStatements.ImportCssRegex.Matches(content);
            foreach (Match match in matches)
            {
                //Ignore external imports
                var urlMatch = RegexStatements.CssUrlRegex.Match(match.Value);
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
    }
}