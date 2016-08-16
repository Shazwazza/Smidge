using System.Collections.Generic;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge
{
    /// <summary>
    /// Returns the ordered file set and ensures that all pre-processor pipelines are applied correctly
    /// </summary>
    public interface IBundleFileSetGenerator
    {
        /// <summary>
        /// Returns the ordered file set for a bundle and ensures that all pre-processor pipelines are applied correctly
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        IEnumerable<IWebFile> GetOrderedFileSet(Bundle bundle, PreProcessPipeline pipeline);

        /// <summary>
        /// Returns the ordered file set for dynamically registered assets and ensures that all pre-processor pipelines are applied correctly
        /// </summary>
        /// <param name="files"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        IEnumerable<IWebFile> GetOrderedFileSet(IEnumerable<IWebFile> files, PreProcessPipeline pipeline);
    }
}