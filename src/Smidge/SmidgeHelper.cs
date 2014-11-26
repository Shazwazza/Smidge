using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Rendering;
using Smidge.CompositeFiles;
using Smidge.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace Smidge
{
    
    /// <summary>
    /// Used in views to register and render dependencies
    /// </summary>
    public class SmidgeHelper
    {
        private SmidgeContext _context;
        private ISmidgeConfig _config;
        private FileMinifyManager _fileManager;
        private FileSystemHelper _fileSystemHelper;
        private IContextAccessor<HttpContext> _http;
        private IHasher _hasher;
        private BundleManager _bundleManager;



        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="config"></param>
        /// <param name="fileManager"></param>
        /// <param name="fileSystemHelper"></param>
        /// <param name="http"></param>
        public SmidgeHelper(
            SmidgeContext context,
            ISmidgeConfig config, 
            FileMinifyManager fileManager, 
            FileSystemHelper fileSystemHelper, 
            IHasher hasher, 
            BundleManager bundleManager,
            IContextAccessor<HttpContext> http)
        {
            _bundleManager = bundleManager;
            _hasher = hasher;
            _fileManager = fileManager;
            _context = context;
            _config = config;
            _fileSystemHelper = fileSystemHelper;
            _http = http;
        }

        public async Task<HtmlString> JsHereAsync(string bundleName)
        {
            var result = new StringBuilder();
            var bundleExists = _bundleManager.Exists(bundleName);
            if (!bundleExists) return null;

            if (_config.IsDebug)
            {
                var urls = new List<string>();
                var files = _bundleManager.GetFiles(bundleName);
                foreach (var d in files)
                {
                    urls.Add(d.FilePath);
                }

                foreach (var url in urls)
                {
                    result.AppendFormat("<script src='{0}' type='text/javascript'></script>", url);
                }
                return new HtmlString(result.ToString());
            }
            else
            {
                var compression = _http.Value.GetClientCompression();
                var url = _context.UrlCreator.GetUrl(bundleName, ".js");

                //now we need to determine if these files have already been minified
                var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, bundleName);
                if (!File.Exists(compositeFilePath))
                {
                    var files = _bundleManager.GetFiles(bundleName);
                    //we need to do the minify on the original files
                    foreach (var file in files)
                    {
                        await _fileManager.MinifyAndCacheFileAsync(file);
                    }
                }
                result.AppendFormat("<script src='{0}' type='text/javascript'></script>", url);
                return new HtmlString(result.ToString());
            }            
        }

        /// <summary>
        /// Renders the JS tags
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// TODO: Once the tags are rendered the collection on the context is cleared. Therefore if this method is called multiple times it will 
        /// render anything that has been registered as 'pending' but has not been rendered.
        /// </remarks>
        public async Task<HtmlString> JsHereAsync()
        {
            var result = new StringBuilder();
            var urls = await GenerateJsUrlsAsync();
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
        public async Task<HtmlString> CssHereAsync()
        {
            var result = new StringBuilder();
            var urls = await GenerateCssUrlsAsync();
            foreach (var url in urls)
            {
                result.AppendFormat("<link href='{0}' rel='stylesheet' type='text/css'/>", url);
            }
            return new HtmlString(result.ToString());
        }

        /// <summary>
        /// Generates the list of URLs to render based on what is registered
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GenerateJsUrlsAsync()
        {
            return await GenerateUrlsAsync(_context.JavaScriptFiles, ".js", s => new JavaScriptFile(s + ".js"));
        }

        /// <summary>
        /// Generates the list of URLs to render based on what is registered
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GenerateCssUrlsAsync()
        {
            return await GenerateUrlsAsync(_context.CssFiles, ".css", s => new CssFile(s + ".css"));
        }

        private async Task<IEnumerable<string>> GenerateUrlsAsync(
            IEnumerable<IWebFile> files, 
            string fileExtension, 
            Func<string, IWebFile> fileCreator)
        {
            var result = new List<string>();

            if (_config.IsDebug)
            {
                foreach (var d in files)
                {
                    result.Add(d.FilePath);
                }
            }
            else
            {
                var compression = _http.Value.GetClientCompression();

                //we need to get a collection of files that have their cached/hashed paths, this is used 
                // to check if the composite file has already been created, if it is then we don't need to worry 
                // about anything. If it is not, then we need to minify each of the files now. Then when the request
                // is made to get the composite file, that process is already complete and the composite handler just
                // needs to combine, compress and store the file
                //TODO: There's surely a nicer way to achieve this
                var cachedFiles = files.Select(x =>
                {
                    var file = fileCreator(_hasher.Hash(x.FilePath));
                    file.Minify = x.Minify;
                    //file.PathNameAlias = x.PathNameAlias;
                    return file;
                });

                var urls = _context.UrlCreator.GetUrls(cachedFiles, fileExtension);

                foreach (var u in urls)
                {
                    //now we need to determine if these files have already been minified
                    var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, u.Key);
                    if (!File.Exists(compositeFilePath))
                    {
                        //we need to do the minify on the original files
                        foreach (var file in files)
                        {
                            await _fileManager.MinifyAndCacheFileAsync(file);
                        }
                    }

                    result.Add(u.Url);
                }
            }

            return result;

        }

        public SmidgeHelper RequiresJs(JavaScriptFile file)
        {
            _context.Files.Add(file);
            return this;
        }

        public SmidgeHelper RequiresJs(params string[] paths)
        {
            foreach (var path in paths)
            {
                RequiresJs(new JavaScriptFile(path));
            }            
            return this;
        }

        public SmidgeHelper RequiresCss(CssFile file)
        {
            _context.Files.Add(file);
            return this;
        }

        public SmidgeHelper RequiresCss(params string[] paths)
        {
            foreach (var path in paths)
            {
                RequiresCss(new CssFile(path));
            }            
            return this;
        }
    }
}