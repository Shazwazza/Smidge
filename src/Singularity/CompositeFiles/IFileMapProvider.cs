using Microsoft.AspNet.Http;
using Singularity.Files;
using System;
using System.Collections.Generic;

namespace Singularity.CompositeFiles
{
    public interface IFileMapProvider
    {

        /// <summary>
        /// Retreives the file map for the key/version/compression type specified
        /// </summary>
        /// <param name="fileKey"></param>
        /// <param name="version"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        CompositeFileMap GetCompositeFile(string fileKey,
            int version,
            string compression);

        /// <summary>
        /// Retreives the dependent file paths for the filekey/version (regardless of compression)
        /// </summary>
        /// <param name="fileKey"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        IEnumerable<string> GetDependentFiles(string fileKey, int version);

        /// <summary>
        /// Creates a map for the version/compression type/dependent file listing
        /// </summary>
        /// <param name="fileKey"></param>
        /// <param name="compressionType"></param>
        /// <param name="dependentFiles"></param>
        /// <param name="compositeFile"></param>
        /// <param name="version"></param>
        void CreateUpdateMap(string fileKey,
            string compressionType,
            IEnumerable<IDependentFile> dependentFiles,
            string compositeFile,
            int version);

        /// <summary>
        /// Creates a new file map and file key for the dependent file list, this is used to create URLs with CompositeUrlType.MappedId 
        /// </summary>       
        string CreateNewMap(HttpContext http,
                                   IEnumerable<IDependentFile> dependentFiles,
                                   int version);

        
    }
}