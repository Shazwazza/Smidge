using Smidge.CompositeFiles;
using Smidge.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smidge
{
    public class SmidgeContext
    {
        public SmidgeContext(IUrlCreator urlCreator)
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

        public IUrlCreator UrlCreator { get; private set; }
    }
}