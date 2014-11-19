using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Rendering;
using Singularity.CompositeFiles;
using Singularity.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;

namespace Singularity
{
    public class SingularityContext
    {
        public SingularityContext(IUrlCreator urlCreator, IFileMapProvider fileMap)
        {
            UrlCreator = urlCreator;
            FileMap = fileMap;
            Files = new HashSet<IDependentFile>();
        }

        internal HashSet<IDependentFile> Files { get; private set; }

        public IEnumerable<IDependentFile> JavaScriptFiles
        {
            get
            {
                return Files.Where(x => x.DependencyType == IDependentFileType.Javascript);
            }
        }

        public IFileMapProvider FileMap { get; private set; }
        public IUrlCreator UrlCreator { get; private set; }
    }

    public class SingularityHelper
    {
        private SingularityContext _context;
        private SingularityConfig _config;
        private FileMinifyManager _fileManager;

        public SingularityHelper(SingularityContext context, SingularityConfig config, FileMinifyManager fileManager)
        {
            _fileManager = fileManager;
            _context = context;
            _config = config;
        }

        public HtmlString RenderJsHere()
        {
            var result = new StringBuilder();

            if (_config.Get<bool>("debug"))
            {
                foreach (var d in _context.JavaScriptFiles)
                {
                    result.AppendFormat("<script src='{0}' type='text/javascript'></script>", d.FilePath);
                }
                return new HtmlString(result.ToString());
            }
            else
            {
                var urls = _context.UrlCreator.GetUrls(IDependentFileType.Javascript, _context.JavaScriptFiles);
                foreach (var u in urls)
                {
                    result.AppendFormat("<script src='{0}' type='text/javascript'></script>", u);
                }                
            }

            return new HtmlString(result.ToString());
        }

        public async Task RequiresJsAsync(JavaScriptFile file)
        {
            //TODO: This probably isn't so good because if there's already a composite file stored and cached, we shouldn't even have to check 
            // for files here. That could all be done at once in RenderJsHere if we could do that async!

            await _fileManager.MinifyAndCacheFile(file);
            RequiresJs(file);
        }

        public async Task RequiresJsAsync(string path)
        {
            await RequiresJsAsync(new JavaScriptFile(path));
        }

        public SingularityHelper RequiresJs(JavaScriptFile file)
        {
            _context.Files.Add(file);
            return this;
        }

        public SingularityHelper RequiresJs(string path)
        {
            RequiresJs(new JavaScriptFile(path));
            return this;
        }

        public SingularityHelper RequiresCss(CssFile file)
        {
            _context.Files.Add(file);
            return this;
        }

        public SingularityHelper RequiresCss(string path)
        {
            RequiresCss(new CssFile(path));
            return this;
        }
    }
}