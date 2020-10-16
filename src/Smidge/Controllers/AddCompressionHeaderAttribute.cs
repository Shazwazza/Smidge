using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;

namespace Smidge.Controllers
{

    /// <summary>
    /// Adds the compression headers
    /// </summary>
    public sealed class AddCompressionHeaderAttribute : Attribute, IFilterFactory, IOrderedFilter
    {        
        /// <summary>Creates an instance of the executable filter.</summary>
        /// <param name="serviceProvider">The request <see cref="T:System.IServiceProvider" />.</param>
        /// <returns>An instance of the executable filter.</returns>
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new AddCompressionFilter(
                serviceProvider.GetRequiredService<IRequestHelper>(),
                serviceProvider.GetRequiredService<IBundleManager>());
        }
        
        /// <summary>
        /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
        /// can be reused across requests.
        /// </summary>
        public bool IsReusable => true;

        public int Order { get; set; }

        private class AddCompressionFilter : IActionFilter
        {
            private readonly IRequestHelper _requestHelper;
            private readonly IBundleManager _bundleManager;

            public AddCompressionFilter(IRequestHelper requestHelper, IBundleManager bundleManager)
            {
                if (requestHelper == null) throw new ArgumentNullException(nameof(requestHelper));
                if (bundleManager == null) throw new ArgumentNullException(nameof(bundleManager));
                _requestHelper = requestHelper;
                _bundleManager = bundleManager;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (!context.ActionArguments.Any()) return;

                //put the model in the context, we'll resolve that after it's executed
                var file = context.ActionArguments.First().Value as RequestModel;
                if (file != null)
                {
                    context.HttpContext.Items[nameof(AddCompressionHeaderAttribute)] = file;
                }
            }

            /// <summary>
            /// Adds the compression headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Exception != null) return;

                //get the model from the items
                if (!context.HttpContext.Items.ContainsKey(nameof(AddCompressionHeaderAttribute))) return;
                var file = context.HttpContext.Items[nameof(AddCompressionHeaderAttribute)] as RequestModel;
                if (file == null) return;

                var enableCompression = true;

                //check if it's a bundle (not composite file)
                var bundleRequest = file as BundleRequestModel;
                if (bundleRequest != null)
                {
                    Bundle b;
                    if (_bundleManager.TryGetValue(bundleRequest.FileKey, out b))
                    {
                        var bundleOptions = b.GetBundleOptions(_bundleManager, bundleRequest.Debug);
                        enableCompression = bundleOptions.CompressResult;
                    }
                }

                if (enableCompression)
                {
                    context.HttpContext.Response.AddCompressionResponseHeader(
                       _requestHelper.GetClientCompression(context.HttpContext.Request.Headers));
                }                
            }
        }
    }

}