using System;

namespace Smidge.Files
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