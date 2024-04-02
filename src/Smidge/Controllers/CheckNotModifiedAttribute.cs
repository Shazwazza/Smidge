using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Smidge.Hashing;

namespace Smidge.Controllers
{
    /// <summary>
    /// This checks the request headers to see if the response has been modified, if it has not we return a 304 and short circuit the request
    /// </summary>
    public class CheckNotModifiedAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new CheckNotModifiedFilter(serviceProvider.GetRequiredService<IHasher>());
        }

        public bool IsReusable => true;

        public int Order { get; set; }

        public sealed class CheckNotModifiedFilter : IActionFilter
        {
            private readonly IHasher _hasher;

            public CheckNotModifiedFilter(IHasher hasher)
            {
                _hasher = hasher;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (context.ActionArguments.Count == 0)
                    return;

                //put the model in the context, we'll resolve that after it's executed
                if (context.ActionArguments.First().Value is RequestModel file)
                    context.HttpContext.Items[nameof(CheckNotModifiedAttribute)] = file;
            }

            /// <summary>
            /// Adds the expiry headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Exception != null) return;

                //get the model from the items
                if (context.HttpContext.Items.TryGetValue(nameof(CheckNotModifiedAttribute), out var requestModel) && requestModel is RequestModel file && file.IsBundleFound)
                {
                    //Don't execute when the request is in Debug
                    if (file.Debug)
                        return;

                    var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);

                    var isDifferent = context.HttpContext.Request.HasETagBeenModified(etag);
                    var hasChanged = context.HttpContext.Request.HasRequestBeenModifiedSince(file.LastFileWriteTime.ToUniversalTime());
                    if (!isDifferent || !hasChanged)
                    {
                        ReturnNotModified(context);
                    }
                }
            }

            private static void ReturnNotModified(ActionExecutedContext context)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            }
        }
    }
}
