using Singularity.CompositeFiles;
using Singularity.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Singularity
{
    public class SingularityContext
    {
        public SingularityContext(IUrlCreator urlCreator, IFileMapProvider fileMap)
        {
            UrlCreator = urlCreator;
            FileMap = fileMap;
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

        public IFileMapProvider FileMap { get; private set; }
        public IUrlCreator UrlCreator { get; private set; }
    }
}