using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Smidge.FileProcessors;

namespace Smidge.Nuglify
{
    public static class SmidgeNuglifyStartup
    {
        public static IServiceCollection AddSmidgeNuglifyServices(this IServiceCollection services,
            IConfiguration smidgeConfiguration = null,
            IFileProvider fileProvider = null)
        {
            //pre processors
            services.AddSingleton<IPreProcessor, NuglifyCss>();
            services.AddSingleton<IPreProcessor, NuglifyJs>();

            return services;
        }

        //_allProcessors.OfType<UglifyNodeMinifier>().Single()
    }
}