using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
    internal sealed class NuglifySourceMapHandler
    {
        private readonly ISmidgeFileSystem _fileSystem;
        private readonly ILogger<NuglifySourceMapHandler> _logger;

        public NuglifySourceMapHandler(ISmidgeFileSystem fileSystem, ILogger<NuglifySourceMapHandler> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IResult SourceMap(BundleRequestModel bundle)
        {
            if (!bundle.IsBundleFound)
            {
                return Results.NotFound();
            }

            // Look up the source map without throwing. A source map is only produced for bundles that were
            // actually minified (e.g. not for files already named *.min.*), and the browser typically requests
            // it lazily (when dev tools are opened) which can be well after the bundle was created. In all of
            // those cases the map may legitimately be absent, so we must return a 404 rather than letting the
            // file system throw a FileNotFoundException that surfaces as an unhandled 500. See issues #199 / #185.
            var sourceMapFilePath = bundle.GetSourceMapFilePath();
            var sourceMapFile = _fileSystem.CacheFileSystem.GetFileInfo(sourceMapFilePath);

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

            _logger.LogDebug("No source map exists for bundle {Bundle} at cache path {SourceMapPath}", bundle.FileKey, sourceMapFilePath);
            return Results.NotFound();
        }
    }
}
