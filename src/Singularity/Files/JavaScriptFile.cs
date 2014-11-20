using System;

namespace Singularity.Files
{
    public class JavaScriptFile : BasicFile
    {
        public JavaScriptFile()
        {

        }
        public JavaScriptFile(string path)
        {
            FilePath = path;
        }
        public override WebFileType DependencyType
        {
            get { return WebFileType.Javascript; }
        }
    }
}