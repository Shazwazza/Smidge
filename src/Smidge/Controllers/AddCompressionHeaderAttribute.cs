using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            base.OnActionExecuted(context);
            context.HttpContext.Response.AddCompressionResponseHeader(context.HttpContext.Request.GetClientCompression());
        }
    }

    
    //public sealed class CacheFilterAttribute : Attribute, IFilterFactory
    //{

    //    public IFilter CreateInstance(IServiceProvider serviceProvider)
    //    {
    //        var memCache = serviceProvider.GetRequiredService<IMemoryCache>();
    //        return new CacheFilter(memCache);
    //    }

    //    private class CacheFilter : ActionFilterAttribute
    //    {
    //        private IMemoryCache _cache;

    //        public CacheFilter(IMemoryCache cache)
    //        {
    //            _cache = cache;
    //        }

    //        //TODO: Given the client compression and the bundle Id, 
    //        public override void OnActionExecuting(ActionExecutingContext context)
    //        {
    //            var bundleId = context.ActionArguments["id"];
    //            if (bundleId != null)
    //            {
    //                var result = _cache.Get<IEnumerable<string>>("\{nameof(CacheFilterAttribute)}\{bundleId}");
    //                if (result != null)
    //                {
    //                    // there is a result, so we just need to construct the result and send it
    //                    using (var resultStream = await GetCombinedStreamAsync(filePaths))
    //                    {
    //                        var compressedStream = await Compressor.CompressAsync(compression, resultStream);

    //                        await CacheCompositeFileAsync(parsed.Names.Single(), compressedStream, compression);

    //                        return new CompositeFileStreamResult(filePaths, compressedStream, mime);
    //                    }
    //                }
    //            }
    //        }

    //        public override void OnActionExecuted(ActionExecutedContext context)
    //        {
    //            base.OnActionExecuted(context);
    //        }
    //    }
    //}
}