using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Core;

namespace Smidge.Controllers
{

    /// <summary>
    /// This checks the file system for an already persisted minified, combined, compressed file for the 
    /// request definition. If there is one it returns that file directly and the controller does not execute.
    /// </summary>
    public sealed class CompositeFileCacheFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new CacheFilter(serviceProvider.GetRequiredService<FileSystemHelper>());
        }

        internal static bool TryGetCachedCompositeFileResult(FileSystemHelper fileSystemHelper, string filesetKey, CompressionType type, string mime, 
            out FileResult result, out DateTime lastWriteTime)
        {
            result = null;
            lastWriteTime = DateTime.Now;

            var filesetPath = fileSystemHelper.GetCurrentCompositeFilePath(type, filesetKey);
            if (System.IO.File.Exists(filesetPath))
            {
                lastWriteTime = System.IO.File.GetLastWriteTime(filesetPath);
                //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files
                result = new PhysicalFileProviderResult(filesetPath, mime);
                return true;
            }

            return false;
        }

        /// <summary>
        /// The internal filter that performs the lookup
        /// </summary>
        private class CacheFilter : IActionFilter
        {
            private FileSystemHelper _fileSystemHelper;

            public CacheFilter(FileSystemHelper fileSystemHelper)
            {
                _fileSystemHelper = fileSystemHelper;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (!context.ActionArguments.Any()) return;
                var file = context.ActionArguments.First().Value as RequestModel;
                if (file != null)
                {
                    FileResult result;
                    DateTime lastWrite;
                    if (TryGetCachedCompositeFileResult(_fileSystemHelper, file.FileKey, file.Compression, file.Mime, out result, out lastWrite))
                    {
                        file.LastFileWriteTime = lastWrite;
                        context.Result = result;
                    }
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
            }
        }
    }
}