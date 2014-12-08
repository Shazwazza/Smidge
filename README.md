![Smidge](assets/logo2.png?raw=true) Smidge
======

A lightweight **ASP.Net 5** library for runtime CSS and JavaScript file minification, combination & compression. 

##Usage

### Install

In Startup.ConfigureServices:

    services.AddSmidge();
    
In Startup.Configure

    app.UseSmidge();

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

*NOTE:View based declarations are really really simple... but this will not work in a load balanced scenario, pre-defined bundles can be used for load balancing.* 

### Pre-defined bundles

//TODO: I need to write these docs :)

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
