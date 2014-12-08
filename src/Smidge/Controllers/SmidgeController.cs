using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smidge.Controllers
{

    internal class CompositeFileStreamResult : FileStreamResult
    {

        public CompositeFileStreamResult(string compFilePath, Stream fileStream, string contentType)
            : base(fileStream, contentType)
        {
            CompositeFilePath = compFilePath;
        }

        public string CompositeFilePath { get; private set; }
    }

    /// <summary>
    /// Controller for handling minified/combined responses
    /// </summary>
    [AddCompressionHeader]
    public class SmidgeController : Controller
    {
        private ISmidgeConfig _config;
        private IApplicationEnvironment _env;
        private FileSystemHelper _fileSystemHelper;
        private IHasher _hasher;
        private BundleManager _bundleManager;
        private IUrlManager _urlManager;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="env"></param>
        /// <param name="config"></param>
        public SmidgeController(
            IApplicationEnvironment env, 
            ISmidgeConfig config, 
            FileSystemHelper fileSystemHelper, 
            IHasher hasher, 
            BundleManager bundleManager,
            IUrlManager urlManager)
        {
            _urlManager = urlManager;
            _hasher = hasher;
            _env = env;
            _config = config;
            _fileSystemHelper = fileSystemHelper;
            _bundleManager = bundleManager;
        }

        /// <summary>
        /// Handles requests for bundles
        /// </summary>
        /// <param name="bundle">The bundle model</param>
        /// <returns></returns>
        public async Task<FileResult> Bundle(
            [FromServices]BundleModel bundle)
        {           
            //Check if it's already processed and return it
            FileResult result;
            if (TryGetCachedCompositeFileResult(bundle.BundleName, bundle.Compression, bundle.Mime, out result))
            {
                return result;
            }

            var found = _bundleManager.GetFiles(bundle.BundleName, Request);
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

                var compositeFilePath = await CacheCompositeFileAsync(bundle.BundleName, compressedStream, bundle.Compression);

                return new CompositeFileStreamResult(compositeFilePath, compressedStream, bundle.Mime);
            }
        }

        /// <summary>
        /// Handles requests for composite files
        /// </summary>
        /// <param name="s">The file key to lookup</param>
        /// <param name="t">The type of file</param>
        /// <param name="v">The version</param>
        /// <returns></returns>
        public async Task<FileResult> Composite(string id)
        {
            var compression = Request.GetClientCompression();

            var parsed = _urlManager.ParsePath(id);

            //Creates a single hash of the full url (which can include many files)
            var filesetKey = _hasher.Hash(string.Join(".", parsed.Names));

            string mime;
            string ext;
            switch (parsed.WebType)
            {
                case WebFileType.Js:
                    ext = ".js";
                    mime = "text/javascript";
                    break;
                case WebFileType.Css:
                default:
                    ext = ".css";
                    mime = "text/css";
                    break;
            }

            //Check if it's already processed and return it
            FileResult result;
            if (TryGetCachedCompositeFileResult(filesetKey, compression, mime, out result))
            {
                return result;
            }

            //get the file list from the fileset string, remember, each of the files listed here
            // is a path to it's already minified version since that is done during file rendering

            if (!parsed.Names.Any())
            {
                //is null right here??
                return null;
            }

            var filePaths = parsed.Names.Select(filePath =>
                Path.Combine(
                    _fileSystemHelper.CurrentCacheFolder,
                    filePath + ext));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(compression, resultStream);

                var compositeFilePath = await CacheCompositeFileAsync(filesetKey, compressedStream, compression);

                return new CompositeFileStreamResult(compositeFilePath, compressedStream, mime);
            }

        }
             

        private bool TryGetCachedCompositeFileResult(string filesetKey, CompressionType type, string mime, out FileResult result)
        {
            result = null;
            var filesetPath = _fileSystemHelper.GetCurrentCompositeFilePath(type, filesetKey);
            if (System.IO.File.Exists(filesetPath))
            {
                result = File(filesetPath, mime);
                return true;
            }
            return false;
        }

        //TODO: This needs to return the composite file name so we can store it with the file result
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