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
        private IHostingEnvironment _env;
        private FileCacheManager _fileManager;

        public SingularityHelper(SingularityContext context, SingularityConfig config, IHostingEnvironment env, FileCacheManager fileManager)
        {
            _fileManager = fileManager;
            _env = env;
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

        public async Task<SingularityHelper> RequiresJsAsync(JavaScriptFile file)
        {
            await _fileManager.SetFilePathAsync(file);

            RequiresJs(file);

            return this;
        }

        public async Task<SingularityHelper> RequiresJsAsync(string path)
        {
            return await RequiresJsAsync(new JavaScriptFile(path));
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