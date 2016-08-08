using System;
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
            return new AddCompressionFilter(serviceProvider.GetRequiredService<IRequestHelper>());
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

            public AddCompressionFilter(IRequestHelper requestHelper)
            {
                if (requestHelper == null) throw new ArgumentNullException(nameof(requestHelper));
                _requestHelper = requestHelper;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
            }

            /// <summary>
            /// Adds the compression headers
            /// </summary>
            /// <param name="context"></param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                context.HttpContext.Response.AddCompressionResponseHeader(
                    _requestHelper.GetClientCompression(context.HttpContext.Request.Headers));
            }
        }
    }

}