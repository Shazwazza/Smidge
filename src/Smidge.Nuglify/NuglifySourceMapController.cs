using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smidge.Cache;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.Nuglify
{
    public class NuglifySourceMapController : Controller
    {
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IBundleManager _bundleManager;

        public NuglifySourceMapController(FileSystemHelper fileSystemHelper, IBundleManager bundleManager)
        {
            _fileSystemHelper = fileSystemHelper;
            _bundleManager = bundleManager;
        }

        public FileResult SourceMap([FromServices] BundleRequestModel bundle)
        {
            Bundle foundBundle;
            if (!_bundleManager.TryGetValue(bundle.FileKey, out foundBundle))
            {
                //TODO: Throw an exception, this will result in an exception anyways
                return null;
            }

            //now we need to determine if this bundle has already been created
            var compositeFilePath = new FileInfo(_fileSystemHelper.GetCurrentCompositeFilePath(bundle.CacheBuster, bundle.Compression, bundle.FileKey));
            //we need to go one level above the composite path into the non-compression named folder since the map request will always be 'none' compression
            var mapPath = new FileInfo(Path.Combine(compositeFilePath.Directory.Parent.FullName, compositeFilePath.Name + ".map"));
            if (mapPath.Exists)
            {
                //this should already be processed if this is being requested!
                return File(mapPath.OpenRead(), "application/json");
            }

            //TODO: Throw an exception, this will result in an exception anyways
            return null;
        }

        
    }
}