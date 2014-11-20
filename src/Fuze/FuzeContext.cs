using Fuze.CompositeFiles;
using Fuze.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fuze
{
    public class FuzeContext
    {
        public FuzeContext(IUrlCreator urlCreator)
        {
            UrlCreator = urlCreator;
            Files = new HashSet<IWebFile>();
        }

        internal HashSet<IWebFile> Files { get; private set; }

        public IEnumerable<IWebFile> JavaScriptFiles
        {
            get
            {
                return Files.Where(x => x.DependencyType == WebFileType.Javascript);
            }
        }

        public IUrlCreator UrlCreator { get; private set; }
    }
}