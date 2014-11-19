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
        public async Task<FileStreamResult> Base64(string s, IDependentFileType t, string v)
        {
            var fileset = Uri.UnescapeDataString(s);

            //get the file list
            var filePaths = fileset.DecodeFrom64Url().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(filePath => Path.Combine(_env.ApplicationBasePath, _config.DataFolder, "Cache", _config.ServerName, _config.Version, filePath));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(Context.GetClientCompression(), resultStream);
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
        public async Task<FileStreamResult> Delimited(string s, IDependentFileType t, string v)
        {
            var fileset = Uri.UnescapeDataString(s);

            //get the file list
            var filePaths = fileset.Split(new[] { ".js" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(filePath => Path.Combine(_env.ApplicationBasePath, _config.DataFolder, "Cache", _config.ServerName, _config.Version, filePath + ".js"));

            using (var resultStream = await GetCombinedStreamAsync(filePaths))
            {
                var compressedStream = await Compressor.CompressAsync(Context.GetClientCompression(), resultStream);
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

        /// <summary>
        /// Combines files into a single stream
        /// </summary>
        /// <param name="filePaths"></param>
        /// <returns></returns>
        private async Task<MemoryStream> GetCombinedStreamAsync(IEnumerable<string> filePaths)
        {
            var token = CancellationToken.None;
            var ms = new MemoryStream();
            foreach (var filePath in filePaths)
            {
                if (System.IO.File.Exists(filePath))
                {
                    using (var fileStream = System.IO.File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(ms, 0x1000, token);
                    }
                }
            }
            //ensure it's reset
            ms.Position = 0;
            return ms;
        }
    }
}