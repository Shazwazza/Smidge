using System;
using Smidge.FileProcessors;

namespace Smidge.Models
{
    public class WebFile : IWebFile
    {
        public WebFile()
        {
            //defaults
            Order = 0;
        }

        public WebFileType DependencyType { get; set; }

        /// <summary>
        /// The order that this dependency should be rendered
        /// </summary>
        public int Order { get; set; }

        public string FilePath { get; set; }

        public PreProcessPipeline Pipeline { get; set; }
    }
}