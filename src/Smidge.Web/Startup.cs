using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using NUglify.JavaScript;
using Smidge.Cache;
using Smidge.Options;
using Smidge.Models;
using Smidge.FileProcessors;
using Smidge.JavaScriptServices;
using Smidge.Nuglify;

namespace Smidge.Web
{
    public class Startup
    {
        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseStartup<Startup>()
              .Build();

            host.Run();
        }

        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor sets up the configuration - for our example we'll load in the config from appsettings.json with
        /// a sub configuration value of 'smidge'
        /// </summary>
        /// <param name="env"></param>
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            var config = builder.Build();
            _config = config.GetSection("smidge");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ApplicationEnvironment>();

            services.AddMvc();

            // Or use services.AddSmidge() to test from smidge.json config.
            services.AddSmidge(_config)
                .Configure<SmidgeOptions>(options =>
                {
                    //options.FileWatchOptions.Enabled = true;
                    options.PipelineFactory.OnGetDefault = GetDefaultPipelineFactory;
                    options.DefaultBundleOptions.DebugOptions.SetCacheBusterType<AppDomainLifetimeCacheBuster>();
                    options.DefaultBundleOptions.ProductionOptions.SetCacheBusterType<AppDomainLifetimeCacheBuster>();
                });

            services.AddSmidgeJavaScriptServices();
            services.AddSmidgeNuglify();
        }

        /// <summary>
        /// A callback used to modify the default pipeline to use Nuglify for JS processing
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="processors"></param>
        /// <returns></returns>
        private static PreProcessPipeline GetDefaultPipelineFactory(WebFileType fileType, IReadOnlyCollection<IPreProcessor> processors)
        {
            //switch (fileType)
            //{
            //    case WebFileType.Js:
            //        return new PreProcessPipeline(new IPreProcessor[]
            //        {
            //            processors.OfType<NuglifyJs>().Single()
            //        });
            //}
            //returning null will fallback to the logic defined in the registered PreProcessPipelineFactory
            return null;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // sends the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute("Default", "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSmidge(bundles =>
            {
                //Create pre-defined bundles

                bundles.Create("test-bundle-1",                    
                    new JavaScriptFile("~/Js/Bundle1/a1.js"),
                    new JavaScriptFile("~/Js/Bundle1/a2.js"),
                    //NOTE: This is already min'd based on it's file name, therefore
                    // by convention JsMin should be removed
                    new JavaScriptFile("~/Js/Bundle1/a3.min.js"))
                    .WithEnvironmentOptions(bundles.DefaultBundleOptions)
                    .OnOrdering(collection =>
                    {
                        //return some custom ordering
                        return collection.OrderBy(x => x.FilePath);
                    });
                
                bundles.Create("test-bundle-2", WebFileType.Js, "~/Js/Bundle2")
                    .WithEnvironmentOptions(BundleEnvironmentOptions.Create()
                            .ForDebug(builder => builder
                                .EnableCompositeProcessing()
                                .EnableFileWatcher()
                                .SetCacheBusterType<AppDomainLifetimeCacheBuster>()
                                .CacheControlOptions(enableEtag: false, cacheControlMaxAge: 0))
                            .Build()
                    );

                bundles.Create("test-bundle-3", bundles.PipelineFactory.GetPipeline(typeof(JsMinifier)), WebFileType.Js, "~/Js/Bundle2");

                bundles.Create("test-bundle-4",
                    new CssFile("~/Css/Bundle1/a1.css"),
                    new CssFile("~/Css/Bundle1/a2.css"));

                bundles.Create("libs-js",
                    //Here we can change the default pipeline to use Nuglify for this single bundle
                    bundles.PipelineFactory.GetPipeline(typeof(NuglifyJs)),
                    WebFileType.Js, "~/Js/Libs/jquery-1.12.2.js", "~/Js/Libs/knockout-es5.js");

                bundles.Create("libs-css", WebFileType.Css, "~/Css/Libs/font-awesome.css");
            });

            app.UseSmidgeNuglify();
        }
    }
}
