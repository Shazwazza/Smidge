using System;
using System.IO;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.Options;

namespace Smidge.Nuglify
{
    internal class SourceMapDeclaration : ISourceMapDeclaration
    {
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IRequestHelper _requestHelper;
        private readonly IOptions<SmidgeOptions> _smidgeOptions;

        public SourceMapDeclaration(FileSystemHelper fileSystemHelper, IRequestHelper requestHelper, IOptions<SmidgeOptions> smidgeOptions)
        {
            _fileSystemHelper = fileSystemHelper;
            _requestHelper = requestHelper;
            _smidgeOptions = smidgeOptions;
        }

        public string GetDeclaration(BundleContext bundleContext, V3DeferredSourceMap sourceMap)
        {
            //Close everything so everything is written to the output
            sourceMap.EndPackage();
            sourceMap.Dispose();

            switch (sourceMap.SourceMapType)
            {
                case SourceMapType.Default:
                    var mapContent = sourceMap.SourceMapOutput;
                    //now we need to save the map file so it can be retreived via the controller

                    //we need to go to the cache folder directly above the cache bust value (root) since that is where the source map is stored
                    var sourceMapFilePath = Path.Combine(bundleContext.BundleRequest.CacheBuster.GetValue(), bundleContext.BundleCompositeFile.Name + ".map");

                    var mapPath = _fileSystemHelper.GetCompositeFileInfo(sourceMapFilePath);
                    using (var writer = new StreamWriter(mapPath.PhysicalPath))
                    {
                        writer.Write(mapContent);
                    }
                    var url = GetSourceMapUrl(
                        bundleContext.BundleRequest.FileKey,
                        bundleContext.BundleRequest.Extension,
                        bundleContext.BundleRequest.Debug,
                        bundleContext.BundleRequest.CacheBuster);

                    return sourceMap.GetExternalFileSourceMapMarkup(url);
                case SourceMapType.Inline:
                    return sourceMap.GetInlineSourceMapMarkup();
                case SourceMapType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetSourceMapUrl(string bundleName, string fileExtension, bool debug, ICacheBuster cacheBuster)
        {
            if (cacheBuster == null) throw new ArgumentNullException(nameof(cacheBuster));

            const string handler = "~/{0}/{1}{2}.{3}{4}";
            return _requestHelper.Content(
                string.Format(
                    handler,
                    _smidgeOptions.Value.UrlOptions.BundleFilePath + "/nmap",
                    Uri.EscapeUriString(bundleName),
                    fileExtension,
                    debug ? 'd' : 'v',
                    cacheBuster.GetValue()));

        }
    }
}