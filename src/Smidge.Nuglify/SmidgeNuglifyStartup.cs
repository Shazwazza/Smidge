using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NUglify.JavaScript;
using Smidge.FileProcessors;

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
        public static IServiceCollection AddSmidgeNuglifyServices(this IServiceCollection services,
            NuglifySettings nuglifySettings = null)
        {
            //pre processors
            services.AddSingleton<IPreProcessor, NuglifyCss>();
            services.AddSingleton<IPreProcessor, NuglifyJs>();

            services.AddSingleton<NuglifySettings>(provider => nuglifySettings ?? new NuglifySettings(new NuglifyCodeSettings(null), new NuglifyCodeSettings(null)));
            
            return services;
        }        
    }
}