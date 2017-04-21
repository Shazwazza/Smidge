using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUglify;
using NUglify.Helpers;
using NUglify.JavaScript;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Nuglify
{
    public class NuglifyJs : IPreProcessor
    {
        private readonly NuglifySettings _settings;

        public NuglifyJs(NuglifySettings settings)
        {
            _settings = settings;
        }

        public Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task<string>> next)
        {
            //TODO: So this is almost working, the problem is that for each minified file currently, the inline source map will be appended to the end of the single file
            // but the spec doesn't support multiple source map declarations, just one source map declaration at the very end of the combined output.
            // So there's a bit of trickery needed here, we'll probably have to update the FileProcessContext to have a reference to some sort of "BundleContext"
            // which we could then add some information to and perhaps callbacks to to prepend or append data to the finalized bundle.
            // ... hrm, interesting ....
            // Some references for this
            // * https://github.com/mishoo/UglifyJS2/issues/849
            // * https://github.com/mishoo/UglifyJS2/issues/145
            // This is a utility that can combine source maps, which is what we'll ultimately need to do as well
            // * https://github.com/edc/mapcat


            //For source mapping, see http://ajaxmin.codeplex.com/wikipage?title=SourceMaps
            // as an example, see http://ajaxmin.codeplex.com/SourceControl/latest#AjaxMinTask/AjaxMinManifestTask.cs under ProcessJavaScript
            // When a source map provider is specified, the process is:
            // * Create a string builder/writer to capture the output of the source map
            // * Create a sourcemap from the SourceMapFactory based on the provider name
            // * Set some V3 source map provider (the default) properties
            // * Call StartPackage, passing in the file path of the original file and the file path of the map (which will be appended to the end of the minified output)
            // * Do the minification
            // * Call EndPackage, and close/dispose of writers
            // * Get the source map result from the string builder

            var codeSettings = fileProcessContext.WebFile.DependencyType == WebFileType.Css
                ? _settings.CssCodeSettings
                : _settings.JsCodeSettings;

            StringBuilder sb = null;
            StringWriter sourceMapWriter = null;
            ISourceMap sourceMap = null;
            try
            {
                if (codeSettings.EnableSourceMaps)
                {
                    sb = new StringBuilder();
                    sourceMapWriter = new Utf8StringWriter(sb);

                    sourceMap = new V3InlineSourceMap((V3SourceMap)SourceMapFactory.Create(sourceMapWriter, V3SourceMap.ImplementationName), sb);                    

                    codeSettings.CodeSettings.SymbolsMap = sourceMap;

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
                    var extension = Path.GetExtension(fileProcessContext.WebFile.FilePath);
                    var minPath = fileProcessContext.WebFile.FilePath.Substring(
                        0,
                        fileProcessContext.WebFile.FilePath.LastIndexOf('.')) + ".min" + extension;

                    //we then need to 'StartPackage'
                    sourceMap.StartPackage(minPath, minPath + ".map");
                }

                var result = Uglify.Js(fileProcessContext.FileContent, fileProcessContext.WebFile.FilePath, codeSettings.CodeSettings);

                if (result.HasErrors)
                {
                    //TODO: need to format this exception message nicely
                    throw new InvalidOperationException(
                        string.Join(",", result.Errors.Select(x => x.Message)));
                }

                if (sb != null)
                {
                    //Close everything so everything is written to the output
                    sourceMap.EndPackage();
                    sourceMap.Dispose();
                    sourceMap = null;
                }

                return next(result.Code);
            }
            finally
            {
                sourceMapWriter?.Dispose();
                sourceMap?.Dispose();
            }
        }
    }
}
