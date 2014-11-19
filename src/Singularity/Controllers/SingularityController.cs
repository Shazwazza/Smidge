using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Runtime;
using Singularity.CompositeFiles;
using Singularity.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Singularity.Controllers
{
    /// <summary>
    /// Controller for handling minified/combined responses
    /// </summary>
    public class SingularityController : Controller
    {
        private SingularityConfig _config;
        private IApplicationEnvironment _env;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="env"></param>
        /// <param name="config"></param>
        public SingularityController(IApplicationEnvironment env, SingularityConfig config)
        {
            _env = env;
            _config = config;
        }

        /// <summary>
        /// Handles Base64 encoded requests
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public async Task<FileResult> Base64(string s, IDependentFileType t, string v)
        {
            var compression = Context.GetClientCompression();

            var fileset = Uri.UnescapeDataString(s);

            //get the file list
            var filePaths = fileset.DecodeFrom64Url().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(filePath => Path.Combine(GetCurrentCacheFolder(), filePath));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(compression, resultStream);
                return File(compressedStream, "text/javascript");
            }
        }

        /// <summary>
        /// Handles delimited requests
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public async Task<FileResult> Delimited(string s, IDependentFileType t, string v)
        {
            var compression = Context.GetClientCompression();

            var fileset = Uri.UnescapeDataString(s);
            var filesetKey = fileset.GenerateHash();

            FileResult result;
            if (TryGetCachedCompositeFileResult(filesetKey, compression, out result))
            {
                return result;
            }

            //get the file list
            var filePaths = fileset.Split(new[] { ".js" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(filePath => Path.Combine(GetCurrentCacheFolder(), filePath + ".js"));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(compression, resultStream);

                await CacheCompositeFileAsync(filesetKey, compressedStream, compression);

                return File(compressedStream, "text/javascript");
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

        private bool TryGetCachedCompositeFileResult(string filesetKey, CompressionType type, out FileResult result)
        {
            result = null;
            var filesetPath = Path.Combine(GetCurrentCompositeFolder(type), filesetKey + ".s");
            if (System.IO.File.Exists(filesetPath))
            {
                result = File(filesetPath, "text/javascript");
                return true;
            }
            return false;
        }

        private async Task CacheCompositeFileAsync(string filesetKey, Stream compositeStream, CompressionType type)
        {
            var folder = GetCurrentCompositeFolder(type);
            Directory.CreateDirectory(folder);
            compositeStream.Position = 0;
            using (var fs = System.IO.File.Create(Path.Combine(folder, filesetKey + ".s")))
            {
                await compositeStream.CopyToAsync(fs);
            }
            compositeStream.Position = 0;
        }

        /// <summary>
        /// Returns the cache folder for composite files for the current compression supported
        /// </summary>
        /// <returns></returns>
        private string GetCurrentCompositeFolder(CompressionType type)
        {
            return Path.Combine(GetCurrentCacheFolder(), type.ToString());
        }

        /// <summary>
        /// The current cache folder for the current version
        /// </summary>
        /// <returns></returns>
        private string GetCurrentCacheFolder()
        {
            return Path.Combine(_env.ApplicationBasePath, _config.DataFolder, "Cache", _config.ServerName, _config.Version);
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