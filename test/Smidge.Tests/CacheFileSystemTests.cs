using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Smidge.Cache;
using Smidge.Hashing;
using Smidge.InMemory;
using Xunit;

namespace Smidge.Tests
{
    /// <summary>
    /// Tests the throwing vs non-throwing lookup contract on the cache file systems. The non-throwing
    /// <see cref="ICacheFileSystem.GetFileInfo(string)"/> is what allows request handlers to return a graceful
    /// 404 instead of an unhandled 500 for missing/stale/spoofed files (issues #199, #185).
    /// </summary>
    public class CacheFileSystemTests
    {
        [Fact]
        public void MemoryCache_GetFileInfo_Missing_Does_Not_Throw()
        {
            var fs = new MemoryCacheFileSystem(new Crc32Hasher());

            IFileInfo result = fs.GetFileInfo("does/not/exist.js");

            Assert.NotNull(result);
            Assert.False(result.Exists);
        }

        [Fact]
        public void MemoryCache_GetRequiredFileInfo_Missing_Throws()
        {
            var fs = new MemoryCacheFileSystem(new Crc32Hasher());

            Assert.Throws<FileNotFoundException>(() => fs.GetRequiredFileInfo("does/not/exist.js"));
        }

        [Fact]
        public void PhysicalCache_GetFileInfo_Missing_Does_Not_Throw()
        {
            using var temp = new TempFolder();
            var fs = new PhysicalFileCacheFileSystem(new PhysicalFileProvider(temp.Path), new Crc32Hasher());

            IFileInfo result = fs.GetFileInfo("does-not-exist.css");

            Assert.NotNull(result);
            Assert.False(result.Exists);
        }

        [Fact]
        public void PhysicalCache_GetRequiredFileInfo_Missing_Throws()
        {
            using var temp = new TempFolder();
            var fs = new PhysicalFileCacheFileSystem(new PhysicalFileProvider(temp.Path), new Crc32Hasher());

            Assert.Throws<FileNotFoundException>(() => fs.GetRequiredFileInfo("does-not-exist.css"));
        }
    }
}
