using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Smidge.Hashing;

namespace Smidge.Controllers
{
    /// <summary>
    /// This checks the request headers to see if the response has been modified, if it has not we return a 304 and short circuit the request
    /// </summary>
    public class CheckNotModifiedAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new CheckNotModifiedFilter(serviceProvider.GetRequiredService<IHasher>());
        }

        /// <summary>
        /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
        /// can be reused across requests.
        /// </summary>
        public bool IsReusable => true;

        public sealed class CheckNotModifiedFilter : IActionFilter
        {
            private readonly IHasher _hasher;

            public CheckNotModifiedFilter(IHasher hasher)
            {
                _hasher = hasher;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (!context.ActionArguments.Any()) return;

                //put the model in the context, we'll resolve that after it's executed
                var file = context.ActionArguments.First().Value as RequestModel;
                if (file != null)
                {
                    context.HttpContext.Items[nameof(CheckNotModifiedAttribute)] = file;
                }
            }

            /// <summary>
            /// Adds the expiry headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                //get the model from the items
                if (!context.HttpContext.Items.ContainsKey(nameof(CheckNotModifiedAttribute))) return;
                var file = context.HttpContext.Items[nameof(CheckNotModifiedAttribute)] as RequestModel;
                if (file == null) return;

                //Don't execute when the request is in Debug
                if (file.Debug) return;

                var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);

                var isDifferent = context.HttpContext.Request.HasETagBeenModified(etag);
                var hasChanged = context.HttpContext.Request.HasRequestBeenModifiedSince(file.LastFileWriteTime.ToUniversalTime());
                if (!isDifferent || !hasChanged)
                {
                    ReturnNotModified(context);
                }
            }

            private void ReturnNotModified(ActionExecutedContext context)
            {
                context.Result = new StatusCodeResult(HttpConstants.NotModified304);
            }
        }

    }
}