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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileSystemHelper"></param>
        /// <param name="bundleManager"></param>
        /// <param name="fileSetGenerator"></param>
        /// <param name="processorFactory"></param>
        public SmidgeController(
            FileSystemHelper fileSystemHelper, 
            IBundleManager bundleManager,
            IBundleFileSetGenerator fileSetGenerator,
            PreProcessPipelineFactory processorFactory)
        {
            if (fileSystemHelper == null) throw new ArgumentNullException(nameof(fileSystemHelper));
            if (bundleManager == null) throw new ArgumentNullException(nameof(bundleManager));
            if (fileSetGenerator == null) throw new ArgumentNullException(nameof(fileSetGenerator));
            if (processorFactory == null) throw new ArgumentNullException(nameof(processorFactory));
            _fileSystemHelper = fileSystemHelper;
            _bundleManager = bundleManager;
            _fileSetGenerator = fileSetGenerator;
            _processorFactory = processorFactory;
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

            //TODO: Check if the files have been processed, if not do this processing since we know the bundle name
            // this is possible (it is not possible for composite files)
            // see: https://github.com/Shazwazza/Smidge/issues/45

            var files = _fileSetGenerator.GetOrderedFileSet(foundBundle,
                _processorFactory.GetDefault(
                    //the file type in the bundle will always be the same
                    foundBundle.Files[0].DependencyType))
                .ToArray();

            if (files == null || files.Length == 0)
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }

            var bundleOptions = foundBundle.GetBundleOptions(_bundleManager, bundle.Debug);

            //Get each file path to it's hashed location since that is what the pre-processed file will be saved as
            Lazy<IFileInfo> fi;
            var filePaths = files.Select(
                x => _fileSystemHelper.GetCacheFilePath(x, bundleOptions.FileWatchOptions.Enabled, bundle.Extension, bundle.CacheBuster, out fi));
            
            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                //compress the response (if enabled)
                var compressedStream = await Compressor.CompressAsync(
                    //do not compress anything if it's not enabled in the bundle options
                    bundleOptions.CompressResult ? bundle.Compression : CompressionType.none, 
                    resultStream);

                //save the resulting compressed file, if compression is not enabled it will just save the non compressed format
                // this persisted file will be used in the CheckNotModifiedAttribute which will short circuit the request and return
                // the raw file if it exists for further requests to this path
                var compositeFilePath = await CacheCompositeFileAsync(bundle.CacheBuster, bundle.FileKey, compressedStream, bundle.Compression);

                //return the stream
                return File(compressedStream, bundle.Mime);
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

            var filePaths = file.ParsedPath.Names.Select(filePath =>
                Path.Combine(
                    _fileSystemHelper.CurrentCacheFolder,
                    filePath + file.Extension));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(file.Compression, resultStream);

                var compositeFilePath = await CacheCompositeFileAsync(file.CacheBuster, file.FileKey, compressedStream, file.Compression);
                
                return File(compressedStream, file.Mime);
            }

        }

        private async Task<string> CacheCompositeFileAsync(ICacheBuster cacheBuster, string filesetKey, Stream compositeStream, CompressionType type)
        {
            var folder = _fileSystemHelper.GetCurrentCompositeFolder(cacheBuster, type);
            Directory.CreateDirectory(folder);
            compositeStream.Position = 0;
            //TODO: Shouldn't this use: GetCurrentCompositeFilePath?
            var fileName = Path.Combine(folder, filesetKey + ".s");
            using (var fs = System.IO.File.Create(fileName))
            {
                await compositeStream.CopyToAsync(fs);
            }
            compositeStream.Position = 0;
            return fileName;
        }      

        /// <summary>
        /// Combines files into a single stream
        /// </summary>
        /// <param name="filePaths"></param>
        /// <returns></returns>
        private async Task<MemoryStream> GetCombinedStreamAsync(IEnumerable<string> filePaths)
        {
            //TODO: Should we use a buffer pool here?

            var semicolon = Encoding.UTF8.GetBytes(";");
            var ms = new MemoryStream();
            foreach (var filePath in filePaths)
            {
                if (System.IO.File.Exists(filePath))
                {
                    using (var fileStream = System.IO.File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(ms);                        
                    }
                    await ms.WriteAsync(semicolon, 0, semicolon.Length);
                }
            }
            //ensure it's reset
            ms.Position = 0;
            return ms;
        }

        
    }


}