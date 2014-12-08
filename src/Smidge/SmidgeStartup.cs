using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.ConfigurationModel;
using Smidge.CompositeFiles;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Hosting;
using System.Runtime.CompilerServices;
using Smidge.Models;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public static class SmidgeStartup
    {
        public static void AddSmidge(
            this IServiceCollection services, 
            SmidgeOptions options = null,
            Action<BundleManager> createBundles = null)
        {

            //Enables memory cache
            services.AddCachingServices();

            if (options == null)
            {
                options = new SmidgeOptions();
            }            

            services.AddSingleton<SmidgeOptions>(provider => options);
            services.AddSingleton<IHasher>(provider => provider.GetRequiredService<SmidgeOptions>().Hasher);
            services.AddSingleton<BundleManager>(provider => new BundleManager(provider.GetRequiredService<FileSystemHelper>(), createBundles));
            services.AddSingleton<FileSystemHelper>();
            services.AddSingleton<FileMinifyManager>();
            services.AddSingleton<ISmidgeConfig, SmidgeConfig>();
            services.AddScoped<SmidgeContext>();
            services.AddScoped<SmidgeHelper>();
            services.AddTransient<UrlManagerOptions>(x => new UrlManagerOptions
            {
                MaxUrlLength = 2048,
                CompositeFilePath = "sc",
                BundleFilePath = "sb"
            });
            services.AddSingleton<IUrlManager, DefaultUrlManager>();

            //Add the controller models as DI services - these get auto created for model binding
            services.AddTransient<BundleModel>();
        }

        public static void UseSmidge(this IApplicationBuilder app)
        {

            //Create custom route
            app.UseMvc(routes =>
            {               
                routes.MapRoute(
                    "SmidgeComposite",
                    "sc/{id}",
                    new { controller = "Smidge", action = "Composite" });

                routes.MapRoute(
                    "SmidgeBundle",
                    "sb/{bundle}",
                    new { controller = "Smidge", action = "Bundle" });

            });
            

        }
    }
}
