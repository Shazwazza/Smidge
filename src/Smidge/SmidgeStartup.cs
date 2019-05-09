using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Smidge.CompositeFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Smidge.Models;
using Microsoft.Extensions.Options;
using Smidge.Options;
using Smidge.FileProcessors;
using Smidge.Hashing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smidge.Cache;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public static class SmidgeStartup
    {


        public static IServiceCollection AddSmidge(this IServiceCollection services, 
            IConfiguration smidgeConfiguration = null, 
            IFileProvider fileProvider = null)
        {            
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            
            services.AddTransient<IConfigureOptions<SmidgeOptions>, SmidgeOptionsSetup>();

            services.AddSingleton<PreProcessManager>();
            services.AddSingleton<IRequestHelper, RequestHelper>();
            services.AddSingleton<IWebsiteInfo, AutoWebsiteInfo>();
            services.AddSingleton<IBundleFileSetGenerator, BundleFileSetGenerator>();
            services.AddSingleton<IHasher, Crc32Hasher>();
            services.AddSingleton<IBundleManager, BundleManager>();            
            services.AddSingleton<PreProcessPipelineFactory>();
            services.AddSingleton<ISmidgeFileSystem>(p =>
            {
                var hosting = p.GetRequiredService<IHostingEnvironment>();
                var provider = fileProvider ?? hosting.WebRootFileProvider;
                return new SmidgeFileSystem(provider, p.GetRequiredService<ICacheFileSystem>());
            });
            services.AddSingleton<ICacheFileSystem>(p =>
            {
                //The default cache folder is a physical folder

                var hosting = p.GetRequiredService<IHostingEnvironment>();
                var provider = fileProvider ?? hosting.WebRootFileProvider;                
                var config = p.GetRequiredService<ISmidgeConfig>();
                var cacheFolder = Path.Combine(hosting.ContentRootPath, config.DataFolder, "Cache", Environment.MachineName.ReplaceNonAlphanumericChars('-'));

                //ensure it exists
                Directory.CreateDirectory(cacheFolder);

                var cacheFileProvider = new PhysicalFileProvider(cacheFolder);
                
                return new PhysicalFileCacheFileSystem(cacheFileProvider, p.GetRequiredService<IHasher>());
            });
            services.AddSingleton<ISmidgeConfig>((p) =>
            {
                if (smidgeConfiguration == null)
                {
                    return new SmidgeConfig(p.GetRequiredService<IHostingEnvironment>());
                }
                return new SmidgeConfig(smidgeConfiguration);
            });
            
            services.AddSingleton<ICacheBuster, ConfigCacheBuster>();
            services.AddSingleton<ICacheBuster, AppDomainLifetimeCacheBuster>();
            services.AddSingleton<CacheBusterResolver>();

            //These all execute as part of the request/scope            
            services.AddScoped<DynamicallyRegisteredWebFiles>();
            services.AddScoped<SmidgeHelper>();
            services.AddScoped<IUrlManager, DefaultUrlManager>();            

            //pre-processors
            services.AddSingleton<IPreProcessor, JsMinifier>();
            services.AddSingleton<IPreProcessor, CssMinifier>();            
            services.AddSingleton<IPreProcessor, CssImportProcessor>();
            services.AddSingleton<IPreProcessor, CssUrlProcessor>();
            services.AddSingleton<Lazy<IEnumerable<IPreProcessor>>>(provider => new Lazy<IEnumerable<IPreProcessor>>(provider.GetRequiredService<IEnumerable<IPreProcessor>>));

            //conventions
            services.AddSingleton<FileProcessingConventions>();
            services.AddSingleton<IFileProcessingConvention, MinifiedFilePathConvention>();

            //Add the controller models as DI services - these get auto created for model binding
            services.AddTransient<BundleRequestModel>();
            services.AddTransient<CompositeFileModel>();

            return services;
        }
        
        public static void UseSmidge(this IApplicationBuilder app, Action<IBundleManager> configureBundles = null)
        {
            //Create custom route
            app.UseMvc(routes =>
            {
                var options = app.ApplicationServices.GetRequiredService<IOptions<SmidgeOptions>>();

                routes.MapRoute(
                    "SmidgeComposite",
                    options.Value.UrlOptions.CompositeFilePath + "/{file}",                    
                    new { controller = "Smidge", action = "Composite" });

                routes.MapRoute(
                    "SmidgeBundle",
                    options.Value.UrlOptions.BundleFilePath + "/{bundle}",
                    new { controller = "Smidge", action = "Bundle" });
            });

            if (configureBundles != null)
            {
                var bundleManager = app.ApplicationServices.GetRequiredService<IBundleManager>();
                configureBundles(bundleManager);

                var cacheBusterResolver = app.ApplicationServices.GetRequiredService<CacheBusterResolver>();
                var fileSystem = app.ApplicationServices.GetRequiredService<ISmidgeFileSystem>();

                //TODO: Now that they are configured we need to wire up the file watching event handlers
                // to the bundle manager, currently these are on the Bundle, but that is not good enough
                // since we need the bundle name
                foreach (var webFileType in new[] {WebFileType.Css, WebFileType.Js })
                {
                    var bundles = bundleManager.GetBundles(webFileType);
                    foreach (var bundle in bundles)
                    {
                        WireUpFileWatchEventHandlers(cacheBusterResolver, fileSystem, bundle);
                    }
                }
            }    

        }

        private static void WireUpFileWatchEventHandlers(CacheBusterResolver cacheBusterResolver, ISmidgeFileSystem fileSystem, Bundle bundle)
        {
            if (bundle.BundleOptions == null) return;

            if (bundle.BundleOptions.DebugOptions.FileWatchOptions.Enabled)
            {
                bundle.BundleOptions.DebugOptions.FileWatchOptions.FileModified += FileWatchOptions_FileModified(cacheBusterResolver, fileSystem, bundle.Name);
            }
            if (bundle.BundleOptions.ProductionOptions.FileWatchOptions.Enabled)
            {
                bundle.BundleOptions.ProductionOptions.FileWatchOptions.FileModified += FileWatchOptions_FileModified(cacheBusterResolver, fileSystem, bundle.Name);
            }
        }

        private static EventHandler<FileWatchEventArgs> FileWatchOptions_FileModified(CacheBusterResolver cacheBusterResolver, ISmidgeFileSystem fileSystem, string bundleName)
        {
            return (sender, args) =>
            {
                FileWatchOptions_FileModified(cacheBusterResolver, fileSystem, bundleName, args);
            };
        }

        //async void = ok here sincew this is an event handler
        private static async void FileWatchOptions_FileModified(CacheBusterResolver cacheBusterResolver, ISmidgeFileSystem fileSystem, string bundleName, FileWatchEventArgs e)
        {
            var bundleOptions = e.File.BundleOptions;
            var cacheBuster = cacheBusterResolver.GetCacheBuster(bundleOptions.GetCacheBusterType());

            //this file is part of this bundle, so the persisted processed/combined/compressed  will need to be 
            // invalidated/deleted/renamed
            foreach (var compressionType in new[] { CompressionType.deflate, CompressionType.gzip, CompressionType.none })
            {
                var compFile = fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBuster, compressionType, bundleName);
                if (compFile.Exists)
                {
                    await fileSystem.CacheFileSystem.ClearFileAsync(compFile);
                }
            }
        }
    }
}
