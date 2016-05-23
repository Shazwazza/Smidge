using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;

namespace Smidge.Controllers
{

    /// <summary>
    /// Adds the compression headers
    /// </summary>
    public sealed class AddCompressionHeaderAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Adds the compression headers
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var requestHelper = new RequestHelper(context.HttpContext.Request);
            context.HttpContext.Response.AddCompressionResponseHeader(requestHelper.GetClientCompression());
        }
    }

}