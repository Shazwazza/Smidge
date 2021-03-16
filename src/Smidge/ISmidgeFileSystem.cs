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
        /// Throws an exception if the file doesn't exist
        /// </remarks>
        IFileInfo GetRequiredFileInfo(IWebFile webfile);

        /// <summary>
        /// Get a required <see cref="IFileInfo"/>
        /// </summary>
        /// <param name="webfile"></param>
        /// <returns></returns>
        /// <remarks>
        /// Throws an exception if the file doesn't exist
        /// </remarks>
        IFileInfo GetRequiredFileInfo(string filePath);

        /// <summary>
        /// Returns all full paths for files in the folder
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        // TODO: This should use Globbing patterns
        IEnumerable<string> GetPathsForFilesInFolder(string folderPath);
        
        // TODO: This won't be needed when we implement Globbing patterns
        bool IsFolder(string path);

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