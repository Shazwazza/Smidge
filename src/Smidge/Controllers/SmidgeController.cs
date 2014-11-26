using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Runtime;
using Smidge.CompositeFiles;
using Smidge.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smidge.Controllers
{
    /// <summary>
    /// Controller for handling minified/combined responses
    /// </summary>
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

        public async Task<FileResult> Bundle(string id)
        {
            var compression = Context.GetClientCompression();

            var parsed = _urlManager.ParsePath(id);

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

            if (!parsed.Names.Any())
            {
                //is null right here??
                return null;
            }

            //it's a bundle, we treat this differently
            var found = _bundleManager.GetFiles(parsed.Names.Single());
            if (found == null || !found.Any())
            {
                //is null right here??
                return null;
            }

            //need to convert each file path to it's hash since that is what the minified file will be saved as                    
            var filePaths = found.Select(file =>
                Path.Combine(
                    _fileSystemHelper.CurrentCacheFolder,
                    _hasher.Hash(file.FilePath) + ext));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(compression, resultStream);

                await CacheCompositeFileAsync(parsed.Names.Single(), compressedStream, compression);

                return File(compressedStream, mime);
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
            var compression = Context.GetClientCompression();

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

                await CacheCompositeFileAsync(filesetKey, compressedStream, compression);

                return File(compressedStream, mime);
            }

        }
        
        /// <summary>
        /// Adds the compression headers
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            context.HttpContext.AddCompressionResponseHeader(Context.GetClientCompression());
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

        private async Task CacheCompositeFileAsync(string filesetKey, Stream compositeStream, CompressionType type)
        {
            var folder = _fileSystemHelper.GetCurrentCompositeFolder(type);
            Directory.CreateDirectory(folder);
            compositeStream.Position = 0;
            using (var fs = System.IO.File.Create(Path.Combine(folder, filesetKey + ".s")))
            {
                await compositeStream.CopyToAsync(fs);
            }
            compositeStream.Position = 0;
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