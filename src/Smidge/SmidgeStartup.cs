using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.ConfigurationModel;
using Smidge.CompositeFiles;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Hosting;

namespace Smidge
{
    public static class SmidgeStartup
    {
        public static void AddSmidge(
            this IServiceCollection services, 
            SmidgeOptions options = null,
            Action<BundleManager> createBundles = null)
        {
            if (options == null)
            {
                options = new SmidgeOptions();
            }            

            services.AddSingleton<SmidgeOptions>(provider => options);
            services.AddSingleton<IHasher>(provider => provider.GetRequiredService<SmidgeOptions>().Hasher);
            services.AddSingleton<BundleManager>(provider => new BundleManager(provider.GetRequiredService<FileSystemHelper>(), createBundles));
            services.AddSingleton<FileSystemHelper>();
            services.AddSingleton<FileMinifyManager>();
            services.AddSingleton<SmidgeConfig>();
            services.AddScoped<SmidgeContext>();
            services.AddScoped<SmidgeHelper>();
            services.AddTransient<UrlCreatorOptions>(x => new UrlCreatorOptions
            {
                MaxUrlLength = 2048,
                RequestHandlerPath = "sg"
            });
            services.AddSingleton<IUrlCreator, DelimitedUrlCreator>();
            
        }

        public static void UseSmidge(this IApplicationBuilder app)
        {

            //Create custom route
            app.UseMvc(routes =>
            {               
                routes.MapRoute(
                    "Smidge",
                    "sg/{id?}",
                    new { controller = "Smidge", action = "Index" });
            });
            

        }
    }
}
