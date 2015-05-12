using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Rendering;
using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Smidge.FileProcessors;

namespace Smidge
{
    
    /// <summary>
    /// Used in views to register and render dependencies
    /// </summary>
    public class SmidgeHelper
    {
        private SmidgeContext _context;
        private ISmidgeConfig _config;
        private PreProcessManager _fileManager;
        private FileSystemHelper _fileSystemHelper;
        private HttpRequest _request;
        private IHasher _hasher;
        private BundleManager _bundleManager;
        private FileBatcher _fileBatcher;
        private PreProcessPipelineFactory _processorFactory;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="config"></param>
        /// <param name="fileManager"></param>
        /// <param name="fileSystemHelper"></param>
        /// <param name="request"></param>
        public SmidgeHelper(
            SmidgeContext context,
            ISmidgeConfig config, 
            PreProcessManager fileManager, 
            FileSystemHelper fileSystemHelper, 
            IHasher hasher, 
            BundleManager bundleManager,
            IHttpContextAccessor http,
            PreProcessPipelineFactory processorFactory)
        {
            _processorFactory = processorFactory;
            _bundleManager = bundleManager;
            _hasher = hasher;
            _fileManager = fileManager;
            _context = context;
            _config = config;
            _fileSystemHelper = fileSystemHelper;
            _request = http.HttpContext.Request;

            _fileBatcher = new FileBatcher(_fileSystemHelper, _request, _hasher);
        }

        public async Task<HtmlString> JsHereAsync(string bundleName)
        {
            var result = new StringBuilder();
            var bundleExists = _bundleManager.Exists(bundleName);
            if (!bundleExists) return null;

            if (_config.IsDebug)
            {
                var urls = new List<string>();
                var files = _bundleManager.GetFiles(bundleName, _request);
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
                var compression = _request.GetClientCompression();
                var url = _context.UrlCreator.GetUrl(bundleName, ".js");

                //now we need to determine if these files have already been minified
                var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, bundleName);
                if (!File.Exists(compositeFilePath))
                {
                    var files = _bundleManager.GetFiles(bundleName, _request);
                    //we need to do the minify on the original files
                    foreach (var file in files)
                    {
                        await _fileManager.ProcessAndCacheFileAsync(file);
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
        public async Task<HtmlString> JsHereAsync(PreProcessPipeline pipeline = null)
        {
            var result = new StringBuilder();
            var urls = await GenerateJsUrlsAsync(pipeline);
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
        public async Task<HtmlString> CssHereAsync(PreProcessPipeline pipeline = null)
        {
            var result = new StringBuilder();
            var urls = await GenerateCssUrlsAsync(pipeline);
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
        public async Task<IEnumerable<string>> GenerateJsUrlsAsync(PreProcessPipeline pipeline = null)
        {
            return await GenerateUrlsAsync(_context.JavaScriptFiles, WebFileType.Js, pipeline);
        }

        /// <summary>
        /// Generates the list of URLs to render based on what is registered
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GenerateCssUrlsAsync(PreProcessPipeline pipeline = null)
        {
            return await GenerateUrlsAsync(_context.CssFiles, WebFileType.Css, pipeline);
        }

        private async Task<IEnumerable<string>> GenerateUrlsAsync(
            IEnumerable<IWebFile> files, 
            WebFileType fileType,
            PreProcessPipeline pipeline = null)
        {
            var result = new List<string>();

            if (_config.IsDebug)
            {
                return GenerateUrlsDebug(files);
            }
            else
            {

                if (pipeline == null)
                {
                    pipeline = _processorFactory.GetDefault(fileType);
                }

                var compression = _request.GetClientCompression();

                //Get the file collection used to create the composite URLs and the external requests
                var fileBatches = _fileBatcher.GetCompositeFileCollectionForUrlGeneration(files);

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
                        var compositeUrls = _context.UrlCreator.GetUrls(batch.Select(x => x.Hashed), fileType == WebFileType.Css ? ".css" : ".js");

                        foreach (var u in compositeUrls)
                        {
                            //now we need to determine if these files have already been minified
                            var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, u.Key);
                            if (!File.Exists(compositeFilePath))
                            {
                                //need to process/minify these files - need to use their original paths of course
                                await ProcessWebFilesAsync(batch.Select(x => x.Original), pipeline);
                            }

                            result.Add(u.Url);
                        }
                    }
                    
                }

                
            }

            return result;

        }
        

        /// <summary>
        /// Minifies (and performs any other operation defined in the pipeline) for each file
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private async Task ProcessWebFilesAsync(IEnumerable<IWebFile> files, PreProcessPipeline pipeline)
        {   
            //we need to do the minify on the original files
            foreach (var file in files)
            {   
                //if the pipeline on the file is null, assign the default one passed in
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }

                //We need to check if this path is a folder, then iterate the files
                if (_fileSystemHelper.IsFolder(file.FilePath))
                {
                    var filePaths = _fileSystemHelper.GetPathsForFilesInFolder(file.FilePath);
                    foreach (var f in filePaths)
                    {
                        await _fileManager.ProcessAndCacheFileAsync(new WebFile
                        {
                            FilePath = _fileSystemHelper.NormalizeWebPath(f, _request),
                            DependencyType = file.DependencyType,
                            Pipeline = file.Pipeline
                        });
                    }
                }
                else
                {
                    await _fileManager.ProcessAndCacheFileAsync(file);
                }
            }
        }

        private IEnumerable<string> GenerateUrlsDebug(IEnumerable<IWebFile> files)
        {
            var result = new List<string>();
            foreach (var file in files)
            {
                file.FilePath = _fileSystemHelper.NormalizeWebPath(file.FilePath, _request);

                //We need to check if this path is a folder, then iterate the files
                if (_fileSystemHelper.IsFolder(file.FilePath))
                {
                    var filePaths = _fileSystemHelper.GetPathsForFilesInFolder(file.FilePath);
                    foreach (var f in filePaths)
                    {
                        result.Add(_fileSystemHelper.NormalizeWebPath(f, _request));
                    }
                }
                else
                {
                    result.Add(file.FilePath);
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