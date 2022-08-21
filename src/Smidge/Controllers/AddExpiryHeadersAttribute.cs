using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Smidge.Hashing;
using Smidge.Options;

namespace Smidge.Controllers
{
    /// <summary>
    /// Adds the correct caching expiry headers when the request is not in debug
    /// </summary>
    public sealed class AddExpiryHeadersAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) => new AddExpiryHeaderFilter(
            serviceProvider.GetRequiredService<ISmidgeProfileStrategy>(),
            serviceProvider.GetRequiredService<IHasher>(),
            serviceProvider.GetRequiredService<IBundleManager>());

        public bool IsReusable => true;

        public int Order { get; set; }

        public sealed class AddExpiryHeaderFilter : IActionFilter
        {
            private readonly ISmidgeProfileStrategy _profileStrategy;
            private readonly IHasher _hasher;
            private readonly IBundleManager _bundleManager;

            public AddExpiryHeaderFilter(ISmidgeProfileStrategy profileStrategy, IHasher hasher, IBundleManager bundleManager)
            {
                _profileStrategy = profileStrategy ?? throw new ArgumentNullException(nameof(profileStrategy));
                _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
                _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (context.ActionArguments.Count == 0)
                    return;

                //put the model in the context, we'll resolve that after it's executed
                if (context.ActionArguments.First().Value is RequestModel file)
                    context.HttpContext.Items[nameof(AddExpiryHeadersAttribute)] = file;
            }

            /// <summary>
            /// Adds the expiry headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Exception != null)
                    return;

                //get the model from the items
                if (!context.HttpContext.Items.TryGetValue(nameof(AddExpiryHeadersAttribute), out object fileObject) || fileObject is not RequestModel file)
                    return;

                var enableETag = true;
                var cacheControlMaxAge = 10 * 24; //10 days

                string profileName;
                BundleOptions bundleOptions;
                
                if (_bundleManager.TryGetValue(file.FileKey, out Bundle bundle))
                {
                    // For backwards compatibility we'll use the Debug profile if it was explicitly requested in the request.
                    if (file.Debug)
                    {
                        profileName = SmidgeOptionsProfile.Debug;
                    }
                    else
                    {
                        // If the Bundle explicitly specifies a profile to use then use it otherwise use the current profile 
                        profileName = !string.IsNullOrEmpty(bundle.ProfileName)
                            ? bundle.ProfileName
                            : _profileStrategy.GetCurrentProfileName();
                    }

                    bundleOptions = bundle.GetBundleOptions(_bundleManager, profileName);
                }
                else
                {
                    // For backwards compatibility we'll use the Debug profile if it was explicitly requested in the request.
                    profileName = file.Debug
                        ? SmidgeOptionsProfile.Debug
                        : _profileStrategy.GetCurrentProfileName();

                    _bundleManager.DefaultBundleOptions.TryGetProfileOptions(profileName, out bundleOptions);
                }

                if (bundleOptions != null)
                {
                    enableETag = bundleOptions.CacheControlOptions.EnableETag;
                    cacheControlMaxAge = bundleOptions.CacheControlOptions.CacheControlMaxAge;
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
    }
}
