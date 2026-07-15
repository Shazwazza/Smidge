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
        /// Returns virtual paths for all files matching the pattern.
        /// </summary>
        /// <param name="filePattern"></param>
        /// <returns></returns>
        /// <remarks>
        /// When the pattern resolves to a directory, all files within it are matched regardless of extension.
        /// </remarks>
        IEnumerable<string> GetMatchingFiles(string filePattern);

        /// <summary>
        /// Returns virtual paths for all files matching the pattern, constrained to the given <see cref="WebFileType"/>.
        /// </summary>
        /// <param name="filePattern"></param>
        /// <param name="fileType"></param>
        /// <returns></returns>
        /// <remarks>
        /// When the pattern resolves to a directory, only files whose extension matches the
        /// <paramref name="fileType"/> (<c>.js</c> or <c>.css</c>) are matched. This prevents generated
        /// artifacts (e.g. <c>.gz</c> or <c>.map</c> files) or files of the wrong type from being pulled
        /// into a bundle and handed to the wrong pre-processor.
        /// </remarks>
        IEnumerable<string> GetMatchingFiles(string filePattern, WebFileType fileType);
        
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
