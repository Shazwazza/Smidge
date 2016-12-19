using Smidge.Models;
using System;

namespace Smidge.FileProcessors
{
    
    public class FileProcessContext
    {
        public FileProcessContext(string fileContent, IWebFile webFile)
        {
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));
            if (webFile == null) throw new ArgumentNullException(nameof(webFile));

            FileContent = fileContent;
            WebFile = webFile;
        }
        
        public string FileContent { get; private set; }

        public IWebFile WebFile { get; private set; }
    }
}