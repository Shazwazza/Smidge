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

                    //needs to be saved in the current cache bust folder
                    var cacheBusterValue = bundleContext.CacheBusterValue;
                    string filePath = bundleContext.GetSourceMapFilePath(cacheBusterValue);
                    await _fileSystem.CacheFileSystem.WriteFileAsync(filePath, mapContent);
                    
                    var url = GetSourceMapUrl(
                        bundleContext.BundleRequest.FileKey,
                        bundleContext.BundleRequest.Extension,
                        bundleContext.BundleRequest.Debug,
                        cacheBusterValue);

                    return sourceMap.GetExternalFileSourceMapMarkup(url);
                case SourceMapType.Inline:
                    return sourceMap.GetInlineSourceMapMarkup();
                case SourceMapType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetSourceMapUrl(string bundleName, string fileExtension, bool debug, string cacheBusterValue)
        {
            if (string.IsNullOrWhiteSpace(cacheBusterValue))
            {
                throw new ArgumentException($"'{nameof(cacheBusterValue)}' cannot be null or whitespace.", nameof(cacheBusterValue));
            }

            const string handler = "~/{0}/{1}{2}.{3}{4}";
            return _requestHelper.Content(
                string.Format(
                    handler,
                    _smidgeOptions.Value.UrlOptions.BundleFilePath + "/nmap",
                    Uri.EscapeDataString(bundleName),
                    fileExtension,
                    debug ? 'd' : 'v',
                    cacheBusterValue));

        }
    }
}
