[![Build status](https://ci.appveyor.com/api/projects/status/y2c08r2wsqsliq7o?svg=true)](https://ci.appveyor.com/project/Shandem/smidge)

![Smidge](assets/logosmall.png?raw=true) Smidge
======

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Smidge.svg)](https://www.nuget.org/packages/Smidge)

A lightweight __runtime__ CSS/JavaScript file minification, combination, compression & management library for **ASP.Net Core**

* [Alpha 2.0 is out!](http://shazwazza.com/post/smidge-20-alpha-is-out/)

## Features

* _Compatible with **NETStandard.1.6** & **.Net Framework 4.5.2**_
* OOTB comes with minification, combination, compression for JS/CSS files
* Properly configurd client side caching, persistent server side caching (no rebunding unecessarily)
* Fluent syntax for creating and configuring bundles
* Debug/Production configurations for each bundle
* Cache busting - and you can customize/replace how it works
* JS source maps
* Extensible - you can completely customize the pre-processor pipeline and create your own processors and for any file type

## Quick Start

1. Install from Nuget: 
	```
	Install-Package Smidge -Pre
	```
1. Add Smidge config to appsettings.json:
	```json
	"smidge": {
	    "dataFolder" : "App_Data/Smidge",
	    "version" : "1"
	  }  
	```
1. Add smidge to your services:
	```csharp	
	services.AddSmidge(Configuration.GetSection("smidge"));
	```
1. Create a bundle in your configure method:
	```csharp	
	app.UseSmidge(bundles =>
	{
	   bundles.Create("my-application", WebFileType.Js, "~/js/site.js", "~/js/app");
	});
	```
1. Add the tag helpers to your _ViewImports.cshtml file:
	```csharp
	@addTagHelper *, Smidge
	```
1. Render your bundle:
	```html
	<script src="my-application" type="text/javascript"></script>
	```

__[See Installation](https://github.com/Shazwazza/Smidge/wiki/installation) for full configuration details__

## Usage

### Pre-defined bundles

Define your bundles during startup:

```csharp
services.UseSmidge(bundles =>
    {
        //Defining using file/folder paths:
	
	bundles.Create("test-bundle-2", WebFileType.Js, 
            "~/Js/Bundle2", "~/Js/OtherFolder*js");

	//Or defining using JavaScriptFile's or CssFile's:
	
        bundles.Create("test-bundle-1", //bundle name
            new JavaScriptFile("~/Js/Bundle1/a1.js"),
            new JavaScriptFile("~/Js/Bundle1/a2.js"));
        
	//Then there's all sorts of options for configuring bundles with regards to customizing their pipelines,
	//customizing how rendering is done based on Debug or Production environments, if you want to 
	//enable file watchers, configure custom cache busters or the cache control options, etc...
	//There's even a fluent API to do this! Example: 

	bundles.Create("test-bundle-3", WebFileType.Js, "~/Js/Bundle3")
	  .WithEnvironmentOptions(BundleEnvironmentOptions.Create()
	     .ForDebug(builder => builder
	        .EnableCompositeProcessing()
	        .EnableFileWatcher()
	        .SetCacheBusterType<AppDomainLifetimeCacheBuster>()
	        .CacheControlOptions(enableEtag: false, cacheControlMaxAge: 0))
	     .Build()
	);
    });
```

If you don't want to create named bundles and just want to declare dependencies individually inside your Views, you can do that too! You can create bundles (named or unnamed) during runtime ... no problem.

__[See Declarations](https://github.com/Shazwazza/Smidge/wiki/Declarations) for full declaration/usage details__

### Rendering

The easiest way to render bundles is simply by the bundle name:

```html
<script src="my-awesome-js-bundle" type="text/javascript"></script>
<link rel="stylesheet" href="my-cool-css-bundle"/>
```
    
This uses Smidge's custom tag helpers to check if the source is a bundle reference and will output the correct bundle URL. You can combine this with environment variables for debug/non-debug modes. Alternatively, you can also use Razor to do the rendering.

__[See Rendering](https://github.com/Shazwazza/Smidge/wiki/Rendering) for full rendering & debug mode details__

### Custom pre-processing pipeline

It's easy to customize how your files are processed including the way files are minified, how URLs are resolved, etc.... 
This can be done at a global/default level, at the bundle level or at an individual file level.

__[See Custom Pre-Processing Pipeline](https://github.com/Shazwazza/Smidge/wiki/Custom-pre-processing) for information about customizing the pre-process pipeline__

### URLs

There's a couple of methods you can use retrieve the URLs that Smidge will generate when rendering the `<link>` or `<script>` html tags. This might be handy in case you need to load in these assets manually (i.e. lazy load scripts, etc...):

```csharp
Task<IEnumerable<string>> SmidgeHelper.GenerateJsUrlsAsync()
Task<IEnumerable<string>> SmidgeHelper.GenerateCssUrlsAsync()
```

__[See Asset URLs](https://github.com/Shazwazza/Smidge/wiki/Asset-Urls) for information about retrieving the debug and non-debug asset urls for your bundles__    

## Documentation

__[All of the documentation lives here](https://github.com/Shazwazza/Smidge/wiki)__

I haven't had time to document all of the features and extensibility points just yet but I'm working on it :)

## Copyright & Licence

&copy; 2017 by Shannon Deminick

This is free software and is licensed under the [MIT License](http://opensource.org/licenses/MIT)

Logo image <a href="http://www.freepik.com">Designed by Freepik</a>
