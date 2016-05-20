using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

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
            private IHasher _hasher;

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

        /// <summary>
        /// Gets the order value for determining the order of execution of filters. Filters execute in
        /// ascending numeric value of the <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" /> property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Filters are executed in an ordering determined by an ascending sort of the <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" /> property.
        /// </para>
        /// <para>
        /// Asynchronous filters, such as <see cref="T:Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter" />, surround the execution of subsequent
        /// filters of the same filter kind. An asynchronous filter with a lower numeric <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" />
        /// value will have its filter method, such as <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter.OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext,Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate)" />,
        /// executed before that of a filter with a higher value of <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" />.
        /// </para>
        /// <para>
        /// Synchronous filters, such as <see cref="T:Microsoft.AspNetCore.Mvc.Filters.IActionFilter" />, have a before-method, such as
        /// <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IActionFilter.OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext)" />, and an after-method, such as
        /// <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IActionFilter.OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext)" />. A synchronous filter with a lower numeric <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" />
        /// value will have its before-method executed before that of a filter with a higher value of
        /// <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" />. During the after-stage of the filter, a synchronous filter with a lower
        /// numeric <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" /> value will have its after-method executed after that of a filter with a higher
        /// value of <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" />.
        /// </para>
        /// <para>
        /// If two filters have the same numeric value of <see cref="P:Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter.Order" />, then their relative execution order
        /// is determined by the filter scope.
        /// </para>
        /// </remarks>
        int IOrderedFilter.Order { get; }
    }
}