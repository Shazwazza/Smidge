using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Core;
using Smidge.Cache;

namespace Smidge.Controllers
{
    //TODO: Should this execute when debug = true?

    /// <summary>
    /// This checks the file system for an already persisted minified, combined, compressed file for the 
    /// request definition. If there is one it returns that file directly and the controller does not execute.
    /// </summary>
    public sealed class CompositeFileCacheFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new CacheFilter(
                serviceProvider.GetRequiredService<FileSystemHelper>(),
                serviceProvider.GetRequiredService<CacheBusterResolver>(),
                serviceProvider.GetRequiredService<IBundleManager>());
        }

        /// <summary>
        /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
        /// can be reused across requests.
        /// </summary>
        public bool IsReusable => true;

        internal static bool TryGetCachedCompositeFileResult(FileSystemHelper fileSystemHelper, ICacheBuster cacheBuster, string filesetKey, CompressionType type, string mime, 
            out FileResult result, out DateTime lastWriteTime)
        {
            result = null;
            lastWriteTime = DateTime.Now;

            var filesetPath = fileSystemHelper.GetCurrentCompositeFilePath(cacheBuster, type, filesetKey);
            if (System.IO.File.Exists(filesetPath))
            {
                lastWriteTime = System.IO.File.GetLastWriteTime(filesetPath);
                //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files
                result = new PhysicalFileResult(filesetPath, mime);
                return true;
            }

            return false;
        }

        /// <summary>
        /// The internal filter that performs the lookup
        /// </summary>
        private class CacheFilter : IActionFilter
        {
            private readonly FileSystemHelper _fileSystemHelper;
            private readonly CacheBusterResolver _cacheBusterResolver;
            private readonly IBundleManager _bundleManager;

            public CacheFilter(FileSystemHelper fileSystemHelper, CacheBusterResolver cacheBusterResolver, IBundleManager bundleManager)
            {
                _fileSystemHelper = fileSystemHelper;
                _cacheBusterResolver = cacheBusterResolver;
                _bundleManager = bundleManager;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (!context.ActionArguments.Any()) return;
                var bundleFile = context.ActionArguments.First().Value as BundleRequestModel;
                ICacheBuster cacheBuster;
                RequestModel file = null;
                if (bundleFile != null)
                {
                    cacheBuster = _cacheBusterResolver.GetCacheBuster(bundleFile.FileBundle.GetBundleOptions(_bundleManager, bundleFile.Debug).GetCacheBusterType());                        
                }
                else
                {
                    //the default for any dynamically (non bundle) file is the default bundle options in production
                    cacheBuster = _cacheBusterResolver.GetCacheBuster(_bundleManager.GetDefaultBundleOptions(false).GetCacheBusterType());
                    file = context.ActionArguments.First().Value as RequestModel;
                }

                if (file != null)
                {
                    FileResult result;
                    DateTime lastWrite;
                    if (TryGetCachedCompositeFileResult(_fileSystemHelper, cacheBuster, file.FileKey, file.Compression, file.Mime, out result, out lastWrite))
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