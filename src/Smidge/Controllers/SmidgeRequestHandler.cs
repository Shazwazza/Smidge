using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Controllers
{

    /// <summary>
    /// Handles requests for minified/combined responses.
    /// </summary>
    /// <remarks>
    /// This was previously an MVC controller. For Smidge 5 it is a lightweight POCO handler invoked directly
    /// from minimal API endpoints, so Smidge no longer requires MVC.
    /// </remarks>
    internal sealed class SmidgeRequestHandler
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly ISmidgeFileSystem _fileSystem;
        private readonly IBundleManager _bundleManager;
        private readonly IBundleFileSetGenerator _fileSetGenerator;
        private readonly PreProcessPipelineFactory _processorFactory;
        private readonly IPreProcessManager _preProcessManager;
        private readonly ILogger _logger;
        private readonly CacheBusterResolver _cacheBusterResolver;

        public SmidgeRequestHandler(
            ISmidgeFileSystem fileSystemHelper,
            IBundleManager bundleManager,
            IBundleFileSetGenerator fileSetGenerator,
            PreProcessPipelineFactory processorFactory,
            IPreProcessManager preProcessManager,
            ILogger<SmidgeRequestHandler> logger,
            CacheBusterResolver cacheBusterResolver)
        {
            _fileSystem = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            _fileSetGenerator = fileSetGenerator ?? throw new ArgumentNullException(nameof(fileSetGenerator));
            _processorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
            _preProcessManager = preProcessManager ?? throw new ArgumentNullException(nameof(preProcessManager));
            _logger = logger;
            _cacheBusterResolver = cacheBusterResolver;
        }

        /// <summary>
        /// Handles requests for named bundles
        /// </summary>
        public async Task<IResult> Bundle(BundleRequestModel bundleModel)
        {
            if (!bundleModel.IsBundleFound || !_bundleManager.TryGetValue(bundleModel.FileKey, out Bundle foundBundle))
            {
                return Results.NotFound();
            }

            if (TryGetBundle(bundleModel, out IResult result, out string cacheFilePath))
            {
                return result;
            }

            SemaphoreSlim bundleLock = s_locks.GetOrAdd(foundBundle.Name, s => new SemaphoreSlim(1, 1));
            await bundleLock.WaitAsync();
            try
            {
                // Double check, might be available now
                if (TryGetBundle(bundleModel, out result, out _))
                {
                    return result;
                }

                //the bundle doesn't exist so we'll go get the files, process them and create the bundle

                //get the files for the bundle
                IWebFile[] files = _fileSetGenerator.GetOrderedFileSet(foundBundle,
                        _processorFactory.CreateDefault(
                            //the file type in the bundle will always be the same
                            foundBundle.Files[0].DependencyType))
                    .ToArray();

                if (files.Length == 0)
                {
                    return Results.NotFound();
                }

                Options.BundleOptions bundleOptions = foundBundle.GetBundleOptions(_bundleManager, bundleModel.Debug);

                // Validate the cache buster in the case where the file wasn't eagerly created by the view,
                // and the request is coming in directly to the handler.
                string cacheBusterValue = bundleModel.ParsedPath.CacheBusterValue;
                Type cacheBusterType = bundleOptions.GetCacheBusterType();
                ICacheBuster cacheBuster = _cacheBusterResolver.GetCacheBuster(cacheBusterType);
                if (cacheBuster is not TimestampCacheBuster timestampCacheBuster || !timestampCacheBuster.TimestampBased)
                {
                    if (cacheBusterValue != cacheBuster.GetValue())
                    {
                        // We cannot let this continue, someone is trying to spoof the cache buster value,
                        // which can lead to lots of arbitrary files being created on the server.
                        _logger.LogWarning(
                            "An invalid cache buster value {cacheBusterValue} was detected for the bundle {bundleName} which was not produced by the registered cache buster type {cacheBusterType}",
                            cacheBusterValue,
                            bundleModel.Bundle.Name,
                            cacheBusterType);
                        return Results.BadRequest();
                    }
                }

                using var bundleContext = new BundleContext(cacheBusterValue, bundleModel, cacheFilePath);

                var watch = new Stopwatch();
                watch.Start();
                _logger.LogDebug($"Processing bundle '{bundleModel.FileKey}', debug? {bundleModel.Debug} ...");

                //we need to do the minify on the original files
                foreach (IWebFile file in files)
                {
                    await _preProcessManager.ProcessAndCacheFileAsync(file, bundleOptions, bundleContext);
                }

                //Get each file path to it's hashed location since that is what the pre-processed file will be saved as
                IEnumerable<IFileInfo> fileInfos = files.Select(x => _fileSystem.CacheFileSystem.GetCacheFile(
                    x,
                    () => _fileSystem.GetRequiredFileInfo(x),
                    bundleOptions.FileWatchOptions.Enabled,
                    Path.GetExtension(x.FilePath),
                    cacheBusterValue,
                    out _));

                using Stream resultStream = await GetCombinedStreamAsync(fileInfos, bundleContext);

                //compress the response (if enabled)
                //do not compress anything if it's not enabled in the bundle options
                Stream compressedStream = await Compressor.CompressAsync(bundleOptions.CompressResult ? bundleModel.Compression : CompressionType.None,
                                                                      bundleOptions.CompressionLevel,
                                                                      resultStream);

                //save the resulting compressed file, if compression is not enabled it will just save the non compressed format
                // this persisted file will be used in the CheckNotModifiedEndpointFilter which will short circuit the request and return
                // the raw file if it exists for further requests to this path
                await CacheCompositeFileAsync(_fileSystem.CacheFileSystem, cacheFilePath, compressedStream);

                _logger.LogDebug($"Processed bundle '{bundleModel.FileKey}' in {watch.ElapsedMilliseconds}ms");

                //return the stream
                return Results.Stream(compressedStream, bundleModel.Mime);
            }
            finally
            {
                // Remove the lock from the dictionary and release the lock.
                if (s_locks.TryRemove(foundBundle.Name, out SemaphoreSlim lck))
                {
                    lck.Release();
                }
            }
        }

        /// <summary>
        /// Handles requests for composite files (non-named bundles)
        /// </summary>
        public async Task<IResult> Composite(CompositeFileModel file)
        {
            if (!file.IsBundleFound || !file.ParsedPath.Names.Any())
            {
                return Results.NotFound();
            }

            string cacheBusterValue = file.ParsedPath.CacheBusterValue;
            IFileInfo cacheFile = _fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, file.Compression, file.FileKey, out string cacheFilePath);
            if (cacheFile.Exists)
            {
                // this is already processed, return it
                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    // If physical path is available then it's the physical file system, in which case we'll deliver the file with the physical file result
                    // which uses IHttpSendFileFeature which is a native host option for sending static files
                    return Results.File(cacheFile.PhysicalPath, file.Mime);
                }
                else
                {
                    return Results.Stream(cacheFile.CreateReadStream(), file.Mime);
                }
            }

            // Validate the cache buster in the case where the file wasn't eagerly created by the view,
            // and the request is coming in directly to the handler.
            Type cacheBusterType = _bundleManager.GetDefaultBundleOptions(file.Debug).GetCacheBusterType();
            ICacheBuster cacheBuster = _cacheBusterResolver.GetCacheBuster(cacheBusterType);
            if (cacheBuster is not TimestampCacheBuster timestampCacheBuster || !timestampCacheBuster.TimestampBased)
            {
                if (cacheBusterValue != cacheBuster.GetValue())
                {
                    // We cannot let this continue, someone is trying to spoof the cache buster value,
                    // which can lead to lots of arbitrary files being created on the server.
                    _logger.LogWarning(
                        "An invalid cache buster value {cacheBusterValue} was detected for the composite file {compositeFile} which was not produced by the registered cache buster type {cacheBusterType}",
                        cacheBusterValue,
                        cacheFilePath,
                        cacheBusterType);
                    return Results.BadRequest();
                }
            }

            using var bundleContext = new BundleContext(cacheBusterValue, file, cacheFilePath);

            // Resolve each requested file from the cache without throwing. The composite URL contains client
            // supplied file hashes, so a stale cache (e.g. after an app restart when using the in-memory cache)
            // or a deliberately malformed request can reference files that don't exist. Previously this threw a
            // FileNotFoundException which surfaced as an unhandled 500 and could be triggered repeatedly (a DoS
            // vector - see issue #199). Instead we return a graceful 404 when any requested file is missing.
            var files = new List<IFileInfo>(file.ParsedPath.Names.Count());
            foreach (var filePath in file.ParsedPath.Names)
            {
                var fileInfo = _fileSystem.CacheFileSystem.GetFileInfo(
                    $"{file.ParsedPath.CacheBusterValue}/{filePath + file.Extension}");

                if (!fileInfo.Exists)
                {
                    _logger.LogWarning(
                        "The requested composite file {CompositeFile} references a file {FilePath} that does not exist in the cache. Returning 404.",
                        cacheFilePath,
                        filePath);
                    return Results.NotFound();
                }

                files.Add(fileInfo);
            }

            using Stream resultStream = await GetCombinedStreamAsync(files, bundleContext);
            Stream compressedStream = await Compressor.CompressAsync(file.Compression, resultStream);

            await CacheCompositeFileAsync(_fileSystem.CacheFileSystem, cacheFilePath, compressedStream);

            return Results.Stream(compressedStream, file.Mime);
        }

        private bool TryGetBundle(BundleRequestModel bundleModel, out IResult result, out string cacheFilePath)
        {
            // TODO: Here or further internally we need to validate the arbitrary value.
            string cacheBusterValue = bundleModel.ParsedPath.CacheBusterValue;

            //now we need to determine if this bundle has already been created
            IFileInfo cacheFile = _fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, bundleModel.Compression, bundleModel.FileKey, out cacheFilePath);
            if (cacheFile.Exists)
            {
                _logger.LogDebug($"Returning bundle '{bundleModel.FileKey}' from cache");


                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the physical file result
                    //which uses IHttpSendFileFeature which is a native host option for sending static files
                    result = Results.File(cacheFile.PhysicalPath, bundleModel.Mime);
                    return true;
                }
                else
                {
                    result = Results.Stream(cacheFile.CreateReadStream(), bundleModel.Mime);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static async Task CacheCompositeFileAsync(ICacheFileSystem cacheProvider, string filePath, Stream compositeStream)
        {
            await cacheProvider.WriteFileAsync(filePath, compositeStream);
            if (compositeStream.CanSeek)
            {
                compositeStream.Position = 0;
            }
        }

        /// <summary>
        /// Combines files into a single stream
        /// </summary>
        private async Task<Stream> GetCombinedStreamAsync(IEnumerable<IFileInfo> files, BundleContext bundleContext)
        {
            //TODO: Here we need to be able to prepend/append based on a "BundleContext" (or similar)

            List<Stream> inputs = null;
            try
            {
                inputs = files.Where(x => x.Exists)
                    .Select(x => x.CreateReadStream())
                    .ToList();

                string delimeter = bundleContext.BundleRequest.Extension == ".js" ? ";\n" : "\n";
                Stream combined = await bundleContext.GetCombinedStreamAsync(inputs, delimeter);
                return combined;
            }
            finally
            {
                if (inputs != null)
                {
                    foreach (Stream input in inputs)
                    {
                        input.Dispose();
                    }
                }
            }
        }
    }
}
