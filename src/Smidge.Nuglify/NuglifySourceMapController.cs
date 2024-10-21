using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Smidge.Cache;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.Nuglify
{
    public class NuglifySourceMapController : Controller
    {
        private readonly ISmidgeFileSystem _fileSystem;
        private readonly IBundleManager _bundleManager;

        public NuglifySourceMapController(ISmidgeFileSystem fileSystem, IBundleManager bundleManager)
        {
            _fileSystem = fileSystem;
            _bundleManager = bundleManager;
        }

        public ActionResult SourceMap([FromServices] BundleRequestModel bundle)
        {
            if (!bundle.IsBundleFound)
            {
                return NotFound();
            }

            var sourceMapFile = _fileSystem.CacheFileSystem.GetRequiredFileInfo(bundle.GetSourceMapFilePath());

            if (sourceMapFile.Exists)
            {
                if (!string.IsNullOrWhiteSpace(sourceMapFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the PhysicalFileResult
                    //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files                    
                    return PhysicalFile(sourceMapFile.PhysicalPath, "application/json");
                }
                else
                {
                    return File(sourceMapFile.CreateReadStream(), "application/json");
                }
            }

            return NotFound();
        }

        
    }
}
