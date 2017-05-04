using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Smidge.CompositeFiles;
using Smidge.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileSystemHelper"></param>
        /// <param name="bundleManager"></param>
        /// <param name="fileSetGenerator"></param>
        /// <param name="processorFactory"></param>
        /// <param name="preProcessManager"></param>
        public SmidgeController(
            FileSystemHelper fileSystemHelper, 
            IBundleManager bundleManager,
            IBundleFileSetGenerator fileSetGenerator,
            PreProcessPipelineFactory processorFactory,
            PreProcessManager preProcessManager)
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
        }

        /// <summary>
        /// Handles requests for named bundles
        /// </summary>
        /// <param name="bundle">The bundle model</param>
        /// <returns></returns>       
        public async Task<FileResult> Bundle(
            [FromServices]BundleRequestModel bundle)
        {
            Bundle foundBundle;
            if (!_bundleManager.TryGetValue(bundle.FileKey, out foundBundle))
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }

            var bundleOptions = foundBundle.GetBundleOptions(_bundleManager, bundle.Debug);
            
            //now we need to determine if this bundle has already been created
            var compositeFilePath = new FileInfo(_fileSystemHelper.GetCurrentCompositeFilePath(bundle.CacheBuster, bundle.Compression, bundle.FileKey));
            if (compositeFilePath.Exists)
            {
                //this is already processed, return it
                return File(compositeFilePath.OpenRead(), bundle.Mime);
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
            
            using (var bundleContext = new BundleContext(bundle, compositeFilePath))
            {
                //we need to do the minify on the original files
                foreach (var file in files)
                {
                    await _preProcessManager.ProcessAndCacheFileAsync(file, bundleOptions, bundleContext);
                }

                //Get each file path to it's hashed location since that is what the pre-processed file will be saved as
                Lazy<IFileInfo> fi;
                var filePaths = files.Select(
                    x => _fileSystemHelper.GetCacheFilePath(x, bundleOptions.FileWatchOptions.Enabled, bundle.Extension, bundle.CacheBuster, out fi));

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
                    await CacheCompositeFileAsync(compositeFilePath, compressedStream);

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

            var compositeFilePath = new FileInfo(_fileSystemHelper.GetCurrentCompositeFilePath(file.CacheBuster, file.Compression, file.FileKey));

            if (compositeFilePath.Exists)
            {
                //this is already processed, return it
                return File(compositeFilePath.OpenRead(), file.Mime);
            }

            //this bundle context isn't really used since this is not a bundle but just a composite file which doesn't support all of the features of a real bundle
            using (var bundleContext = BundleContext.CreateEmpty())
            {
                var filePaths = file.ParsedPath.Names.Select(filePath =>
                    Path.Combine(
                        _fileSystemHelper.CurrentCacheFolder,
                        filePath + file.Extension));

                using (var resultStream = await GetCombinedStreamAsync(filePaths, bundleContext))
                {
                    var compressedStream = await Compressor.CompressAsync(file.Compression, resultStream);

                    await CacheCompositeFileAsync(compositeFilePath, compressedStream);

                    return File(compressedStream, file.Mime);
                }
            }
        }

        private static async Task CacheCompositeFileAsync(FileInfo compositeFilePath, Stream compositeStream)
        {
            //ensure it exists
            compositeFilePath.Directory.Create();            
            compositeStream.Position = 0;            
            using (var fs = compositeFilePath.Create())
            {
                await compositeStream.CopyToAsync(fs);
            }
            compositeStream.Position = 0;
        }

        /// <summary>
        /// Combines files into a single stream
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="bundleContext"></param>
        /// <returns></returns>
        private async Task<Stream> GetCombinedStreamAsync(IEnumerable<string> filePaths, BundleContext bundleContext)
        {
            //TODO: Here we need to be able to prepend/append based on a "BundleContext" (or similar)

            List<Stream> inputs = null;
            try
            {
                inputs = filePaths.Where(System.IO.File.Exists)
                    .Select(System.IO.File.OpenRead)
                    .Cast<Stream>()
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