![Smidge](assets/logo2.png?raw=true) Smidge
======

> Makes things smaller and better

A lightweight **ASP.Net 5** library for runtime CSS and JavaScript file management, minification, combination & compression. 

##Work in progress

I haven't had time to document all of the features and extensibility points just yet and some of them are not quite finished but all of the usages documented below work.

##Usage

### Install

In Startup.ConfigureServices:

    services.AddSmidge();
    
In Startup.Configure

    app.UseSmidge();

Add a config file to your app root (not wwwroot) called **smidge.json** with this content... you can of course change configure this content.

    {
        "debug": false,                     //true to enable file processing
        "dataFolder": "App_Data/Smidge",    //where the cache files are stored
        "version":  "1"                     //can be any string
    }

In _ViewStart.cshtml add an injected service:

    @inject SmidgeHelper Smidge

### View based declarations:

Require multiple files

    @{ Smidge.RequiresJs("~/Js/test1.js", "Js/test2.js"); }

Require a folder - optionally you can also include filters (i.e. this includes all .js files)

    @{ Smidge.RequiresJs("Js/Folder*js"); }

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

### Rendering

Rendering is done async, examples:

    @await Smidge.CssHereAsync()
    @await Smidge.JsHereAsync()
    @await Smidge.JsHereAsync("test-bundle-1")
    @await Smidge.JsHereAsync("test-bundle-2")

## Contribution

This is currently still a work in progress, I'm hoping to get an alpha out within the next couple of weeks. In the meantime you can test it out and/or get involved, any help/contributions would be fantastic.

Some of the logic for this application has been ported over from [CDF (Client Dependency Framework)](https://github.com/Shazwazza/ClientDependency).

## Copyright & Licence

&copy; 2014 by Shannon Deminick

This is free software and is licensed under the [MIT License](http://opensource.org/licenses/MIT)

Logo image <a href="http://www.freepik.com">Designed by Freepik</a>
