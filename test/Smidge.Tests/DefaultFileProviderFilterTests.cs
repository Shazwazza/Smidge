using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using System.Linq;
using Xunit;

namespace Smidge.Tests
{
    public class DefaultFileProviderFilterTests
    {
        [Theory]
        [InlineData("**/*.js")]
        [InlineData("**/*.*")]
        public void Matches_Files_In_Recursive_Folders(string pattern)
        {
            var root = new InMemoryDirectory();
            root.AddFile("", new StringFileInfo("file1", "file1.js"));
            var dir1 = root.GetOrAddFolder("dir1");
            dir1.AddFile(new StringFileInfo("file2", "file2.js"));
            var dir2 = dir1.GetOrAddFolder("dir2");
            dir2.AddFile(new StringFileInfo("file3", "file3.js"));
            dir2.AddFile(new StringFileInfo("file3.5", "jquery-1.12.2.js"));
            var dir3 = dir2.GetOrAddFolder("dir3");
            dir3.AddFile(new StringFileInfo("file4", "file4.js"));

            var fileProvider = new InMemoryFileProvider(root);

            var defaultFileFilter = new DefaultFileProviderFilter();

            var filesFound = defaultFileFilter.GetMatchingFiles(fileProvider, pattern).ToList();

            Assert.Equal(5, filesFound.Count);
            Assert.Contains("file1.js", filesFound);
            Assert.Contains("dir1/file2.js", filesFound);
            Assert.Contains("dir1/dir2/file3.js", filesFound);
            Assert.Contains("dir1/dir2/jquery-1.12.2.js", filesFound);
            Assert.Contains("dir1/dir2/dir3/file4.js", filesFound);
        }

        [Theory]
        [InlineData("**/*.js", 2)]
        [InlineData("dir1/*.js", 2)]
        [InlineData("**/*.*", 5)]
        [InlineData("dir1/*.*", 5)]
        [InlineData("**/*.css", 3)]
        [InlineData("dir1/*.css", 3)]
        [InlineData("*.css", 0)]
        [InlineData("*.*", 0)]
        [InlineData("dir2/*.css", 0)]
        [InlineData("*/*.css", 3)]
        [InlineData("*/*.js", 2)]
        [InlineData("jquery-1.12.2.js", 1)]
        [InlineData("dir1", 5)]
        public void Matches_Files_In_Folders(string pattern, int count)
        {
            var root = new InMemoryDirectory();            
            var dir1 = root.GetOrAddFolder("dir1");
            dir1.AddFile(new StringFileInfo("file2", "jquery-1.12.2.js"));
            dir1.AddFile(new StringFileInfo("file3", "file3.js"));
            dir1.AddFile(new StringFileInfo("file4", "file4.css"));
            dir1.AddFile(new StringFileInfo("file5", "file5.css"));
            dir1.AddFile(new StringFileInfo("file6", "file6.css"));

            var fileProvider = new InMemoryFileProvider(root);

            var defaultFileFilter = new DefaultFileProviderFilter();

            var filesFound = defaultFileFilter.GetMatchingFiles(fileProvider, pattern).ToList();

            Assert.Equal(count, filesFound.Count);

        }
    }
}
