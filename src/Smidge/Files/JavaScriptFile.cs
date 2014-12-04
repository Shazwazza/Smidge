using System;

namespace Smidge.Files
{
    public class JavaScriptFile : WebFile
    {
        public JavaScriptFile()
        {
            DependencyType = WebFileType.Js;
        }
        public JavaScriptFile(string path) : this()
        {
            FilePath = path;
        }
    }
}