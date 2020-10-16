using Smidge.FileProcessors;

namespace Smidge.Models
{
    public interface IWebFile
    {
        string FilePath { get; set; }
        WebFileType DependencyType { get; }

        /// <summary>
        /// The order that this dependency should be rendered
        /// </summary>
        int Order { get; set; }

        //int Group { get; set; }
        //string PathNameAlias { get; set; }
        //string ForceProvider { get; set; }
        //bool ForceBundle { get; set; }

        /// <summary>
        /// The pre-processor pipeline that will be used to process this file, if it is null then the default pipeline for this
        /// file type will be applied.
        /// </summary>
        PreProcessPipeline Pipeline { get; set; }

    }
}