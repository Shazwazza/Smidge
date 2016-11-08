using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Smidge.FileProcessors;

namespace Smidge.JavaScriptServices
{
    public static class SmidgeJavaScriptServicesStartup
    {
        public static IServiceCollection AddSmidgeJavaScriptServices(this IServiceCollection services,
            IConfiguration smidgeConfiguration = null,
            IFileProvider fileProvider = null)
        {
            services.AddSingleton<SmidgeJavaScriptServices>(provider =>
            {
                var env = provider.GetRequiredService<IHostingEnvironment>();
                return new SmidgeJavaScriptServices(NodeServicesFactory.CreateNodeServices(
                    new NodeServicesOptions(provider)
                    {
                        ProjectPath = env.ContentRootPath,
                        WatchFileExtensions = new string[] { }
                    }));
            });

            //pre processors
            services.AddSingleton<IPreProcessor, UglifyNodeMinifier>();

            return services;
        }

        //_allProcessors.OfType<UglifyNodeMinifier>().Single()
    }
}