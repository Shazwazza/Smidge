using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Smidge.CompositeFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.FileProviders;
//using Microsoft.AspNetCore.NodeServices;
using Smidge.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Smidge.Options;
using Smidge.FileProcessors;
using Smidge.Hashing;
using Smidge.NodeServices;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public static class SmidgeStartup
    {


        public static IServiceCollection AddSmidge(this IServiceCollection services, 
            IConfiguration smidgeConfiguration = null, 
            IFileProvider fileProvider = null)
        {            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IRequestHelper, RequestHelper>();
            services.AddSingleton<IWebsiteInfo, AutoWebsiteInfo>();

            //services.AddNodeServices(NodeHostingModel.Http);

            services.AddTransient<IConfigureOptions<SmidgeOptions>, SmidgeOptionsSetup>();
            services.AddSingleton<BundleManager>();
            services.AddSingleton<FileSystemHelper>(p =>
            {
                var hosting = p.GetRequiredService<IHostingEnvironment>();
                var provider = fileProvider ?? hosting.WebRootFileProvider;
                return new FileSystemHelper(hosting, p.GetRequiredService<ISmidgeConfig>(), provider, p.GetRequiredService<IHasher>());
            });


            services.AddSingleton<PreProcessManager>();
            services.AddSingleton<ISmidgeConfig>((p) =>
            {
                if (smidgeConfiguration == null)
                {
                    return new SmidgeConfig(p.GetRequiredService<IHostingEnvironment>());
                }
                return new SmidgeConfig(smidgeConfiguration);
            });
            services.AddScoped<DynamicallyRegisteredWebFiles>();
            services.AddScoped<SmidgeHelper>();
            services.AddScoped<IUrlManager, DefaultUrlManager>();
            services.AddSingleton<IHasher, Crc32Hasher>();

            services.AddSingleton<SmidgeNodeServices>(provider =>
            {
                var env = provider.GetRequiredService<IHostingEnvironment>();
                return new SmidgeNodeServices(Configuration.CreateNodeServices(
                    new NodeServicesOptions
                    {
                        ProjectPath = env.ContentRootPath,
                        WatchFileExtensions = new string[] {}
                    }));
            });
                
            services.AddSingleton<PreProcessPipelineFactory>();
            //pre-processors
            services.AddSingleton<IPreProcessor, JsMinifier>();
            services.AddSingleton<IPreProcessor, CssMinifier>();
            services.AddSingleton<IPreProcessor, UglifyNodeMinifier>();
            services.AddSingleton<IPreProcessor, CssImportProcessor>();
            services.AddSingleton<IPreProcessor, CssUrlProcessor>();
            //conventions
            services.AddSingleton<FileProcessingConventions>();
            services.AddSingleton<IFileProcessingConvention, MinifiedFilePathConvention>();

            //Add the controller models as DI services - these get auto created for model binding
            services.AddTransient<BundleRequestModel>();
            services.AddTransient<CompositeFileModel>();

            return services;
        }
        
        public static void UseSmidge(this IApplicationBuilder app, Action<BundleManager> configureBundles = null)
        {
            //middleware to auto-configure the base path + url for use with the IWebsiteInfo
            // if the registered typed is AutoWebsiteInfo
            var siteInfo = app.ApplicationServices.GetRequiredService<IWebsiteInfo>() as AutoWebsiteInfo;
            if (siteInfo != null)
            {
                app.Use(async (context, next) =>
                {
                    if (!siteInfo.IsConfigured)
                    {
                        //TODO: Check for nulls here
                        siteInfo.ConfigureOnce(
                            context.Request.PathBase, 
                            //TODO: This could be any URI for the site, need to clean this up
                            // probably just need the SchemaAndServer + the PathBase
                            new Uri(context.Request.GetEncodedUrl()));
                    }
                    await next.Invoke();
                });
            }            

            //Create custom route
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "SmidgeComposite",
                    "sc/{file}",
                    new { controller = "Smidge", action = "Composite" });

                routes.MapRoute(
                    "SmidgeBundle",
                    "sb/{bundle}",
                    new { controller = "Smidge", action = "Bundle" });
            });

            if (configureBundles != null)
            {
                var bundleFactory = app.ApplicationServices.GetRequiredService<BundleManager>();
                configureBundles(bundleFactory);
            }

        }
    }
}
