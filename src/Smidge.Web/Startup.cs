using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.Routing;
using Smidge;
using Smidge.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using System.Threading.Tasks;
using Smidge.Controllers;
using Smidge.Options;
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
                        new JavaScriptFile("~/Js/Bundle1/a2.js"));

                    bundles.Create("test-bundle-2", WebFileType.Js, "~/Js/Bundle2");

                    bundles.Create("test-bundle-3", bundles.PipelineFactory.GetPipeline(typeof(JsMin)), WebFileType.Js, "~/Js/Bundle2");
                });
        }

        public void Configure(IApplicationBuilder app)
        {

            app.UseMvc(routes =>
            {
                routes.MapRoute("Default", "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSmidge();
        }
    }
}
