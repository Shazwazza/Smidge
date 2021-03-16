using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUglify;
using NUglify.Helpers;
using NUglify.JavaScript;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Nuglify
{
    public class NuglifyJs : IPreProcessor
    {
        private readonly NuglifySettings _settings;
        private readonly ISourceMapDeclaration _sourceMapDeclaration;
        private readonly FileSystemHelper _fileSystemHelper;

        public NuglifyJs(NuglifySettings settings, ISourceMapDeclaration sourceMapDeclaration, FileSystemHelper fileSystemHelper)
        {
            _settings = settings;
            _sourceMapDeclaration = sourceMapDeclaration;
            _fileSystemHelper = fileSystemHelper;
        }
        
        public Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {            
            //Info for source mapping, see http://ajaxmin.codeplex.com/wikipage?title=SourceMaps
            // as an example, see http://ajaxmin.codeplex.com/SourceControl/latest#AjaxMinTask/AjaxMinManifestTask.cs under ProcessJavaScript
            // When a source map provider is specified, the process is:
            // * Create a string builder/writer to capture the output of the source map
            // * Create a sourcemap from the SourceMapFactory based on the provider name
            // * Set some V3 source map provider (the default) properties
            // * Call StartPackage, passing in the file path of the original file and the file path of the map (which will be appended to the end of the minified output)
            // * Do the minification
            // * Call EndPackage, and close/dispose of writers
            // * Get the source map result from the string builder

            if (fileProcessContext.WebFile.DependencyType == WebFileType.Css)
                throw new InvalidOperationException("Cannot use " + nameof(NuglifyJs) + " with a css file source");

            var nuglifyJsCodeSettings = _settings.JsCodeSettings;

            //Its very important that we clone here because the code settings is a singleton and we are changing it (i.e. the CodeSettings class is mutable)
            var codeSettings = nuglifyJsCodeSettings.CodeSettings.Clone();

            if (nuglifyJsCodeSettings.SourceMapType != SourceMapType.None)
            {
                var sourceMap = fileProcessContext.BundleContext.GetSourceMapFromContext(nuglifyJsCodeSettings.SourceMapType);

                codeSettings.SymbolsMap = sourceMap;

                //These are a couple of options that could be needed for V3 source maps

                //sourceRoot is explained here: 
                //  http://blog.teamtreehouse.com/introduction-source-maps
                //  https://www.html5rocks.com/en/tutorials/developertools/sourcemaps/
                //sourceMap.SourceRoot = 

                //SafeHeader is used to avoid XSS and adds some custom json to the top of the file , here's what the source code says:
                // "if we want to add the cross-site script injection protection string" it adds this to the top ")]}'"
                // explained here: https://www.html5rocks.com/en/tutorials/developertools/sourcemaps/ under "Potential XSSI issues"
                // ** not needed for inline
                //sourceMap.SafeHeader = 

                //generate a minified file path - this is really not used but is used in our inline source map like test.js --> test.min.js
                //var extension = Path.GetExtension(fileProcessContext.WebFile.FilePath);
                //var minPath = fileProcessContext.WebFile.FilePath.Substring(
                //                  0,
                //                  fileProcessContext.WebFile.FilePath.LastIndexOf('.')) + ".min" + extension;

                //we then need to 'StartPackage', this will be called once per file for the same source map instance but that is ok it doesn't cause any harm
                var fileName = fileProcessContext.BundleContext.BundleName + fileProcessContext.BundleContext.FileExtension;
                sourceMap.StartPackage(fileName, fileName + ".map");
            }

            //now do the processing
            var result = NuglifyProcess(fileProcessContext, codeSettings);

            if (result.HasErrors)
            {
                throw new InvalidOperationException(
                    string.Join(",", result.Errors.Select(x => x.ToString())));
            }

            fileProcessContext.Update(result.Code);

            if (nuglifyJsCodeSettings.SourceMapType != SourceMapType.None)
            {
                AddSourceMapAppenderToContext(fileProcessContext.BundleContext, nuglifyJsCodeSettings.SourceMapType);
            }

            return next(fileProcessContext);
        }

        /// <summary>
        /// Processes the file content by Nuglify
        /// </summary>
        /// <remarks>
        /// This is virtual allowing developers to override this in cases where customizations may need to be done 
        /// to the Nuglify process. For example, changing the FilePath used.
        /// </remarks>
        protected virtual UglifyResult NuglifyProcess(FileProcessContext fileProcessContext, CodeSettings codeSettings)
            => Uglify.Js(fileProcessContext.FileContent, _fileSystemHelper.ConvertToFileProviderPath(fileProcessContext.WebFile.FilePath), codeSettings);

        /// <summary>
        /// Adds a SourceMapAppender into the current bundle context if it doesn't already exist
        /// </summary>
        /// <param name="bundleContext"></param>
        /// <param name="sourceMapType"></param>
        /// <returns></returns>
        private void AddSourceMapAppenderToContext(BundleContext bundleContext, SourceMapType sourceMapType)
        {
            //if it already exist, then ignore
            var key = typeof(SourceMapDeclaration).Name;
            if (bundleContext.Items.TryGetValue(key, out object sm))
            {
                return;
            }

            //not in the context so add a flag so it's not re-added
            bundleContext.Items[key] = "added";

            bundleContext.AddAppender(() =>
            {
                var sourceMap = bundleContext.GetSourceMapFromContext(sourceMapType);
                return _sourceMapDeclaration.GetDeclaration(bundleContext, sourceMap);
            });
        }


    }
}
