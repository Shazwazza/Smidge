using Smidge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using Smidge.Hashing;

namespace Smidge
{
    /// <summary>
    /// Used in views to register and render dependencies
    /// </summary>
    public class SmidgeHelper : ISmidgeRequire
    {
        private readonly DynamicallyRegisteredWebFiles _dynamicallyRegisteredWebFiles;
        private readonly IPreProcessManager _preProcessManager;
        private readonly ISmidgeFileSystem _fileSystem;
        private readonly IBundleManager _bundleManager;
        private readonly FileBatcher _fileBatcher;
        private readonly IBundleFileSetGenerator _fileSetGenerator;
        private readonly PreProcessPipelineFactory _processorFactory;
        private readonly IUrlManager _urlManager;
        private readonly IRequestHelper _requestHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CacheBusterResolver _cacheBusterResolver;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileSetGenerator"></param>
        /// <param name="dynamicallyRegisteredWebFiles"></param>
        /// <param name="preProcessManager"></param>
        /// <param name="fileSystem"></param>
        /// <param name="hasher"></param>
        /// <param name="bundleManager"></param>
        /// <param name="processorFactory"></param>
        /// <param name="urlManager"></param>
        /// <param name="requestHelper"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="cacheBusterResolver"></param>
        public SmidgeHelper(
            IBundleFileSetGenerator fileSetGenerator,
            DynamicallyRegisteredWebFiles dynamicallyRegisteredWebFiles,
            IPreProcessManager preProcessManager,
            ISmidgeFileSystem fileSystem,
            IHasher hasher,
            IBundleManager bundleManager,
            PreProcessPipelineFactory processorFactory,
            IUrlManager urlManager,
            IRequestHelper requestHelper,
            IHttpContextAccessor httpContextAccessor,
            CacheBusterResolver cacheBusterResolver)
        {
            _fileSetGenerator = fileSetGenerator ?? throw new ArgumentNullException(nameof(fileSetGenerator));
            _processorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
            _urlManager = urlManager ?? throw new ArgumentNullException(nameof(urlManager));
            _requestHelper = requestHelper ?? throw new ArgumentNullException(nameof(requestHelper));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _cacheBusterResolver = cacheBusterResolver ?? throw new ArgumentNullException(nameof(cacheBusterResolver));
            _bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            _preProcessManager = preProcessManager ?? throw new ArgumentNullException(nameof(preProcessManager));
            _dynamicallyRegisteredWebFiles = dynamicallyRegisteredWebFiles ?? throw new ArgumentNullException(nameof(dynamicallyRegisteredWebFiles));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _fileBatcher = new FileBatcher(_fileSystem, _requestHelper, hasher);
        }

        public async Task<HtmlString> JsHereAsync(string bundleName, bool debug = false)
        {
            var urls = await GenerateJsUrlsAsync(bundleName, debug);
            var result = new StringBuilder();

            foreach (var url in urls)
            {
                result.AppendFormat("<script src=\"{0}\"></script>", url);
            }
            return new HtmlString(result.ToString());
        }

        public async Task<HtmlString> CssHereAsync(string bundleName, bool debug = false)
        {
            var urls = await GenerateCssUrlsAsync(bundleName, debug);
            var result = new StringBuilder();

            foreach (var url in urls)
            {
                result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\">", url);
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
                result.AppendFormat("<script src=\"{0}\"></script>", url);
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
                result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\">", url);
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

        public Task<IEnumerable<string>> GenerateJsUrlsAsync(string bundleName, bool debug = false)
        {
            return Task.FromResult(GenerateBundleUrlsAsync(bundleName, ".js", debug));
        }

        /// <summary>
        /// Generates the list of URLs to render based on what is dynamically registered
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GenerateCssUrlsAsync(PreProcessPipeline pipeline = null, bool debug = false)
        {
            return await GenerateUrlsAsync(_dynamicallyRegisteredWebFiles.CssFiles, WebFileType.Css, pipeline, debug);
        }

        public Task<IEnumerable<string>> GenerateCssUrlsAsync(string bundleName, bool debug = false)
        {
            return Task.FromResult(GenerateBundleUrlsAsync(bundleName, ".css", debug));
        }

        /// <summary>
        /// Generates the URLs for a given bundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="fileExt"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        private IEnumerable<string> GenerateBundleUrlsAsync(string bundleName, string fileExt, bool debug)
        {
            //TODO: We should cache this, but problem is how do we do that with file watchers enabled? We'd still have to lookup the bundleOptions
            // or maybe we just cache when file watchers are not enabled - probably the way to do it

            var bundle = _bundleManager.GetBundle(bundleName) ?? throw new BundleNotFoundException(bundleName);

            if (bundle.Files.Count == 0)
            {
                return Enumerable.Empty<string>();
            }

            var result = new List<string>();

            //get the bundle options from the bundle if they have been set otherwise with the defaults
            var bundleOptions = bundle.GetBundleOptions(_bundleManager, debug);

            var cacheBuster = _cacheBusterResolver.GetCacheBuster(bundleOptions.GetCacheBusterType());
            var cacheBusterValue = cacheBuster.GetValue();

            //if not processing as composite files, then just use their native file paths
            if (!bundleOptions.ProcessAsCompositeFile)
            {
                var files = _fileSetGenerator.GetOrderedFileSet(bundle,
                    _processorFactory.CreateDefault(
                        //the file type in the bundle will always be the same
                        bundle.Files[0].DependencyType));
                result.AddRange(files.Select(d => _urlManager.AppendCacheBuster(_requestHelper.Content(d), debug, cacheBusterValue)));
                return result;
            }

            var url = _urlManager.GetUrl(bundleName, fileExt, debug, cacheBusterValue);
            if (!string.IsNullOrWhiteSpace(url))
            {
                result.Add(url);
            }

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

            var orderedFiles = _fileSetGenerator.GetOrderedFileSet(files, pipeline ?? _processorFactory.CreateDefault(fileType));

            var cacheBuster = _cacheBusterResolver.GetCacheBuster(_bundleManager.GetDefaultBundleOptions(debug).GetCacheBusterType());
            var cacheBusterValue = cacheBuster.GetValue();

            if (debug)
            {
                return orderedFiles.Select(x => _urlManager.AppendCacheBuster(_requestHelper.Content(x), debug, cacheBusterValue));
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
                    var compositeUrls = _urlManager.GetUrls(
                        batch.Select(x => x.Hashed),
                        fileType == WebFileType.Css ? ".css" : ".js",
                        cacheBusterValue);

                    foreach (var u in compositeUrls)
                    {
                        //now we need to determine if these files have already been minified

                        var defaultBundleOptions = _bundleManager.GetDefaultBundleOptions(false);

                        var cacheFile = _fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, compression, u.Key, out _);
                        if (!cacheFile.Exists)
                        {
                            using (var bundleContext = BundleContext.CreateEmpty(cacheBusterValue))
                            {
                                //need to process/minify these files - need to use their original paths of course
                                foreach (var file in batch.Select(x => x.Original))
                                {
                                    await _preProcessManager.ProcessAndCacheFileAsync(file, null, bundleContext);
                                }
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

            if (_bundleManager.Exists(bundleName)) return new NoopSmidgeRequire();

            return new SmidgeRequire(bundleName, _bundleManager, WebFileType.Js, _requestHelper);
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

            if (_bundleManager.Exists(bundleName)) return new NoopSmidgeRequire();

            return new SmidgeRequire(bundleName, _bundleManager, WebFileType.Css, _requestHelper);
        }
    }
}
