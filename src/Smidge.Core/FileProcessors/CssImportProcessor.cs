using Smidge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public CssImportProcessor(ISmidgeFileSystem fileSystem, IWebsiteInfo siteInfo, IRequestHelper requestHelper)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _siteInfo = siteInfo ?? throw new ArgumentNullException(nameof(siteInfo));
            _requestHelper = requestHelper ?? throw new ArgumentNullException(nameof(requestHelper));
        }

        private readonly ISmidgeFileSystem _fileSystem;
        private readonly IWebsiteInfo _siteInfo;
        private readonly IRequestHelper _requestHelper;

        public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            var sb = new StringBuilder();

            var removedImports = ParseImportStatements(fileProcessContext.FileContent, out IEnumerable<string> internalImports, out _);

            //need to write the imported sheets first since these theoretically should *always* be at the top for browser to support them
            foreach (var importPath in internalImports)
            {
                // convert to it's absolute path
                var contentPath = _requestHelper.Content(fileProcessContext.WebFile);
                var uri = new Uri(contentPath, UriKind.RelativeOrAbsolute).MakeAbsoluteUri(_siteInfo.GetBaseUrl());
                var absolute = uri.ToAbsolutePath(importPath);
                var path = _requestHelper.Content(absolute);

                //is it external?
                if (path.Contains(SmidgeConstants.SchemeDelimiter))
                {
                    // This should never happend
                }
                else
                {
                    //it's internal (in theory)
                    var filePath = _fileSystem.GetRequiredFileInfo(path);
                    var content = await _fileSystem.ReadContentsAsync(filePath);

                    //This needs to be put back through the whole pre-processor pipeline before being added,
                    // so we'll clone the original webfile with it's new path, this will inherit the whole pipeline,
                    // and then we'll execute the pipeline for that file
                    var clone = fileProcessContext.WebFile.Duplicate(path);
                    var processed = await clone.Pipeline.ProcessAsync(new FileProcessContext(content, clone, fileProcessContext.BundleContext));

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

            fileProcessContext.Update(sb.ToString());

            await next(fileProcessContext);
        }

        /// <summary>
        /// Returns the paths for the import statements and the resultant original css without the import statements
        /// </summary>
        /// <param name="content">The original css contents</param>
        /// <param name="importedPaths"></param>
        /// <param name="externalPaths">
        /// imports declared that are external resources - this output is just information, those declared imports will remain in the content string 
        /// to be loaded naturally by the output css file.
        /// </param>
        /// <returns></returns>
        internal string ParseImportStatements(string content, out IEnumerable<string> importedPaths, out IEnumerable<string> externalPaths)
        {
            var internalPathsFound = new List<string>();
            var externalPathsFound = new List<string>();
            var matches = RegexStatements.ImportCssRegex.Matches(content);
            foreach (Match match in matches)
            {                
                //Ignore external imports - they might be wrapped in a url( block so get it
                var urlMatch = RegexStatements.CssUrlRegex.Match(match.Value);
                if (urlMatch.Success && urlMatch.Groups.Count >= 2)
                {
                    var path = urlMatch.Groups[1].Value.Trim('\'', '"');
                    if (_requestHelper.IsExternalRequestPath(path))
                    {
                        externalPathsFound.Add(path);
                        continue;
                    }
                }
                
                //Strip the import statement
                content = content.ReplaceFirst(match.Value, "");

                //get the last non-empty match
                var filePath = match.Groups.Cast<Group>().Where(x => !string.IsNullOrEmpty(x.Value)).Last().Value.Trim('\'', '"');

                //Ignore external imports - this will occur if they are not wrapped in a url block
                if (_requestHelper.IsExternalRequestPath(filePath)) continue;

                internalPathsFound.Add(filePath);
            }

            importedPaths = internalPathsFound;
            externalPaths = externalPathsFound;
            return content.Trim();
        }

        
    }
}