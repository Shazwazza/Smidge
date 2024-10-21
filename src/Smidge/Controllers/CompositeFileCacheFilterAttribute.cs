using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smidge.Controllers
{
    //TODO: Should this execute when debug = true?

    /// <summary>
    /// This checks the file system for an already persisted minified, combined, compressed file for the 
    /// request definition. If there is one it returns that file directly and the controller does not execute.
    /// </summary>
    public sealed class CompositeFileCacheFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new CacheFilter(
                serviceProvider.GetRequiredService<ISmidgeFileSystem>());
        }

        public bool IsReusable => true;

        public int Order { get; set; }

        internal static bool TryGetCachedCompositeFileResult(ISmidgeFileSystem fileSystem, string cacheBusterValue, string filesetKey, CompressionType type, string mime, out FileResult result, out DateTime lastWriteTime)
        {
            result = null;

            var cacheFile = fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, type, filesetKey, out _);
            if (cacheFile.Exists)
            {
                lastWriteTime = cacheFile.LastModified.DateTime;

                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the PhysicalFileResult
                    //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files
                    result = new PhysicalFileResult(cacheFile.PhysicalPath, mime);
                    return true;
                }

                //deliver the file via stream
                result = new FileStreamResult(cacheFile.CreateReadStream(), mime);
                return true;
            }

            lastWriteTime = DateTime.Now;
            return false;
        }

        /// <summary>
        /// The internal filter that performs the lookup
        /// </summary>
        private class CacheFilter : IActionFilter
        {
            private readonly ISmidgeFileSystem _fileSystem;

            public CacheFilter(ISmidgeFileSystem fileSystem)
            {
                _fileSystem = fileSystem;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (context.ActionArguments.Count == 0) return;

                var firstArg = context.ActionArguments.First().Value;
                if (firstArg is RequestModel file && file.IsBundleFound)
                {
                    var cacheBusterValue = file.ParsedPath.CacheBusterValue;

                    if (TryGetCachedCompositeFileResult(_fileSystem, cacheBusterValue, file.FileKey, file.Compression, file.Mime, out FileResult result, out DateTime lastWrite))
                    {
                        file.LastFileWriteTime = lastWrite;
                        context.Result = result;
                    }
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            { }
        }
    }
}
