using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smidge.Hashing;
using Smidge.Models;
using Smidge.Options;

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
    public sealed class CompositeFileCacheEndpointFilter : IEndpointFilter
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

    /// <summary>
    /// Checks the request headers to see if the response has been modified, if it has not a 304 is returned and the request is short circuited
    /// </summary>
    public sealed class CheckNotModifiedEndpointFilter : IEndpointFilter
    {
        private readonly IHasher _hasher;

        public CheckNotModifiedEndpointFilter(IHasher hasher)
            => _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));

        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);

            if (context.Arguments.OfType<RequestModel>().FirstOrDefault() is RequestModel file && file.IsBundleFound)
            {
                //Don't execute when the request is in Debug
                if (file.Debug)
                    return result;

                var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);

                var request = context.HttpContext.Request;
                var isDifferent = request.HasETagBeenModified(etag);
                var hasChanged = request.HasRequestBeenModifiedSince(file.LastFileWriteTime.ToUniversalTime());
                if (!isDifferent || !hasChanged)
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Adds the correct caching expiry headers when the request is not in debug
    /// </summary>
    public sealed class AddExpiryHeadersEndpointFilter : IEndpointFilter
    {
        private readonly IHasher _hasher;
        private readonly IBundleManager _bundleManager;

        public AddExpiryHeadersEndpointFilter(IHasher hasher, IBundleManager bundleManager)
        {
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
        }

        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);

            if (context.Arguments.OfType<RequestModel>().FirstOrDefault() is not RequestModel file || !file.IsBundleFound)
                return result;

            var enableETag = true;
            var cacheControlMaxAge = 10 * 24; //10 days

            BundleOptions bundleOptions;

            if (_bundleManager.TryGetValue(file.FileKey, out Bundle b))
            {
                bundleOptions = b.GetBundleOptions(_bundleManager, file.Debug);
            }
            else
            {
                bundleOptions = file.Debug ? _bundleManager.DefaultBundleOptions.DebugOptions : _bundleManager.DefaultBundleOptions.ProductionOptions;
            }

            if (bundleOptions != null)
            {
                enableETag = bundleOptions.CacheControlOptions.EnableETag;
                cacheControlMaxAge = bundleOptions.CacheControlOptions.CacheControlMaxAge;
            }

            var response = context.HttpContext.Response;

            if (enableETag)
            {
                var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);
                response.AddETagResponseHeader(etag);
            }

            if (cacheControlMaxAge > 0)
            {
                response.AddCacheControlResponseHeader(cacheControlMaxAge);
                response.AddLastModifiedResponseHeader(file);
                response.AddExpiresResponseHeader(cacheControlMaxAge);
            }

            return result;
        }
    }

    /// <summary>
    /// Adds the compression headers
    /// </summary>
    public sealed class AddCompressionHeaderEndpointFilter : IEndpointFilter
    {
        private readonly IRequestHelper _requestHelper;
        private readonly IBundleManager _bundleManager;

        public AddCompressionHeaderEndpointFilter(IRequestHelper requestHelper, IBundleManager bundleManager)
        {
            _requestHelper = requestHelper ?? throw new ArgumentNullException(nameof(requestHelper));
            _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
        }

        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);

            if (context.Arguments.OfType<RequestModel>().FirstOrDefault() is RequestModel file && file.IsBundleFound)
            {
                var enableCompression = true;

                //check if it's a bundle (not composite file)
                if (file is BundleRequestModel bundleRequest && _bundleManager.TryGetValue(bundleRequest.FileKey, out var bundle))
                {
                    var bundleOptions = bundle.GetBundleOptions(_bundleManager, bundleRequest.Debug);
                    enableCompression = bundleOptions.CompressResult;
                }

                if (enableCompression)
                    context.HttpContext.Response.AddCompressionResponseHeader(_requestHelper.GetClientCompression(context.HttpContext.Request.Headers));
            }

            return result;
        }
    }
}
