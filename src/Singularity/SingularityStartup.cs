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
            services.AddSingleton<FileCacheManager>();
            services.AddSingleton<SingularityConfig>();
            services.AddScoped<SingularityContext>();
            services.AddScoped<SingularityHelper>();            
            services.AddSingleton<IFileMapProvider, XmlFileMapProvider>();
            services.AddTransient<Base64UrlCreatorOptions>(x => new Base64UrlCreatorOptions
            {
                MaxUrlLength = 2048,
                RequestHandlerPath = "sg64",
                Version = "1"
            });
            services.AddSingleton<IUrlCreator, Base64UrlCreator>();
        }

        public static void UseSingularity(this IApplicationBuilder app)
        {

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "SingularityBase64",
                    "sg64/{id?}",
                    new { controller = "Singularity", action = "Base64" });
            });
        }
    }
}
