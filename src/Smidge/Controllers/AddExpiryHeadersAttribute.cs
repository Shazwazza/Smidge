using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

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

        /// <summary>
        /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
        /// can be reused across requests.
        /// </summary>
        public bool IsReusable => true;

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