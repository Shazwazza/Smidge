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

        public bool IsReusable => true;

        public int Order { get; set; }

        private class AddCompressionFilter : IActionFilter
        {
            private readonly IRequestHelper _requestHelper;
            private readonly IBundleManager _bundleManager;

            public AddCompressionFilter(IRequestHelper requestHelper, IBundleManager bundleManager)
            {
                _requestHelper = requestHelper ?? throw new ArgumentNullException(nameof(requestHelper));
                _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (context.ActionArguments.Count == 0)
                    return;

                //put the model in the context, we'll resolve that after it's executed
                if (context.ActionArguments.First().Value is RequestModel file)
                    context.HttpContext.Items[nameof(AddCompressionHeaderAttribute)] = file;
            }

            /// <summary>
            /// Adds the compression headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Exception != null) return;

                //get the model from the items
                if (context.HttpContext.Items.TryGetValue(nameof(AddCompressionHeaderAttribute), out var requestModel) && requestModel is RequestModel file && file.IsBundleFound)
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
            }
        }
    }
}
