using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.ConfigurationModel;
using Fuze.CompositeFiles;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Hosting;

namespace Fuze
{
    public static class FuzeStartup
    {
        public static void AddFuze(this IServiceCollection services)
        {
            services.AddSingleton<FileSystemHelper>();
            services.AddSingleton<FileMinifyManager>();
            services.AddSingleton<FuzeConfig>();
            services.AddScoped<FuzeContext>();
            services.AddScoped<FuzeHelper>();            
            services.AddTransient<UrlCreatorOptions>(x => new UrlCreatorOptions
            {
                MaxUrlLength = 2048,
                RequestHandlerPath = "sg"
            });
            services.AddSingleton<IUrlCreator, DelimitedUrlCreator>();
            
        }

        public static void UseFuze(this IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {               
                routes.MapRoute(
                    "Fuze",
                    "sg/{id?}",
                    new { controller = "Fuze", action = "Index" });
            });
        }
    }
}
