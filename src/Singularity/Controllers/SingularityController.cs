using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Singularity.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Singularity.Controllers
{
    public class SingularityController : Controller
    {
        private SingularityConfig _config;
        private IHostingEnvironment _env;

        public SingularityController(IHostingEnvironment env, SingularityConfig config)
        {
            _env = env;
            _config = config;
        }

        public async Task<FileStreamResult> Base64(string s, IDependentFileType t, string v)
        {
            var fileset = Uri.UnescapeDataString(s);

            //get the file list
            string[] filePaths = fileset.DecodeFrom64Url().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var resultStream = await GetCombinedStreamAsync(filePaths);

            return new FileStreamResult(resultStream, "text/javascript");
        }

        public async Task<MemoryStream> GetCombinedStreamAsync(IEnumerable<string> filePaths)
        {
            var token = CancellationToken.None;
            var ms = new MemoryStream();
            foreach (var file in filePaths)
            {
                var filePath = Path.Combine(_env.WebRoot, file).Replace("/", "\\");
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