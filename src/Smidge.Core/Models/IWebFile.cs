using Smidge.FileProcessors;

namespace Smidge.Models
{
    public interface IWebFile
    {
        /// <summary>
        /// The file path to read from an IFileProvider to be served
        /// </summary>
        string FilePath { get; set; }

        /// <summary>
        /// Optional request path of the file
        /// </summary>
        /// <remarks>
        /// Typically this is null and will be resolved based on the <see cref="IRequestHelper.Content(string)"/> method.
        /// However in situations where custom <see cref="StaticFileOptions"/> are used with a custom <see cref="StaticFileOptions.RequestPath"/>
        /// this property will need to be set in order to know how to resolve the web request.
        /// The value should be the same value used for the <see cref="StaticFileOptions"/> <see cref="StaticFileOptions.RequestPath"/> property.
        /// </remarks>
        string RequestPath { get; set; }

        WebFileType DependencyType { get; }

        /// <summary>
        /// The order that this dependency should be rendered
        /// </summary>
        int Order { get; set; }

        /// <summary>
        /// The pre-processor pipeline that will be used to process this file, if it is null then the default pipeline for this
        /// file type will be applied.
        /// </summary>
        PreProcessPipeline Pipeline { get; set; }

    }
}