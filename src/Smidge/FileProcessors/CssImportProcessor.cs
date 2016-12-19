using Microsoft.AspNetCore.Http;
using Smidge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Processes @import statements in css and includes the imported content in the result
    /// </summary>
    public class CssImportProcessor : IPreProcessor
    {
        public CssImportProcessor(FileSystemHelper fileSystemHelper, IWebsiteInfo siteInfo, IRequestHelper requestHelper)
        {
            if (fileSystemHelper == null) throw new ArgumentNullException(nameof(fileSystemHelper));
            if (siteInfo == null) throw new ArgumentNullException(nameof(siteInfo));
            if (requestHelper == null) throw new ArgumentNullException(nameof(requestHelper));
            _fileSystemHelper = fileSystemHelper;
            _siteInfo = siteInfo;
            _requestHelper = requestHelper;
        }

        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IWebsiteInfo _siteInfo;
        private readonly IRequestHelper _requestHelper;

        public async Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task> next)
        {
            var sb = new StringBuilder();

            IEnumerable<string> importedPaths;
            var removedImports = ParseImportStatements(fileProcessContext.FileContent, out importedPaths);

            //need to write the imported sheets first since these theoretically should *always* be at the top for browser to support them
            foreach (var importPath in importedPaths)
            {
                var uri = new Uri(fileProcessContext.WebFile.FilePath, UriKind.RelativeOrAbsolute).MakeAbsoluteUri(_siteInfo.GetBaseUrl());
                var absolute = uri.ToAbsolutePath(importPath);

                var path = _requestHelper.Content(absolute);
                //is it external?
                if (path.Contains(Constants.SchemeDelimiter))
                {
                    //Pretty sure we just leave the external refs in there
                    //TODO: Look in CDF, we have tests for this, pretty sure the ParseImportStatements removes that
                }
                else
                {
                    //it's internal (in theory)
                    var filePath = _fileSystemHelper.GetFileInfo(path);
                    var content = await _fileSystemHelper.ReadContentsAsync(filePath);

                    //This needs to be put back through the whole pre-processor pipeline before being added,
                    // so we'll clone the original webfile with it's new path, this will inherit the whole pipeline,
                    // and then we'll execute the pipeline for that file
                    var clone = fileProcessContext.WebFile.Duplicate(path);
                    var processed = await clone.Pipeline.ProcessAsync(new FileProcessContext(content, clone));

                    sb.Append(processed);

                    ////  _fileSystemHelper.MapWebPath(path.StartsWith("/") ? path : string.Format("~/{0}", path));
                    //if (System.IO.File.Exists(filePath))
                    //{
                       
                    //}
                    //else
                    //{
                    //    //TODO: Need to log this
                    //}
                }

            }

            sb.Append(removedImports);

            await next(sb.ToString());
        }

        /// <summary>
        /// Returns the paths for the import statements and the resultant original css without the import statements
        /// </summary>
        /// <param name="content">The original css contents</param>
        /// <param name="importedPaths"></param>
        /// <returns></returns>
        internal string ParseImportStatements(string content, out IEnumerable<string> importedPaths)
        {
            var pathsFound = new List<string>();
            var matches = RegexStatements.ImportCssRegex.Matches(content);
            foreach (Match match in matches)
            {                
                //Ignore external imports - they might be wrapped in a url( block so get it
                var urlMatch = RegexStatements.CssUrlRegex.Match(match.Value);
                if (urlMatch.Success && urlMatch.Groups.Count >= 2)
                {
                    var path = urlMatch.Groups[1].Value.Trim('\'', '"');
                    if (_requestHelper.IsExternalRequestPath(path)) continue;
                }
                
                //Strip the import statement                
                content = content.ReplaceFirst(match.Value, "");

                //get the last non-empty match
                var filePath = match.Groups.Cast<Group>().Where(x => !string.IsNullOrEmpty(x.Value)).Last().Value.Trim('\'', '"');

                //Ignore external imports - this will occur if they are not wrapped in a url block
                if (_requestHelper.IsExternalRequestPath(filePath)) continue;

                pathsFound.Add(filePath);
            }

            importedPaths = pathsFound;
            return content.Trim();
        }

        
    }
}