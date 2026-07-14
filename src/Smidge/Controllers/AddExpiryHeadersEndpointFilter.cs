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
    /// Adds the correct caching expiry headers when the request is not in debug
    /// </summary>
    internal sealed class AddExpiryHeadersEndpointFilter : IEndpointFilter
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
}
