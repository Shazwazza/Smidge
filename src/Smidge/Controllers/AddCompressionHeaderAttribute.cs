using Microsoft.AspNet.Mvc;

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
            context.HttpContext.Response.AddCompressionResponseHeader(context.HttpContext.Request.GetClientCompression());
        }
    }

}