using Microsoft.AspNetCore.Http;
using Smidge.Cache;
using Smidge.Models;

namespace Smidge.Nuglify
{
    /// <summary>
    /// Handles requests for Nuglify generated source map files.
    /// </summary>
    /// <remarks>
    /// This was previously an MVC controller. For Smidge 5 it is a lightweight POCO handler invoked directly
    /// from a minimal API endpoint.
    /// </remarks>
    public sealed class NuglifySourceMapHandler
    {
        private readonly ISmidgeFileSystem _fileSystem;

        public NuglifySourceMapHandler(ISmidgeFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IResult SourceMap(BundleRequestModel bundle)
        {
            if (!bundle.IsBundleFound)
            {
                return Results.NotFound();
            }

            var sourceMapFile = _fileSystem.CacheFileSystem.GetRequiredFileInfo(bundle.GetSourceMapFilePath());

            if (sourceMapFile.Exists)
            {
                if (!string.IsNullOrWhiteSpace(sourceMapFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the physical file result
                    //which uses IHttpSendFileFeature which is a native host option for sending static files
                    return Results.File(sourceMapFile.PhysicalPath, "application/json");
                }
                else
                {
                    return Results.Stream(sourceMapFile.CreateReadStream(), "application/json");
                }
            }

            return Results.NotFound();
        }
    }
}
