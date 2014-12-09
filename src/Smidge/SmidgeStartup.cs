using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.ConfigurationModel;
using Smidge.CompositeFiles;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Hosting;
using System.Runtime.CompilerServices;
using Smidge.Models;
using Microsoft.Framework.OptionsModel;
using Smidge.Options;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public static class SmidgeStartup
    {
        public static IServiceCollection AddSmidge(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SmidgeOptions>, SmidgeOptionsSetup>();
            services.AddTransient<IConfigureOptions<Bundles>, BundlesSetup>();

            services.AddSingleton<DefaultFileProcessors>();
            services.AddSingleton<BundleManager>();
            services.AddSingleton<FileSystemHelper>();
            services.AddSingleton<FileMinifyManager>();
            services.AddSingleton<ISmidgeConfig, SmidgeConfig>();
            services.AddScoped<SmidgeContext>();
            services.AddScoped<SmidgeHelper>();
            services.AddSingleton<IUrlManager, DefaultUrlManager>();
            services.AddSingleton<IMinifier, JsMin>();
            services.AddSingleton<IMinifier, CssHelper>();
            services.AddSingleton<IHasher, Crc32Hasher>();

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
