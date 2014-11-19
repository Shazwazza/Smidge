using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.ConfigurationModel;
using Singularity.CompositeFiles;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Hosting;

namespace Singularity
{
    public static class SingularityStartup
    {
        public static void AddSingularity(this IServiceCollection services)
        {
            services.AddSingleton<FileMinifyManager>();
            services.AddSingleton<SingularityConfig>();
            services.AddScoped<SingularityContext>();
            services.AddScoped<SingularityHelper>();            
            services.AddSingleton<IFileMapProvider, XmlFileMapProvider>();
            services.AddTransient<UrlCreatorOptions>(x => new UrlCreatorOptions
            {
                MaxUrlLength = 2048,
                //RequestHandlerPath = "sg64"
                RequestHandlerPath = "sgd"
            });
            //services.AddSingleton<IUrlCreator, Base64UrlCreator>();
            services.AddSingleton<IUrlCreator, DelimitedUrlCreator>();
            
        }

        public static void UseSingularity(this IApplicationBuilder app)
        {

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "SingularityBase64",
                    "sg64/{id?}",
                    new { controller = "Singularity", action = "Base64" });

                routes.MapRoute(
                    "SingularityDelimited",
                    "sgd/{id?}",
                    new { controller = "Singularity", action = "Delimited" });
            });
        }
    }
}
