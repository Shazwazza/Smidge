using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smidge.Models;

namespace Smidge.Controllers
{
    /// <summary>
    /// Checks the file system for an already persisted minified, combined, compressed file for the
    /// request definition. If there is one it returns that file directly and the endpoint handler does not execute.
    /// </summary>
    /// <remarks>
    /// This is the inner-most endpoint filter so that its short-circuit behaviour is equivalent to the
    /// previous MVC action filter that had the highest <c>Order</c>.
    /// </remarks>
    internal sealed class CompositeFileCacheEndpointFilter : IEndpointFilter
    {
        private readonly ISmidgeFileSystem _fileSystem;

        public CompositeFileCacheEndpointFilter(ISmidgeFileSystem fileSystem)
            => _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            if (context.Arguments.OfType<RequestModel>().FirstOrDefault() is RequestModel file && file.IsBundleFound)
            {
                var cacheBusterValue = file.ParsedPath.CacheBusterValue;

                if (TryGetCachedCompositeFileResult(_fileSystem, cacheBusterValue, file.FileKey, file.Compression, file.Mime, out IResult result, out DateTime lastWrite))
                {
                    file.LastFileWriteTime = lastWrite;

                    // short-circuit: return the cached file without invoking the handler
                    return result;
                }
            }

            return await next(context);
        }

        internal static bool TryGetCachedCompositeFileResult(ISmidgeFileSystem fileSystem, string cacheBusterValue, string filesetKey, CompressionType type, string mime, out IResult result, out DateTime lastWriteTime)
        {
            result = null;

            var cacheFile = fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, type, filesetKey, out _);
            if (cacheFile.Exists)
            {
                lastWriteTime = cacheFile.LastModified.DateTime;

                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with a physical file result
                    //which uses IHttpSendFileFeature which is a native host option for sending static files
                    result = Results.File(cacheFile.PhysicalPath, mime);
                    return true;
                }

                //deliver the file via stream
                result = Results.Stream(cacheFile.CreateReadStream(), mime);
                return true;
            }

            lastWriteTime = DateTime.Now;
            return false;
        }
    }
}
