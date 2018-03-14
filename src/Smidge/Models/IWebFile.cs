using Smidge.FileProcessors;
using System;
using System.IO;

namespace Smidge.Models
{
    public interface IWebFile
    {
        string FilePath { get; set; }
        /// <summary>
        /// If the file has different of physical path and request path
        /// Look at https://github.com/Shazwazza/Smidge/issues/74
        /// </summary>
        string RequestPath { get; set; }
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