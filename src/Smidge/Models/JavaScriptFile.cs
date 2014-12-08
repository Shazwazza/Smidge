using System;

namespace Smidge.Models
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