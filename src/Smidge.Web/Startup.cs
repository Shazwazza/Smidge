using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smidge.Cache;
using Smidge.Options;
using Smidge.Models;
using Smidge.FileProcessors;
using Smidge.Nuglify;
using Microsoft.Extensions.Hosting;
using Smidge.InMemory;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Smidge.Web
{
    //public class DotlessPreProcessor : IPreProcessor
    //{
    //    private readonly IHostingEnvironment _hostingEnvironment;

    //    public DotlessPreProcessor(IHostingEnvironment hostingEnvironment)
    //    {
    //        _hostingEnvironment = hostingEnvironment;
    //    }

    //    public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
    //    {
    //        if (Path.GetExtension(fileProcessContext.WebFile.FilePath) == ".less")
    //        {
    //            var result = dotless.Core.Less.Parse(fileProcessContext.FileContent);
    //            fileProcessContext.Update(result);
    //        }

    //        await next(fileProcessContext);
    //    }
    //}

    public class Startup
    {

        // Entry point for the application.
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();        

        public IConfigurationRoot Configuration { get; }
        public IWebHostEnvironment CurrentEnvironment { get; }

        /// <summary>
        /// Constructor sets up the configuration - for our example we'll load in the config from appsettings.json with
        /// a sub configuration value of 'smidge'
        /// </summary>
        /// <param name="env"></param>
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            Configuration = builder.Build();
            CurrentEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<ISmidgeFileProvider>(f =>
            {
                var hostEnv = f.GetRequiredService<IWebHostEnvironment>();

                return new SmidgeFileProvider(
                    hostEnv.WebRootFileProvider,
                    new PhysicalFileProvider(Path.Combine(hostEnv.ContentRootPath, "Smidge", "Static")));
            });

            // Or use services.AddSmidge() to test from smidge.json config.
            services.AddSmidge(Configuration.GetSection("smidge"));

            // We could replace a processor in the default pipeline like this
            //services.Configure<SmidgeOptions>(opt =>
            //{
            //    opt.PipelineFactory.OnCreateDefault = (type, pipeline) => pipeline.Replace<JsMinifier, NuglifyJs>(opt.PipelineFactory);                
            //});

            // We could change a lot of defaults like this
            services.Configure<SmidgeOptions>(options =>
            {
                //options.PipelineFactory.OnCreateDefault = (type, processors) =>
                //options.FileWatchOptions.Enabled = true;
                //options.PipelineFactory.OnCreateDefault = GetDefaultPipelineFactory;

                options.DefaultBundleOptions.DebugOptions.SetCacheBusterType<AppDomainLifetimeCacheBuster>();
                options.DefaultBundleOptions.ProductionOptions.SetCacheBusterType<AppDomainLifetimeCacheBuster>();
            });

            services.AddSmidgeNuglify();
            services.AddSmidgeInMemory();

            //services.AddSingleton<IPreProcessor, DotlessPreProcessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(CurrentEnvironment.ContentRootPath, "Smidge", "Static")),
                RequestPath = "/smidge-static"
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                        name: "Default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");                
            });

            app.UseSmidge(bundles =>
            {
                //Create pre-defined bundles

                //var lessPipeline = bundles.PipelineFactory.DefaultCss();
                //lessPipeline.Processors.Insert(0, bundles.PipelineFactory.Resolve<DotlessPreProcessor>());
                //bundles.CreateCss(
                //    "less-test",
                //    lessPipeline,
                //    "~/Css/test.less")
                //    .WithEnvironmentOptions(BundleEnvironmentOptions.Create()
                //        .ForDebug(builder => builder.EnableCompositeProcessing().SetCacheBusterType<AppDomainLifetimeCacheBuster>())
                //        .Build());

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
                
                bundles.CreateJs("test-bundle-2", "~/Js/Bundle2")
                    .WithEnvironmentOptions(BundleEnvironmentOptions.Create()
                            .ForDebug(builder => builder
                                .EnableCompositeProcessing()
                                .EnableFileWatcher()
                                .SetCacheBusterType<AppDomainLifetimeCacheBuster>()
                                .CacheControlOptions(enableEtag: false, cacheControlMaxAge: 0))
                            .Build()
                    );

                bundles.Create("test-bundle-3", WebFileType.Js, "~/Js/Bundle2");

                bundles.Create("test-bundle-4",
                    new CssFile("~/Css/Bundle1/a1.css"),
                    new CssFile("~/Css/Bundle1/a2.css"));

                bundles.CreateJs("libs-js",
                    //Here we can change the default pipeline to use Nuglify for this single bundle
                    bundles.PipelineFactory.Create<NuglifyJs>(),
                    "~/Js/Libs/jquery-1.12.2.js", "~/Js/Libs/knockout-es5.js");

                bundles.CreateCss("libs-css",
                    //Here we can change the default pipeline to use Nuglify for this single bundle (we'll replace the default)
                    bundles.PipelineFactory.DefaultCss().Replace<CssMinifier, NuglifyCss>(bundles.PipelineFactory),
                    "~/Css/Libs/font-awesome.css");

                bundles.Create("test-bundle-10", new JavaScriptFile("~/test10.js")
                {
                    RequestPath = "/smidge-static"
                });

                bundles
             .CreateCss("notfound-map-css-bundle",
             "~/Css/notFoundMap.min.css"
             );
            });

            app.UseSmidgeNuglify();
        }
    }
}
