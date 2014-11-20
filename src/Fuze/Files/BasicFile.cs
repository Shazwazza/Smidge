using System;

namespace Fuze.Files
{
    public abstract class BasicFile : IWebFile
    {
        public BasicFile()
        {
            //defaults
            Minify = true;
        }

        public abstract WebFileType DependencyType { get; }

        public string FilePath { get; set; }

        public string PathNameAlias { get; set; }

        public bool Minify { get; set; }
    }
}