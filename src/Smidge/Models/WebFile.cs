using System;

namespace Smidge.Models
{
    public class WebFile : IWebFile
    {
        public WebFile()
        {
            //defaults
            Minify = true;
        }

        public WebFileType DependencyType { get; set; }

        public string FilePath { get; set; }

        //public string PathNameAlias { get; set; }

        public bool Minify { get; set; }
    }
}