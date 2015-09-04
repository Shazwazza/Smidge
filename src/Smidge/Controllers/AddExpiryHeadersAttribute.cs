using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
namespace Smidge.Controllers
{
    /// <summary>
    /// Adds the correct caching expiry headers
    /// </summary>
    public sealed class AddExpiryHeadersAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new AddExpiryHeaderFilter(serviceProvider.GetRequiredService<IHasher>());
        }

        public sealed class AddExpiryHeaderFilter : IActionFilter
        {
            private IHasher _hasher;

            public AddExpiryHeaderFilter(IHasher hasher)
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
                    context.HttpContext.Items[nameof(AddExpiryHeadersAttribute)] = file;
                }
            }

            /// <summary>
            /// Adds the expiry headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                //get the model from the items
                if (!context.HttpContext.Items.ContainsKey(nameof(AddExpiryHeadersAttribute))) return;
                var file = context.HttpContext.Items[nameof(AddExpiryHeadersAttribute)] as RequestModel;
                if (file == null) return;

                var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);

                context.HttpContext.Response.AddETagResponseHeader(etag);
                context.HttpContext.Response.AddCacheControlResponseHeader();
                context.HttpContext.Response.AddLastModifiedResponseHeader(file);
                context.HttpContext.Response.AddExpiresResponseHeader();
            }
        }
    }
}