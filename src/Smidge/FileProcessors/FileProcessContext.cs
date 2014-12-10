using Smidge.Models;
using System;

namespace Smidge.FileProcessors
{
    public class FileProcessContext
    {
        public FileProcessContext(string fileContent, IWebFile webFile)
        {
            FileContent = fileContent;
            WebFile = webFile;
        }

        public string FileContent { get; set; }

        public IWebFile WebFile { get; set; }
    }
}