using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Rendering;
using Fuze.CompositeFiles;
using Fuze.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace Fuze
{
    
    /// <summary>
    /// Used in views to register and render dependencies
    /// </summary>
    public class FuzeHelper
    {
        private FuzeContext _context;
        private FuzeConfig _config;
        private FileMinifyManager _fileManager;
        private FileSystemHelper _fileSystemHelper;
        private IContextAccessor<HttpContext> _http;

        public FuzeHelper(FuzeContext context, FuzeConfig config, FileMinifyManager fileManager, FileSystemHelper fileSystemHelper, IContextAccessor<HttpContext> http)
        {
            _fileManager = fileManager;
            _context = context;
            _config = config;
            _fileSystemHelper = fileSystemHelper;
            _http = http;
        }

        /// <summary>
        /// Renders the JS tags
        /// </summary>
        /// <returns></returns>
        public async Task<HtmlString> RenderJsHereAsync()
        {
            var result = new StringBuilder();

            if (_config.IsDebug)
            {
                foreach (var d in _context.JavaScriptFiles)
                {
                    result.AppendFormat("<script src='{0}' type='text/javascript'></script>", d.FilePath);
                }
                return new HtmlString(result.ToString());
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
                var cachedFiles = _context.JavaScriptFiles.Select(x => new JavaScriptFile(x.FilePath.GenerateHash() + ".js")
                {
                    Minify = x.Minify,
                    PathNameAlias = x.PathNameAlias
                });

                var urls = _context.UrlCreator.GetUrls(WebFileType.Javascript, cachedFiles);

                foreach (var u in urls)
                {
                    //now we need to determine if these files have already been minified
                    var compositeFilePath = _fileSystemHelper.GetCurrentCompositeFilePath(compression, u.Key);
                    if (!File.Exists(compositeFilePath))
                    {
                        //we need to do the minify on the original files
                        foreach(var file in _context.JavaScriptFiles)
                        {
                            await _fileManager.MinifyAndCacheFileAsync(file);
                        }
                    }

                    result.AppendFormat("<script src='{0}' type='text/javascript'></script>", u.Url);
                }                
            }

            return new HtmlString(result.ToString());
        }

        public FuzeHelper RequiresJs(JavaScriptFile file)
        {
            _context.Files.Add(file);
            return this;
        }

        public FuzeHelper RequiresJs(string path)
        {
            RequiresJs(new JavaScriptFile(path));
            return this;
        }

        public FuzeHelper RequiresCss(CssFile file)
        {
            _context.Files.Add(file);
            return this;
        }

        public FuzeHelper RequiresCss(string path)
        {
            RequiresCss(new CssFile(path));
            return this;
        }
    }
}