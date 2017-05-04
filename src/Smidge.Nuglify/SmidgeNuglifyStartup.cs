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

        public static void UseSmidgeNuglify(this IApplicationBuilder app)
        {
            //Create custom route
            app.UseMvc(routes =>
            {
                var options = app.ApplicationServices.GetRequiredService<IOptions<SmidgeOptions>>();

                routes.MapRoute(
                    "SmidgeNuglifySourceMap",
                    options.Value.UrlOptions.BundleFilePath + "/nmap/{bundle}",
                    new {controller = "NuglifySourceMap", action = "SourceMap" });
            });

        }
    }
}