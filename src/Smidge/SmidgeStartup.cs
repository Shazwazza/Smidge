using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Configuration;
using Smidge.CompositeFiles;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Hosting;
using System.Runtime.CompilerServices;
using Microsoft.AspNet.NodeServices;
using Smidge.Models;
using Microsoft.Framework.OptionsModel;
using Smidge.Options;
using Smidge.FileProcessors;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public static class SmidgeStartup
    {
        public static IServiceCollection AddSmidge(this IServiceCollection services)
        {
            services.AddNodeServices(NodeHostingModel.Http);

            services.AddTransient<IConfigureOptions<SmidgeOptions>, SmidgeOptionsSetup>();
            services.AddTransient<IConfigureOptions<Bundles>, BundlesSetup>();
            services.AddSingleton<PreProcessPipelineFactory>();
            services.AddSingleton<BundleManager>();
            services.AddSingleton<FileSystemHelper>();
            services.AddSingleton<PreProcessManager>();
            services.AddSingleton<ISmidgeConfig, SmidgeConfig>();
            services.AddScoped<SmidgeContext>();
            services.AddScoped<SmidgeHelper>();
            services.AddSingleton<IUrlManager, DefaultUrlManager>();
            services.AddSingleton<IHasher, Crc32Hasher>();

            //pre-processors
            services.AddSingleton<IPreProcessor, JsMin>();
            services.AddSingleton<IPreProcessor, CssMinifier>();
            services.AddSingleton<IPreProcessor, NodeMinifier>();
            services.AddScoped<IPreProcessor, CssImportProcessor>();
            services.AddScoped<IPreProcessor, CssUrlProcessor>();


            //Add the controller models as DI services - these get auto created for model binding
            services.AddTransient<BundleModel>();
            services.AddTransient<CompositeFileModel>();

            return services;
        }

        public static void UseSmidge(this IApplicationBuilder app)
        {

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
            

        }
    }
}
