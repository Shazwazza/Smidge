using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Smidge.Cache;
using Smidge.Models;
using Smidge.Options;

namespace Smidge
{
    public interface ISmidgeFileSystem
    {
        /// <summary>
        /// Get the <see cref="ICacheFileSystem"/>
        /// </summary>
        ICacheFileSystem CacheFileSystem { get; }

        /// <summary>
        /// Get a required <see cref="IFileInfo"/>
        /// </summary>
        /// <param name="webfile"></param>
        /// <returns></returns>
        /// <remarks>
        /// Returns even if the file doesn't exist
        /// </remarks>
        IFileInfo GetRequiredFileInfo(IWebFile webfile);

        /// <summary>
        /// Get a required <see cref="IFileInfo"/>
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <remarks>
        /// Returns even if the file doesn't exist
        /// </remarks>
        IFileInfo GetRequiredFileInfo(string filePath);

        /// <summary>
        /// Returns virtual paths for all files matching the pattern.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        IEnumerable<string> GetMatchingFiles(string filePattern);
        
        /// <summary>
        /// Reads the content of a file
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        Task<string> ReadContentsAsync(IFileInfo fileInfo);


        string ReverseMapPath(string subPath, IFileInfo fileInfo);
        bool Watch(IWebFile webFile, IFileInfo fileInfo, BundleOptions bundleOptions, Action<WatchedFile> fileModifiedCallback);
        string ConvertToFileProviderPath(string path);
    }
}
