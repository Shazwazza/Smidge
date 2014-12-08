using System;

namespace Smidge.Models
{
    public class CssFile : WebFile
    {
        public CssFile()
        {
            DependencyType = WebFileType.Css;
        }
        public CssFile(string path) : this()
        {
            FilePath = path;
        }

    }
}