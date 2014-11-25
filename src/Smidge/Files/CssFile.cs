using System;

namespace Smidge.Files
{
    public class CssFile : BasicFile
    {
        public CssFile()
        {

        }
        public CssFile(string path)
        {
            FilePath = path;
        }

        public override WebFileType DependencyType
        {
            get { return WebFileType.Css; }
        }
    }
}