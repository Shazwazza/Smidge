using System.IO;
using Microsoft.Extensions.FileProviders;
using Smidge.Hashing;
using Smidge.Models;

namespace Smidge
{
    public static class SmidgeFileSystemExtensions
    {
        

        /// <summary>
        /// Returns a file's hash
        /// </summary>
        /// <param name="file"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GetFileHash(this IHasher hasher, IWebFile file, string extension)
        {
            var hashName = hasher.Hash(file.FilePath) + extension;
            return hashName;
        }

        /// <summary>
        /// Returns a file's hash which includes it's timestamp
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileInfo"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GetFileHash(this IHasher hasher, IWebFile file, IFileInfo fileInfo, string extension)
        {
            var lastWrite = fileInfo.LastModified;
            var hashName = hasher.Hash(file.FilePath + lastWrite) + extension;
            return hashName;
        }
    }
}
