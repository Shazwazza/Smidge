using System;

namespace Smidge.Models
{

    public interface IWebFile
    {
        string FilePath { get; set; }
        WebFileType DependencyType { get; }

        //int Priority { get; set; }
        //int Group { get; set; }
        //string PathNameAlias { get; set; }
        //string ForceProvider { get; set; }
        //bool ForceBundle { get; set; }

        //TODO: Instead of just having this flag, each file can have a list of 
        // pipeline elements to execute (i.e. minify, uglify, etc...)
        bool Minify { get; set; }
    }
}