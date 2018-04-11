using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Smidge.CompositeFiles;
using Smidge.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Smidge.Cache;
using Smidge.FileProcessors;
using Smidge.Hashing;

namespace Smidge.Controllers
{

    /// <summary>
    /// Controller for handling minified/combined responses
    /// </summary>    
    [AddCompressionHeader(Order = 0)]
    [AddExpiryHeaders(Order = 1)]
    [CheckNotModified(Order = 2)]
    [CompositeFileCacheFilter(Order = 3)]        
    public class SmidgeController : Controller
    {
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IBundleManager _bundleManager;
        private readonly IBundleFileSetGenerator _fileSetGenerator;
        private readonly PreProcessPipelineFactory _processorFactory;
        private readonly PreProcessManager _preProcessManager;
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
            FileSystemHelper fileSystemHelper, 
            IBundleManager bundleManager,
            IBundleFileSetGenerator fileSetGenerator,
            PreProcessPipelineFactory processorFactory,
            PreProcessManager preProcessManager,
            ILogger<SmidgeController> logger)
        {
            if (fileSystemHelper == null) throw new ArgumentNullException(nameof(fileSystemHelper));
            if (bundleManager == null) throw new ArgumentNullException(nameof(bundleManager));
            if (fileSetGenerator == null) throw new ArgumentNullException(nameof(fileSetGenerator));
            if (processorFactory == null) throw new ArgumentNullException(nameof(processorFactory));
            if (preProcessManager == null) throw new ArgumentNullException(nameof(preProcessManager));
            _fileSystemHelper = fileSystemHelper;
            _bundleManager = bundleManager;
            _fileSetGenerator = fileSetGenerator;
            _processorFactory = processorFactory;
            _preProcessManager = preProcessManager;
            _logger = logger;
        }

        /// <summary>
        /// Handles requests for named bundles
        /// </summary>
        /// <param name="bundle">The bundle model</param>
        /// <returns></returns>       
        public async Task<FileResult> Bundle(
            [FromServices]BundleRequestModel bundle)
        {
            if (!_bundleManager.TryGetValue(bundle.FileKey, out Bundle foundBundle))
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }

            var bundleOptions = foundBundle.GetBundleOptions(_bundleManager, bundle.Debug);
            
            //now we need to determine if this bundle has already been created
            var compositeFileInfo = _fileSystemHelper.GetCompositeFileInfo(bundle.CacheBuster, bundle.Compression, bundle.FileKey);
            if (compositeFileInfo.Exists)
            {
                _logger.LogDebug($"Returning bundle '{bundle.FileKey}' from cache");

                //this is already processed, return it
                return File(compositeFileInfo.CreateReadStream(), bundle.Mime);
            }

            //the bundle doesn't exist so we'll go get the files, process them and create the bundle
            //TODO: We should probably lock here right?! we don't want multiple threads trying to do this at the same time

            //get the files for the bundle
            var files = _fileSetGenerator.GetOrderedFileSet(foundBundle,
                    _processorFactory.CreateDefault(
                        //the file type in the bundle will always be the same
                        foundBundle.Files[0].DependencyType))
                .ToArray();

            if (files.Length == 0)
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }
            
            using (var bundleContext = new BundleContext(bundle, compositeFileInfo))
            {
                var watch = new Stopwatch();
                watch.Start();
                _logger.LogDebug($"Processing bundle '{bundle.FileKey}', debug? {bundle.Debug} ...");

                //we need to do the minify on the original files
                foreach (var file in files)
                {
                    await _preProcessManager.ProcessAndCacheFileAsync(file, bundleOptions, bundleContext);
                }

                //Get each file path to it's hashed location since that is what the pre-processed file will be saved as
                Lazy<IFileInfo> fi;
                var filePaths = files.Select(
                    x => _fileSystemHelper.GetCacheFile(x, bundleOptions.FileWatchOptions.Enabled, bundle.Extension, bundle.CacheBuster, out fi));

                using (var resultStream = await GetCombinedStreamAsync(filePaths, bundleContext))
                {
                    //compress the response (if enabled)
                    var compressedStream = await Compressor.CompressAsync(
                        //do not compress anything if it's not enabled in the bundle options
                        bundleOptions.CompressResult ? bundle.Compression : CompressionType.none,
                        resultStream);

                    //save the resulting compressed file, if compression is not enabled it will just save the non compressed format
                    // this persisted file will be used in the CheckNotModifiedAttribute which will short circuit the request and return
                    // the raw file if it exists for further requests to this path
                    await CacheCompositeFileAsync(compositeFileInfo, compressedStream);

                    _logger.LogDebug($"Processed bundle '{bundle.FileKey}' in {watch.ElapsedMilliseconds}ms");

                    //return the stream
                    return File(compressedStream, bundle.Mime);
                }
            }
        }

        /// <summary>
        /// Handles requests for composite files (non-named bundles)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<FileResult> Composite(
             [FromServices]CompositeFileModel file)
        {
            if (!file.ParsedPath.Names.Any())
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }

            var compositeFilePath = _fileSystemHelper.GetCompositeFileInfo(file.CacheBuster, file.Compression, file.FileKey);

            if (compositeFilePath.Exists)
            {
                //this is already processed, return it
                return File(compositeFilePath.CreateReadStream(), file.Mime);
            }

            //this bundle context isn't really used since this is not a bundle but just a composite file which doesn't support all of the features of a real bundle
            using (var bundleContext = BundleContext.CreateEmpty())
            {
                var filePaths = file.ParsedPath.Names.Select(filePath =>
                    _fileSystemHelper.CacheFileProvider.GetFileInfo(filePath + file.Extension));
                
                using (var resultStream = await GetCombinedStreamAsync(filePaths, bundleContext))
                {
                    var compressedStream = await Compressor.CompressAsync(file.Compression, resultStream);

                    await CacheCompositeFileAsync(compositeFilePath, compressedStream);

                    return File(compressedStream, file.Mime);
                }
            }
        }

        private static async Task CacheCompositeFileAsync(IFileInfo compositeFileInfo, Stream compositeStream)
        {
            //ensure it exists
            Directory.CreateDirectory(Path.GetDirectoryName(compositeFileInfo.PhysicalPath));
            
            compositeStream.Position = 0;
            using (var fs = System.IO.File.Create(compositeFileInfo.PhysicalPath))
            {
                await compositeStream.CopyToAsync(fs);
            }
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

                var combined = await bundleContext.GetCombinedStreamAsync(inputs);
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