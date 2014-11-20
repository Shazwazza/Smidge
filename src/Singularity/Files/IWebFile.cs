using System;

namespace Singularity.Files
{

    public interface IWebFile
    {
        string FilePath { get; set; }
        WebFileType DependencyType { get; }
        //int Priority { get; set; }
        //int Group { get; set; }
        string PathNameAlias { get; set; }
        //string ForceProvider { get; set; }
        //bool ForceBundle { get; set; }

        bool Minify { get; set; }
    }
}