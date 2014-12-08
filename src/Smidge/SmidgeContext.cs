using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smidge
{
    public class SmidgeContext
    {
        public SmidgeContext(IUrlManager urlCreator)
        {
            UrlCreator = urlCreator;
            Files = new HashSet<IWebFile>();
        }

        internal HashSet<IWebFile> Files { get; private set; }

        public IEnumerable<IWebFile> JavaScriptFiles
        {
            get
            {
                return Files.Where(x => x.DependencyType == WebFileType.Js);
            }
        }

        public IEnumerable<IWebFile> CssFiles
        {
            get
            {
                return Files.Where(x => x.DependencyType == WebFileType.Css);
            }
        }

        public IUrlManager UrlCreator { get; private set; }
    }
}