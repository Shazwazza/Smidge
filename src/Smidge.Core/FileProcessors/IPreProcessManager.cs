using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Smidge.CompositeFiles;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.FileProcessors
{
    public interface IPreProcessManager
    {
        /// <summary>
        /// This will first check if the file is in cache and if not it will run all pre-processors assigned to the file and store the output in a persisted file cache.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bundleOptions"></param>
        /// <param name="bundleContext"></param>
        /// <returns></returns>
        Task ProcessAndCacheFileAsync(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext);
    }
}
