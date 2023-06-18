using System;
using Microsoft.AspNetCore.Mvc;
using Smidge.CompositeFiles;
using Smidge.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Smidge.FileProcessors;
using Smidge.Cache;
using Microsoft.AspNetCore.Authorization;

namespace Smidge.Controllers
{

    /// <summary>
    /// Controller for handling minified/combined responses
    /// </summary>
    [AddCompressionHeader(Order = 0)]
    [AddExpiryHeaders(Order = 1)]
    [CheckNotModified(Order = 2)]
    [CompositeFileCacheFilter(Order = 3)]
    [AllowAnonymous]
    public class SmidgeController : Controller
    {
        private readonly ISmidgeFileSystem _fileSystem;
        private readonly IBundleManager _bundleManager;
        private readonly IBundleFileSetGenerator _fileSetGenerator;
        private readonly PreProcessPipelineFactory _processorFactory;
        private readonly IPreProcessManager _preProcessManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileSystemHelper"></param>
        /// <param name="bundleManager"></param>
        /// <param name="fileSetGenerator"></param>
        /// <param name="processorFactory"></param>
        /// <param name="preProcessManager"></param>
        /// <param name="logger"></param>
        public SmidgeController(
            ISmidgeFileSystem fileSystemHelper,
            IBundleManager bundleManager,
            IBundleFileSetGenerator fileSetGenerator,
            PreProcessPipelineFactory processorFactory,
            IPreProcessManager preProcessManager,
            ILogger<SmidgeController> logger)
        {
            _fileSystem = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            _fileSetGenerator = fileSetGenerator ?? throw new ArgumentNullException(nameof(fileSetGenerator));
            _processorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
            _preProcessManager = preProcessManager ?? throw new ArgumentNullException(nameof(preProcessManager));
            _logger = logger;
        }

        /// <summary>
        /// Handles requests for named bundles
        /// </summary>
        /// <param name="bundleModel">The bundle model</param>
        /// <returns></returns>       
        public async Task<IActionResult> Bundle(
            [FromServices] BundleRequestModel bundleModel)
        {
            if (!bundleModel.IsBundleFound || !_bundleManager.TryGetValue(bundleModel.FileKey, out Bundle foundBundle))
            {
                return NotFound();
            }

            var bundleOptions = foundBundle.GetBundleOptions(_bundleManager, bundleModel.Debug);

            var cacheBusterValue = bundleModel.ParsedPath.CacheBusterValue;

            //now we need to determine if this bundle has already been created
            var cacheFile = _fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, bundleModel.Compression, bundleModel.FileKey, out var cacheFilePath);
            if (cacheFile.Exists)
            {
                _logger.LogDebug($"Returning bundle '{bundleModel.FileKey}' from cache");


                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the PhysicalFileResult
                    //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files                    
                    return PhysicalFile(cacheFile.PhysicalPath, bundleModel.Mime);
                }
                else
                {
                    return File(cacheFile.CreateReadStream(), bundleModel.Mime);
                }
            }

            //the bundle doesn't exist so we'll go get the files, process them and create the bundle
            //TODO: We should probably lock here right?! we don't want multiple threads trying to do this at the same time, we'll need a dictionary of locks to do this effectively

            //get the files for the bundle
            var files = _fileSetGenerator.GetOrderedFileSet(foundBundle,
                    _processorFactory.CreateDefault(
                        //the file type in the bundle will always be the same
                        foundBundle.Files[0].DependencyType))
                .ToArray();

            if (files.Length == 0)
            {
                return NotFound();
            }

            using (var bundleContext = new BundleContext(cacheBusterValue, bundleModel, cacheFilePath))
            {
                var watch = new Stopwatch();
                watch.Start();
                _logger.LogDebug($"Processing bundle '{bundleModel.FileKey}', debug? {bundleModel.Debug} ...");

                //we need to do the minify on the original files
                foreach (var file in files)
                {
                    await _preProcessManager.ProcessAndCacheFileAsync(file, bundleOptions, bundleContext);
                }

                //Get each file path to it's hashed location since that is what the pre-processed file will be saved as
                var fileInfos = files.Select(x => _fileSystem.CacheFileSystem.GetCacheFile(
                    x,
                    () => _fileSystem.GetRequiredFileInfo(x),
                    bundleOptions.FileWatchOptions.Enabled,
                    Path.GetExtension(x.FilePath),
                    cacheBusterValue,
                    out _));

                using (var resultStream = await GetCombinedStreamAsync(fileInfos, bundleContext))
                {
                    //compress the response (if enabled)
                    //do not compress anything if it's not enabled in the bundle options
                    var compressedStream = await Compressor.CompressAsync(bundleOptions.CompressResult ? bundleModel.Compression : CompressionType.None,
                                                                          bundleOptions.CompressionLevel,
                                                                          resultStream);

                    //save the resulting compressed file, if compression is not enabled it will just save the non compressed format
                    // this persisted file will be used in the CheckNotModifiedAttribute which will short circuit the request and return
                    // the raw file if it exists for further requests to this path
                    await CacheCompositeFileAsync(_fileSystem.CacheFileSystem, cacheFilePath, compressedStream);

                    _logger.LogDebug($"Processed bundle '{bundleModel.FileKey}' in {watch.ElapsedMilliseconds}ms");

                    //return the stream
                    return File(compressedStream, bundleModel.Mime);
                }
            }
        }

        /// <summary>
        /// Handles requests for composite files (non-named bundles)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<IActionResult> Composite(
             [FromServices] CompositeFileModel file)
        {
            if (!file.IsBundleFound || !file.ParsedPath.Names.Any())
            {
                return NotFound();
            }

            var cacheBusterValue = file.ParsedPath.CacheBusterValue;

            var cacheFile = _fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, file.Compression, file.FileKey, out var cacheFilePath);

            if (cacheFile.Exists)
            {
                //this is already processed, return it
                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the PhysicalFileResult
                    //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files                    
                    return PhysicalFile(cacheFile.PhysicalPath, file.Mime);
                }
                else
                {
                    return File(cacheFile.CreateReadStream(), file.Mime);
                }
            }

            using (var bundleContext = new BundleContext(cacheBusterValue, file, cacheFilePath))
            {
                var files = file.ParsedPath.Names.Select(filePath =>
                    _fileSystem.CacheFileSystem.GetRequiredFileInfo(
                        $"{file.ParsedPath.CacheBusterValue}/{filePath + file.Extension}"));

                using (var resultStream = await GetCombinedStreamAsync(files, bundleContext))
                {
                    var compressedStream = await Compressor.CompressAsync(file.Compression, resultStream);

                    await CacheCompositeFileAsync(_fileSystem.CacheFileSystem, cacheFilePath, compressedStream);

                    return File(compressedStream, file.Mime);
                }
            }
        }

        private static async Task CacheCompositeFileAsync(ICacheFileSystem cacheProvider, string filePath, Stream compositeStream)
        {
            await cacheProvider.WriteFileAsync(filePath, compositeStream);
            if (compositeStream.CanSeek)
                compositeStream.Position = 0;
        }

        /// <summary>
        /// Combines files into a single stream
        /// </summary>
        /// <param name="files"></param>
        /// <param name="bundleContext"></param>
        /// <returns></returns>
        private async Task<Stream> GetCombinedStreamAsync(IEnumerable<IFileInfo> files, BundleContext bundleContext)
        {
            //TODO: Here we need to be able to prepend/append based on a "BundleContext" (or similar)

            List<Stream> inputs = null;
            try
            {
                inputs = files.Where(x => x.Exists)
                    .Select(x => x.CreateReadStream())
                    .ToList();

                var delimeter = bundleContext.BundleRequest.Extension == ".js" ? ";\n" : "\n";
                var combined = await bundleContext.GetCombinedStreamAsync(inputs, delimeter);
                return combined;
            }
            finally
            {
                if (inputs != null)
                {
                    foreach (var input in inputs)
                    {
                        input.Dispose();
                    }
                }
            }
        }
    }
}
