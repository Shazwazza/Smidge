using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NUglify.Css;
using NUglify.JavaScript;
using Smidge.FileProcessors;
using Smidge.Options;

namespace Smidge.Nuglify
{
    public static class SmidgeNuglifyStartup
    {
        /// <summary>
        /// Adds nuglify services for smidge with optional code settings
        /// </summary>
        /// <param name="services"></param>
        /// <param name="nuglifySettings"></param>
        /// <returns></returns>
        public static IServiceCollection AddSmidgeNuglify(this IServiceCollection services,
            NuglifySettings nuglifySettings = null)
        {
            //pre processors
            services.AddSingleton<IPreProcessor, NuglifyCss>();
            services.AddSingleton<IPreProcessor, NuglifyJs>();
            services.AddSingleton<ISourceMapDeclaration, SourceMapDeclaration>();

            services.AddSingleton<NuglifySettings>(provider => nuglifySettings ?? new NuglifySettings(new NuglifyCodeSettings(null), new CssSettings()));
            
            return services;
        }

        public static void UseSmidgeNuglify(this IApplicationBuilder app, bool useEndpointRouting = true)
        {
#if NETCORE3_0
            //NOTE: It's no longer polite to just call UseMVC as it enables things that the developer may 
            //not need and the dev must disable EndpointRouting - so we let the dev decide.
            //with core 3.0 you have to explicitly disable EndpointRouting se we default to on here 
            if (useEndpointRouting)
            {
                //Create custom route
                app.UseEndpoints(endpoints =>
                {
                    var options = app.ApplicationServices.GetRequiredService<IOptions<SmidgeOptions>>();

                    endpoints.MapControllerRoute(
                        "SmidgeNuglifySourceMap",
                        options.Value.UrlOptions.BundleFilePath + "/nmap/{bundle}",
                        new { controller = "NuglifySourceMap", action = "SourceMap" });
                });

            }
            else
            {
#endif
                //Create custom route
                app.UseMvc(routes =>
                {
                    var options = app.ApplicationServices.GetRequiredService<IOptions<SmidgeOptions>>();

                    routes.MapRoute(
                        "SmidgeNuglifySourceMap",
                        options.Value.UrlOptions.BundleFilePath + "/nmap/{bundle}",
                        new { controller = "NuglifySourceMap", action = "SourceMap" });
                });
#if NETCORE3_0
            }
#endif

            

        }
    }
}