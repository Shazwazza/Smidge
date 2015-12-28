using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Options;
using Smidge.Models;
using Smidge.FileProcessors;

namespace Smidge.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
          
            services.AddMvc();

            services.AddSmidge()
                .Configure<SmidgeOptions>(options =>
                {                    
                })
                .Configure<Bundles>(bundles =>
                {
                    bundles.Create("test-bundle-1",
                        new JavaScriptFile("~/Js/Bundle1/a1.js"),
                        new JavaScriptFile("~/Js/Bundle1/a2.js"))
                        .OnOrdering(collection =>
                        {
                            //return some custom ordering
                            return collection.OrderBy(x => x.FilePath);
                        });

                    bundles.Create("test-bundle-2", WebFileType.Js, "~/Js/Bundle2");

                    bundles.Create("test-bundle-3", bundles.PipelineFactory.GetPipeline(typeof(JsMin)), WebFileType.Js, "~/Js/Bundle2");
                    
                    bundles.Create("test-bundle-4",
                        new CssFile("~/Css/Bundle1/a1.css"),
                        new CssFile("~/Css/Bundle1/a2.css"));
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseIISPlatformHandler();

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

            app.UseSmidge();
        }
    }
}
