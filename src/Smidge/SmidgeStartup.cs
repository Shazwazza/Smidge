﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.Controllers;
using Smidge.FileProcessors;
using Smidge.Hashing;
using Smidge.Models;
using Smidge.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public static class SmidgeStartup
    {
        //For .net core 3.0, call this before AddControllers/AddControllersWithViews etc
        //If you call it after, then if AddControllersAsServices was used before you need
        //to tell smidge to do so

#if NETCORE3_0
        public static IServiceCollection AddSmidge(this IServiceCollection services,
            IConfiguration smidgeConfiguration = null,
            IFileProvider fileProvider = null)
#else

        public static IServiceCollection AddSmidge(this IServiceCollection services,
            IConfiguration smidgeConfiguration = null,
            IFileProvider fileProvider = null)
#endif
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
            services.AddSingleton<FileSystemHelper>(p =>
            {
#if NETCORE3_0                     
                var hosting = p.GetRequiredService<IWebHostEnvironment>();
#else
                var hosting = p.GetRequiredService<IHostingEnvironment>();
#endif
                var provider = fileProvider ?? hosting.WebRootFileProvider;

                return new FileSystemHelper(hosting, p.GetRequiredService<ISmidgeConfig>(), provider, p.GetRequiredService<IHasher>(), p.GetRequiredService<IWebsiteInfo>());
            });
            services.AddSingleton<ISmidgeConfig>((p) =>
            {
                if (smidgeConfiguration == null)
                {
#if NETCORE3_0                     
                    return new SmidgeConfig(p.GetRequiredService<IWebHostEnvironment>());
#else
                    return new SmidgeConfig(p.GetRequiredService<IHostingEnvironment>());
#endif
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
            services.AddSingleton<IPreProcessor, JsSourceMapProcessor>();
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

            // NOTE: This wasn't explicitly requred for app previous to .net core 3, however it seems like it should have always been there for 
            // previous versions anyways. Seems sort of odd that this ever worked without it?
            var builder = services.AddMvcCore();
            builder.AddApplicationPart(typeof(SmidgeStartup).Assembly);

            return services;
        }



#if NETCORE3_0
        public static void UseSmidge(this IApplicationBuilder app, Action<IBundleManager> configureBundles = null, bool useEndpointRouting = true)
#else
        public static void UseSmidge(this IApplicationBuilder app, Action<IBundleManager> configureBundles = null)
#endif
        {
            //Creates custom routes 
            var options = app.ApplicationServices.GetRequiredService<IOptions<SmidgeOptions>>();
#if NETCORE3_0
            //NOTE: It's no longer polite to just call UseMVC as it enables things that the developer may 
            //not need and the dev must disable EndpointRouting - so we let the dev decide.
            //with core 3.0 you have to explicitly disable EndpointRouting se we default to on here 
            if (useEndpointRouting)
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                            name: "SmidgeComposite",
                            pattern: options.Value.UrlOptions.CompositeFilePath + "/{file}",
                            defaults: new { controller = "Smidge", action = "Composite" });
                    endpoints.MapControllerRoute(
                            name: "SmidgeBundle",
                            pattern: options.Value.UrlOptions.BundleFilePath + "/{bundle}",
                            defaults: new { controller = "Smidge", action = "Bundle" });
                });

            }
            else
            {
#endif
                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        "SmidgeComposite",
                        options.Value.UrlOptions.CompositeFilePath + "/{file}",
                        new { controller = "Smidge", action = "Composite" });

                    routes.MapRoute(
                        "SmidgeBundle",
                        options.Value.UrlOptions.BundleFilePath + "/{bundle}",
                        new { controller = "Smidge", action = "Bundle" });
                });
#if NETCORE3_0
            }
#endif
            if (configureBundles != null)
            {
                var bundleManager = app.ApplicationServices.GetRequiredService<IBundleManager>();
                configureBundles(bundleManager);

                var cacheBusterResolver = app.ApplicationServices.GetRequiredService<CacheBusterResolver>();

                //TODO: Now that they are configured we need to wire up the file watching event handlers
                // to the bundle manager, currently these are on the Bundle, but that is not good enough
                // since we need the bundle name
                foreach (var webFileType in new[] {WebFileType.Css, WebFileType.Js })
                {
                    var names = bundleManager.GetBundleNames(webFileType);
                    foreach (var cssName in names)
                    {
                        var bundle = bundleManager.GetBundle(cssName);
                        WireUpFileWatchEventHandlers(bundleManager, cacheBusterResolver, cssName, bundle);
                    }
                }
            }    
        }

        private static void WireUpFileWatchEventHandlers(IBundleManager bundleManager, CacheBusterResolver cacheBusterResolver, string bundleName, Bundle bundle)
        {
            if (bundle.BundleOptions == null) return;

            if (bundle.BundleOptions.DebugOptions.FileWatchOptions.Enabled)
            {
                bundle.BundleOptions.DebugOptions.FileWatchOptions.FileModified += FileWatchOptions_FileModified(bundleManager, cacheBusterResolver, bundleName, bundle, true);
            }
            if (bundle.BundleOptions.ProductionOptions.FileWatchOptions.Enabled)
            {
                bundle.BundleOptions.ProductionOptions.FileWatchOptions.FileModified += FileWatchOptions_FileModified(bundleManager, cacheBusterResolver, bundleName, bundle, false);
            }
        }

        private static EventHandler<FileWatchEventArgs> FileWatchOptions_FileModified(IBundleManager bundleManager, CacheBusterResolver cacheBusterResolver, string bundleName, Bundle bundle, bool debug)
        {
            return (sender, args) =>
            {
                FileWatchOptions_FileModified(bundleManager, cacheBusterResolver, bundleName, bundle, debug, args);
            };
        }

        private static void FileWatchOptions_FileModified(IBundleManager bundleManager, CacheBusterResolver cacheBusterResolver, string bundleName, Bundle bundle, bool debug, FileWatchEventArgs e)
        {
            var cacheBuster = cacheBusterResolver.GetCacheBuster(bundle.GetBundleOptions(bundleManager, debug).GetCacheBusterType());                

            //this file is part of this bundle, so the persisted processed/combined/compressed  will need to be 
            // invalidated/deleted/renamed
            foreach (var compressionType in new[] { CompressionType.deflate, CompressionType.gzip, CompressionType.none })
            {
                var compFilePath = e.FileSystemHelper.GetCurrentCompositeFilePath(cacheBuster, compressionType, bundleName);
                if (File.Exists(compFilePath))
                {
                    File.Delete(compFilePath);
                }
            }
        }
    }
}
