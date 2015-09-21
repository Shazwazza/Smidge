![Smidge](assets/logosmall.png?raw=true) Smidge
======

A lightweight **ASP.Net 5** library for runtime CSS and JavaScript file management, minification, combination & compression. 

## Install

_Currently supporting DNX 4.5.1 & DNXCore 5.0, built against **beta7**_

Nuget:

    Install-Package Smidge -Pre

In Startup.ConfigureServices:

    services.AddSmidge();
    
In Startup.Configure

    app.UseSmidge();

Add a config file to your app root (not wwwroot) called **smidge.json** with this content:

    {
        "debug": false,                     //true to disable file processing
        "dataFolder": "App_Data/Smidge",    //where the cache files are stored
        "version":  "1"                     //can be any string
    }

Create a file in your ~/Views folder:  `ViewImports.cshtml`
(This is an MVC 6 way of injecting services into all of your views)
In `_ViewImports.cshtml` add an injected service:

    @inject Smidge.SmidgeHelper Smidge

_NOTE: There is a website example project in this source for a reference: https://github.com/Shazwazza/Smidge/tree/master/src/Smidge.Web_

##Usage

### View based declarations:

Require multiple files

    @{ Smidge.RequiresJs("~/Js/test1.js", "Js/test2.js"); }

Require a folder - optionally you can also include filters (i.e. this includes all .js files)

    @{ Smidge.RequiresJs("Js/Stuff*js"); }

Chaining:

    @{ Smidge
        //external resources work too!
        .RequiresJs("//cdnjs.cloudflare.com/ajax/libs/jquery/2.1.1/jquery.min.js")
        .RequiresJs("Js/Folder*js")
        .RequiresCss("Css/test1.css", "Css/test2.css", "Css/test3.css", "Css/test4.css");  
    }

### Pre-defined bundles

Define your bundles during startup:

    services.AddSmidge()
        .Configure<Bundles>(bundles =>
        {
            //Defining using JavaScriptFile's or CssFile's:

            bundles.Create("test-bundle-1", //bundle name
                new JavaScriptFile("~/Js/Bundle1/a1.js"),
                new JavaScriptFile("~/Js/Bundle1/a2.js"));

            //Or defining using file/folder paths:

            bundles.Create("test-bundle-2", WebFileType.Js, 
                "~/Js/Bundle2", "~/Js/OtherFolder*js");
        });

_There are quite a few overloads for creating bundles._

### Rendering

Examples of how you output the `<link>` and `<script>` html tags for you assets (rendering is done async):

    @await Smidge.CssHereAsync()
    @await Smidge.JsHereAsync()
    @await Smidge.JsHereAsync("test-bundle-1")
    @await Smidge.JsHereAsync("test-bundle-2")

### Custom pre-processing pipeline

It's easy to customize how your files are processed. This can be done at a global/default level, at the bundle level or at an individual file level.

Each processor is of type `Smidge.FileProcessors.IPreProcessor` which contains a single method: `Task<string> ProcessAsync(FileProcessContext fileProcessContext);`. The built-in processors are:

* `CssImportProcessor`
* `CssUrlProcessor`
* `CssMinifier`
* `JsMin`

But you can create and add your own just by adding the instance to the IoC container, for example if you created a dotless processor::

`services.AddScoped<IPreProcessor, DotLessProcessor>();`

##### Global custom pipeline

If you want to override the default processing pipeline for all files, then you'd add your own implementation of `Smidge.FileProcessors.PreProcessPipelineFactory` to the IoC container after you've called `AddSmidge();`, like:

`services.AddSingleton<PreProcessPipelineFactory, MyCustomPreProcessPipelineFactory>();`

and override the `GetDefault` method. You can see the default implementation here: https://github.com/Shazwazza/Smidge/blob/master/src/Smidge/FileProcessors/PreProcessPipelineFactory.cs

##### Individual file custom pipeline 

If you want to customize the pipeline for any given file it's really easy. Each registered file is of type `Smidge.Models.IFile` which contains a property called `Pipeline` of type `Smidge.FileProcessors.PreProcessPipeline`. So if you wanted to customize the pipeline for a single JS file, you could do something like:

    @inject Smidge.FileProcessors.PreProcessPipelineFactory PipelineFactory
    
    @{ Smidge.RequiresJs(new JavaScriptFile("~/Js/test2.js")
            {
                Pipeline = PipelineFactory.GetPipeline(
                    //add as many processor types as you want
                    typeof(DotLess), typeof(JsMin))
            })

##### Bundle level custom pipeline

If you want to customize the pipeline for a particular bundle, you can just create your bundle with a custom pipeline like:

    services.AddSmidge()
        .Configure<Bundles>(bundles =>
        {                   
            bundles.Create("test-bundle-3", 
                bundles.PipelineFactory.GetPipeline(
                    //add as many processor types as you want
                    typeof(DotLess), typeof(JsMin)), 
                WebFileType.Js, 
                "~/Js/Bundle2");
        });
        
_There are quite a few overloads for creating bundles with custom pipelines._

### URLs

There's a couple of methods you can use retrieve the URLs that Smidge will generate when rendering the `<link>` or `<script>` html tags. This might be handy in case you need to load in these assets manually (i.e. lazy load scripts, etc...):

    Task<IEnumerable<string>> SmidgeHelper.GenerateJsUrlsAsync()
    Task<IEnumerable<string>> SmidgeHelper.GenerateCssUrlsAsync()
    
Both of these methods return a list of strings and there are several overloads for each which allow you to generate the URLs for pre-defined bundles, or the URLs for runtime registered dependencies. Examples of this can be seen in the demo web site's Index.cshtml: https://github.com/Shazwazza/Smidge/blob/master/src/Smidge.Web/Views/Home/Index.cshtml

##Work in progress

I haven't had time to document all of the features and extensibility points just yet and some of them are not quite finished but all of the usages documented above work.

## Copyright & Licence

&copy; 2015 by Shannon Deminick

This is free software and is licensed under the [MIT License](http://opensource.org/licenses/MIT)

Logo image <a href="http://www.freepik.com">Designed by Freepik</a>
