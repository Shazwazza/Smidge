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
        ICacheFileSystem CacheFileSystem { get; }
        IFileProvider SourceFileProvider { get; }

        //TODO: I don't think we need to even pass in the IFileProvider, the file system will already know about it
        //IFileInfo GetCachedCompositeFile(IFileProvider cacheFileProvider, ICacheBuster cacheBuster, CompressionType type, string filesetKey);
        //IFileInfo GetCacheFile(IFileProvider cacheFileProvider, IWebFile file, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster, out Lazy<IFileInfo> fileInfo);

        //string GetFileHash(IWebFile file, IFileInfo fileInfo, string extension);
        //string GetFileHash(IWebFile file, string extension);
        //IFileInfo GetFileInfo(IWebFile webfile);
        //IFileInfo GetFileInfo(string filePath);

        IEnumerable<string> GetPathsForFilesInFolder(string folderPath);
        bool IsFolder(string path);
        Task<string> ReadContentsAsync(IFileInfo fileInfo);
        string ReverseMapPath(string subPath, IFileInfo fileInfo);
        bool Watch(IWebFile webFile, IFileInfo fileInfo, BundleOptions bundleOptions, Action<WatchedFile> fileModifiedCallback);
    }
}