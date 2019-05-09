using System;
using System.IO;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.Options;
using Smidge.Models;
using System.Threading.Tasks;

namespace Smidge.Nuglify
{
    internal class SourceMapDeclaration : ISourceMapDeclaration
    {
        private readonly IRequestHelper _requestHelper;
        private readonly IOptions<SmidgeOptions> _smidgeOptions;
        private readonly ISmidgeFileSystem _fileSystem;

        public SourceMapDeclaration(IRequestHelper requestHelper, IOptions<SmidgeOptions> smidgeOptions, ISmidgeFileSystem fileSystem)
        {
            _requestHelper = requestHelper;
            _smidgeOptions = smidgeOptions;
            _fileSystem = fileSystem;
        }

        public async Task<string> GetDeclarationAsync(BundleContext bundleContext, V3DeferredSourceMap sourceMap)
        {
            //Close everything so everything is written to the output
            sourceMap.EndPackage();
            sourceMap.Dispose();

            switch (sourceMap.SourceMapType)
            {
                case SourceMapType.Default:
                    var mapContent = sourceMap.SourceMapOutput;
                    //now we need to save the map file so it can be retreived via the controller

                    //TODO: No idea if this is gonna work
                    
                    //needs to be saved in a non compress folder
                    var sourceMapFile = _fileSystem.CacheFileSystem.FileProvider.GetRequiredFileInfo(bundleContext.BundleCompositeFile.Name + ".map");
                    await _fileSystem.CacheFileSystem.WriteFileAsync(sourceMapFile, mapContent);
                    
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