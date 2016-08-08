using Smidge.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using Smidge.Hashing;
using Smidge.Options;

namespace Smidge
{
    /// <summary>
    /// Used in views to register and render dependencies
    /// </summary>
    public class SmidgeHelper : ISmidgeRequire
    {
        private readonly DynamicallyRegisteredWebFiles _dynamicallyRegisteredWebFiles;
        private readonly PreProcessManager _preProcessManager;
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IHasher _hasher;
        private readonly BundleManager _bundleManager;
        private readonly FileBatcher _fileBatcher;
        private readonly PreProcessPipelineFactory _processorFactory;
        private readonly IUrlManager _urlManager;
        private readonly IRequestHelper _requestHelper;
        private readonly FileProcessingConventions _fileProcessingConventions;
        private readonly IHttpContextAccessor _httpContextAccessor;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dynamicallyRegisteredWebFiles"></param>
        /// <param name="preProcessManager"></param>
        /// <param name="fileSystemHelper"></param>
        /// <param name="hasher"></param>
        /// <param name="bundleManager"></param>
        /// <param name="processorFactory"></param>
        /// <param name="urlManager"></param>
        /// <param name="requestHelper"></param>
        /// <param name="fileProcessingConventions"></param>
        /// <param name="httpContextAccessor"></param>
        public SmidgeHelper(
            DynamicallyRegisteredWebFiles dynamicallyRegisteredWebFiles,
            PreProcessManager preProcessManager,
            FileSystemHelper fileSystemHelper,
            IHasher hasher,
            BundleManager bundleManager,
            PreProcessPipelineFactory processorFactory,
            IUrlManager urlManager,
            IRequestHelper requestHelper,
            FileProcessingConventions fileProcessingConventions,
            IHttpContextAccessor httpContextAccessor)
        {
            if (dynamicallyRegisteredWebFiles == null) throw new ArgumentNullException(nameof(dynamicallyRegisteredWebFiles));
            if (preProcessManager == null) throw new ArgumentNullException(nameof(preProcessManager));
            if (fileSystemHelper == null) throw new ArgumentNullException(nameof(fileSystemHelper));
            if (bundleManager == null) throw new ArgumentNullException(nameof(bundleManager));
            if (processorFactory == null) throw new ArgumentNullException(nameof(processorFactory));
            if (urlManager == null) throw new ArgumentNullException(nameof(urlManager));
            if (requestHelper == null) throw new ArgumentNullException(nameof(requestHelper));
            if (fileProcessingConventions == null) throw new ArgumentNullException(nameof(fileProcessingConventions));
            if (httpContextAccessor == null) throw new ArgumentNullException(nameof(httpContextAccessor));
            _processorFactory = processorFactory;
            _urlManager = urlManager;
            _requestHelper = requestHelper;
            _fileProcessingConventions = fileProcessingConventions;
            _httpContextAccessor = httpContextAccessor;
            _bundleManager = bundleManager;
            _preProcessManager = preProcessManager;
            _dynamicallyRegisteredWebFiles = dynamicallyRegisteredWebFiles;
            _fileSystemHelper = fileSystemHelper;
            _hasher = hasher;
            _fileBatcher = new FileBatcher(_fileSystemHelper, _requestHelper, hasher);
        }

        public async Task<HtmlString> JsHereAsync(string bundleName, bool debug = false)
        {
            var urls = await GenerateJsUrlsAsync(bundleName, debug);
            var result = new StringBuilder();

            foreach (var url in urls)
            {
                result.AppendFormat("<script src='{0}' type='text/javascript'></script>", url);
            }
            return new HtmlString(result.ToString());
        }

        public async Task<HtmlString> CssHereAsync(string bundleName, bool debug = false)
        {
            var urls = await GenerateCssUrlsAsync(bundleName, debug);
            var result = new StringBuilder();

            foreach (var url in urls)
            {
                result.AppendFormat("<link href='{0}' rel='stylesheet' type='text/css'/>", url);
            }
            return new HtmlString(result.ToString());
        }

        /// <summary>
        /// Renders the JS tags
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// TODO: Once the tags are rendered the collection on the context is cleared. Therefore if this method is called multiple times it will 
        /// render anything that has been registered as 'pending' but has not been rendered.
        /// </remarks>
        public async Task<HtmlString> JsHereAsync(PreProcessPipeline pipeline = null, bool debug = false)
        {
            var result = new StringBuilder();
            var urls = await GenerateJsUrlsAsync(pipeline, debug);
            foreach (var url in urls)
            {
                result.AppendFormat("<script src='{0}' type='text/javascript'></script>", url);
            }
            return new HtmlString(result.ToString());
        }

        /// <summary>
        /// Renders the CSS tags
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// TODO: Once the tags are rendered the collection on the context is cleared. Therefore if this method is called multiple times it will 
        /// render anything that has been registered as 'pending' but has not been rendered.
        /// </remarks>
        public async Task<HtmlString> CssHereAsync(PreProcessPipeline pipeline = null, bool debug = false)
        {
            var result = new StringBuilder();
            var urls = await GenerateCssUrlsAsync(pipeline, debug);
            foreach (var url in urls)
            {
                result.AppendFormat("<link href='{0}' rel='stylesheet' type='text/css'/>", url);
            }
            return new HtmlString(result.ToString());
        }

        /// <summary>
        /// Generates the list of URLs to render based on what is dynamically registered
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GenerateJsUrlsAsync(PreProcessPipeline pipeline = null, bool debug = false)
        {
            return await GenerateUrlsAsync(_dynamicallyRegisteredWebFiles.JavaScriptFiles, WebFileType.Js, pipeline, debug);
        }

        public async Task<IEnumerable<string>> GenerateJsUrlsAsync(string bundleName, bool debug = false)
        {
            return await GenerateBundleUrlsAsync(bundleName, ".js", debug);
        }

        /// <summary>
        /// Generates the list of URLs to render based on what is dynamically registered
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GenerateCssUrlsAsync(PreProcessPipeline pipeline = null, bool debug = false)
        {
            return await GenerateUrlsAsync(_dynamicallyRegisteredWebFiles.CssFiles, WebFileType.Css, pipeline, debug);
        }

        public async Task<IEnumerable<string>> GenerateCssUrlsAsync(string bundleName, bool debug = false)
        {
            return await GenerateBundleUrlsAsync(bundleName, ".css", debug);
        }

        /// <summary>
        /// Generates the URLs for a given bundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="fileExt"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> GenerateBundleUrlsAsync(string bundleName, string fileExt, bool debug)
        {
            var result = new List<string>();

            var bundle = _bundleManager.GetBundle(bundleName);
            if (bundle == null)
            {
                throw new BundleNotFoundException(bundleName);
            }

            //get the bundle options from the bundle if they have been set otherwise with the defaults
            var bundleOptions = debug
                ? (bundle.BundleOptions == null ? _bundleManager.DefaultBundleOptions.DebugOptions : bundle.BundleOptions.DebugOptions)
                : (bundle.BundleOptions == null ? _bundleManager.DefaultBundleOptions.ProductionOptions : bundle.BundleOptions.ProductionOptions);                
            
            //if not processing as composite files, then just use their native file paths
            if (!bundleOptions.ProcessAsCompositeFile)
            {                
                var files = _bundleManager.GetFiles(bundleName);
                foreach (var d in files)
                {
                    result.Add(d.FilePath);
                }                
                return result;
            }

            var compression = bundleOptions.CompressResult 
                ? _requestHelper.GetClientCompression(_httpContextAccessor.HttpContext.Request.Headers) 
                : CompressionType.none;
            var url = _urlManager.GetUrl(bundleName, fileExt);

            //now we need to determine if these files have already been minified
            var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, bundleName);
            if (!File.Exists(compositeFilePath))
            {
                var files = _bundleManager.GetFiles(bundleName);
                //we need to do the minify on the original files
                foreach (var file in files)
                {
                    await _preProcessManager.ProcessAndCacheFileAsync(file, bundleOptions.FileWatchOptions);
                }
            }
            result.Add(url);
            return result;
        }

        /// <summary>
        /// Generates the URLs for a dynamically registered set of files (non pre-defined bundle)
        /// </summary>
        /// <param name="files"></param>
        /// <param name="fileType"></param>
        /// <param name="pipeline"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> GenerateUrlsAsync(
            IEnumerable<IWebFile> files,
            WebFileType fileType,
            PreProcessPipeline pipeline = null,
            bool debug = false)
        {
            var result = new List<string>();

            var orderedSet = new OrderedFileSet(files,
                _fileSystemHelper, _requestHelper,
                pipeline ?? _processorFactory.GetDefault(fileType),
                _fileProcessingConventions);
            var orderedFiles = orderedSet.GetOrderedFileSet();

            if (debug)
            {
                return orderedFiles.Select(x => x.FilePath);
            }

            var compression = _requestHelper.GetClientCompression(_httpContextAccessor.HttpContext.Request.Headers);
                
            //Get the file collection used to create the composite URLs and the external requests
            var fileBatches = _fileBatcher.GetCompositeFileCollectionForUrlGeneration(orderedFiles);

            foreach (var batch in fileBatches)
            {
                //if it's external, the rule is that a WebFileBatch can only contain a single external file
                // it's path will be normalized as an external url so we just use it
                if (batch.IsExternal)
                {
                    result.Add(batch.Single().Original.FilePath);
                }
                else
                {
                    //Get the URLs for the batch, this could be more than one resulting URL depending on how many
                    // files are in the batch and the max url length
                    var compositeUrls = _urlManager.GetUrls(batch.Select(x => x.Hashed), fileType == WebFileType.Css ? ".css" : ".js");

                    foreach (var u in compositeUrls)
                    {
                        //now we need to determine if these files have already been minified
                        var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, u.Key);
                        if (!File.Exists(compositeFilePath))
                        {
                            //need to process/minify these files - need to use their original paths of course
                            foreach (var file in batch.Select(x => x.Original))
                            {
                                await _preProcessManager.ProcessAndCacheFileAsync(file,
                                    //TODO: Need to make global bundle options for dynamically registered files
                                    new Options.FileWatchOptions
                                    {
                                        Enabled = false
                                    });
                            }
                        }
                        result.Add(u.Url);
                    }
                }
            }

            return result;

        }

        public ISmidgeRequire RequiresJs(JavaScriptFile file)
        {
            _dynamicallyRegisteredWebFiles.Files.Add(file);
            return this;
        }

        public ISmidgeRequire RequiresJs(params string[] paths)
        {
            foreach (var path in paths)
            {
                RequiresJs(new JavaScriptFile(path));
            }
            return this;
        }

        public ISmidgeRequire RequiresCss(CssFile file)
        {
            _dynamicallyRegisteredWebFiles.Files.Add(file);
            return this;
        }

        public ISmidgeRequire RequiresCss(params string[] paths)
        {
            foreach (var path in paths)
            {
                RequiresCss(new CssFile(path));
            }
            return this;
        }

        /// <summary>
        /// Creates a new bundle and returns a bundle context to add files to it
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        /// <remarks>
        /// The bundle is write once - so if it already exists, a noop context is returned that does nothing
        /// </remarks>
        public ISmidgeRequire CreateJsBundle(string bundleName)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentNullException(nameof(bundleName));

            if (_bundleManager.Exists(bundleName)) return new NoopBundleContext();

            return new SmidgeBundleContext(bundleName, _bundleManager, WebFileType.Js, _requestHelper);
        }

        /// <summary>
        /// Creates a new bundle and returns a bundle context to add files to it
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        /// <remarks>
        /// The bundle is write once - so if it already exists, a noop context is returned that does nothing
        /// </remarks>
        public ISmidgeRequire CreateCssBundle(string bundleName)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentNullException(nameof(bundleName));

            if (_bundleManager.Exists(bundleName)) return new NoopBundleContext();

            return new SmidgeBundleContext(bundleName, _bundleManager, WebFileType.Css, _requestHelper);
        }
    }
}