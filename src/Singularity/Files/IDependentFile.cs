using System;

namespace Singularity.Files
{

    public enum IDependentFileType
    {
        Javascript, Css
    }

    public class JavaScriptFile : BasicFile
    {
        public JavaScriptFile()
        {

        }
        public JavaScriptFile(string path)
        {
            FilePath = path;
        }
        public override IDependentFileType DependencyType
        {
            get{ return IDependentFileType.Javascript; }
        }
    }

    public class CssFile : BasicFile
    {
        public CssFile()
        {

        }
        public CssFile(string path)
        {
            FilePath = path;
        }

        public override IDependentFileType DependencyType
        {
            get { return IDependentFileType.Css; }
        }
    }

    public abstract class BasicFile : IDependentFile
    {
        public BasicFile()
        {
            //defaults
            Minify = true;
        }

        public abstract IDependentFileType DependencyType { get; }

        public string FilePath { get; set; }

        public string PathNameAlias { get; set; }

        public bool Minify { get; set; }
    }

    public interface IDependentFile
    {
        string FilePath { get; set; }
        IDependentFileType DependencyType { get; }
        //int Priority { get; set; }
        //int Group { get; set; }
        string PathNameAlias { get; set; }
        //string ForceProvider { get; set; }
        //bool ForceBundle { get; set; }

        bool Minify { get; set; }
    }
}