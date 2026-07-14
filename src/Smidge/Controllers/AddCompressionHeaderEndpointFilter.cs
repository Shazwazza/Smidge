using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smidge.Models;

namespace Smidge.Controllers
{
    /// <summary>
    /// Adds the compression headers
    /// </summary>
    internal sealed class AddCompressionHeaderEndpointFilter : IEndpointFilter
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
