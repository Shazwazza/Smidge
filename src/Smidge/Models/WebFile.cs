using System;
using Smidge.FileProcessors;

namespace Smidge.Models
{
    public class WebFile : IWebFile
    {
        public WebFile()
        {
        }

        public WebFileType DependencyType { get; set; }

        public string FilePath { get; set; }

        public PreProcessPipeline Pipeline { get; set; }

        //public string PathNameAlias { get; set; }

        //public bool Minify { get; set; }
    }
}