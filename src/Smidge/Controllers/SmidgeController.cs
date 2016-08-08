using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Smidge.CompositeFiles;
using Smidge.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IHasher _hasher;
        private readonly BundleManager _bundleManager;     

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileSystemHelper"></param>
        /// <param name="hasher"></param>
        /// <param name="bundleManager"></param>
        public SmidgeController(
            FileSystemHelper fileSystemHelper, 
            IHasher hasher, 
            BundleManager bundleManager)
        {
            _hasher = hasher;
            _fileSystemHelper = fileSystemHelper;
            _bundleManager = bundleManager;
        }

        /// <summary>
        /// Handles requests for bundles
        /// </summary>
        /// <param name="bundle">The bundle model</param>
        /// <returns></returns>       
        public async Task<FileResult> Bundle(
            [FromServices]BundleRequestModel bundle)
        {  
            var found = _bundleManager.GetFiles(bundle.FileKey);
            if (found == null || !found.Any())
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }

            //need to convert each file path to it's hash since that is what the minified file will be saved as                    
            var filePaths = found.Select(file =>
                Path.Combine(
                    _fileSystemHelper.CurrentCacheFolder,
                    _hasher.Hash(file.FilePath) + bundle.Extension));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(bundle.Compression, resultStream);

                var compositeFilePath = await CacheCompositeFileAsync(bundle.FileKey, compressedStream, bundle.Compression);

                return File(compressedStream, bundle.Mime);
            }
        }

        /// <summary>
        /// Handles requests for composite files
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

                var compositeFilePath = await CacheCompositeFileAsync(file.FileKey, compressedStream, file.Compression);
                
                return File(compressedStream, file.Mime);
            }

        }

        private async Task<string> CacheCompositeFileAsync(string filesetKey, Stream compositeStream, CompressionType type)
        {
            var folder = _fileSystemHelper.GetCurrentCompositeFolder(type);
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
            var ms = new MemoryStream();
            foreach (var filePath in filePaths)
            {
                if (System.IO.File.Exists(filePath))
                {
                    using (var fileStream = System.IO.File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(ms);
                    }
                }
            }
            //ensure it's reset
            ms.Position = 0;
            return ms;
        }

        
    }


}