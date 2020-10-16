using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Smidge.Hashing;

namespace Smidge.Controllers
{
    /// <summary>
    /// Adds the correct caching expiry headers when the request is not in debug
    /// </summary>
    public sealed class AddExpiryHeadersAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new AddExpiryHeaderFilter(
                serviceProvider.GetRequiredService<IHasher>(),
                serviceProvider.GetRequiredService<IBundleManager>());
        }

        /// <summary>
        /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
        /// can be reused across requests.
        /// </summary>
        public bool IsReusable => true;

        public sealed class AddExpiryHeaderFilter : IActionFilter
        {
            private readonly IHasher _hasher;
            private readonly IBundleManager _bundleManager;

            public AddExpiryHeaderFilter(IHasher hasher, IBundleManager bundleManager)
            {
                _hasher = hasher;
                _bundleManager = bundleManager;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (!context.ActionArguments.Any()) return;

                //put the model in the context, we'll resolve that after it's executed
                var file = context.ActionArguments.First().Value as RequestModel;
                if (file != null)
                {
                    context.HttpContext.Items[nameof(AddExpiryHeadersAttribute)] = file;
                }
            }

            /// <summary>
            /// Adds the expiry headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Exception != null) return;

                //get the model from the items
                if (!context.HttpContext.Items.ContainsKey(nameof(AddExpiryHeadersAttribute))) return;
                var file = context.HttpContext.Items[nameof(AddExpiryHeadersAttribute)] as RequestModel;
                if (file == null) return;

                var enableETag = true;
                var cacheControlMaxAge = 10*24; //10 days

                //check if it's a bundle (not composite file)
                var bundleRequest = file as BundleRequestModel;
                if (bundleRequest != null)
                {
                    Bundle b;
                    if (_bundleManager.TryGetValue(bundleRequest.FileKey, out b))
                    {
                        var bundleOptions = b.GetBundleOptions(_bundleManager, bundleRequest.Debug);
                        enableETag = bundleOptions.CacheControlOptions.EnableETag;
                        cacheControlMaxAge = bundleOptions.CacheControlOptions.CacheControlMaxAge;
                    }
                }

                if (enableETag)
                {
                    var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);
                    context.HttpContext.Response.AddETagResponseHeader(etag);
                }

                if (cacheControlMaxAge > 0)
                {
                    context.HttpContext.Response.AddCacheControlResponseHeader(cacheControlMaxAge);
                    context.HttpContext.Response.AddLastModifiedResponseHeader(file);
                    context.HttpContext.Response.AddExpiresResponseHeader(cacheControlMaxAge);
                }
            }
        }

        public int Order { get; set; }
    }
}