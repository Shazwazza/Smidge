using System;

namespace Smidge.FileProcessors
{
    public class FileProcessContext
    {
        public FileProcessContext(string fileContent, string originalRequestPath)
        {
            FileContent = fileContent;
            OriginalRequestPath = originalRequestPath;
        }

        public string FileContent { get; set; }

        public string OriginalRequestPath { get; set; }
    }
}