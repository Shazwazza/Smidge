using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Smidge.Tests
{
    public class DefaultFileProviderFilterTests
    {
        [Theory]
        [InlineData("/**/*.js")]
        [InlineData("/**/*.*")]
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
        [InlineData("/**/*.js", 2)]
        [InlineData("/dir1/*.js", 2)]
        [InlineData("/**/*.*", 5)]
        [InlineData("/dir1/*.*", 5)]
        [InlineData("/**/*.css", 3)]
        [InlineData("/dir1/*.css", 3)]
        [InlineData("/*.css", 0)]
        [InlineData("/*.*", 0)]
        [InlineData("/dir2/*.css", 0)]
        [InlineData("/*/*.css", 3)]
        [InlineData("/*/*.js", 2)]
        [InlineData("/jquery-1.12.2.js", 1)]
        [InlineData("/dir1", 5)]
        [InlineData("/**.css", 3)] // RFC's literal (non-idiomatic) recursive syntax
        [InlineData("/**.js", 2)]
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

        /// <summary>
        /// Regression coverage for https://github.com/Shazwazza/Smidge/issues/197 (RFC: globbing
        /// pattern support) exercised against a real <see cref="PhysicalFileProvider"/>, which is
        /// the branch of <see cref="DefaultFileProviderFilter"/> that uses the built-in
        /// <see cref="Microsoft.Extensions.FileSystemGlobbing.Matcher"/> and is what real ASP.NET Core
        /// apps hit in production (the other tests in this file only exercise the non-physical
        /// fallback matcher via <see cref="InMemoryFileProvider"/>).
        /// </summary>
        [Theory]
        [InlineData("/**/*.css", 3)] // recursive glob, correct Matcher syntax
        [InlineData("/dir1/*.css", 1)] // single directory level
        [InlineData("/**/*.js", 2)]
        public void Matches_Files_With_Physical_File_Provider(string pattern, int count)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "smidge-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(tempRoot, "dir1", "dir2"));
            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "a.css"), "a{}");
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "b.js"), "b");
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "dir2", "c.css"), "c{}");
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "dir2", "d.css"), "d{}");
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "dir2", "e.js"), "e");

                var fileProvider = new PhysicalFileProvider(tempRoot);
                var defaultFileFilter = new DefaultFileProviderFilter();

                var filesFound = defaultFileFilter.GetMatchingFiles(fileProvider, pattern).ToList();

                Assert.Equal(count, filesFound.Count);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        /// <summary>
        /// Regression test for https://github.com/Shazwazza/Smidge/issues/197 (RFC: globbing
        /// pattern support): verifies the reporter's exact literal pattern syntax
        /// (<c>**.css</c>, with no slash between the recursive wildcard and the extension)
        /// already matches recursively against a real <see cref="PhysicalFileProvider"/>, and
        /// that the idiomatic <c>Microsoft.Extensions.FileSystemGlobbing</c> equivalent
        /// (<c>**/*.css</c>) works the same way.
        /// </summary>
        [Fact]
        public void Matches_Files_Recursively_With_Issue197_Literal_DoubleStar_Extension_Syntax()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "smidge-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(tempRoot, "dir1", "dir2"));
            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "a.css"), "a{}");
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "b.css"), "b{}");
                File.WriteAllText(Path.Combine(tempRoot, "dir1", "dir2", "c.css"), "c{}");

                var fileProvider = new PhysicalFileProvider(tempRoot);
                var defaultFileFilter = new DefaultFileProviderFilter();

                // Document current behavior for the RFC's literal (non-idiomatic) pattern.
                // .NET's Matcher treats "**.css" equivalently to "**/*.css" - it already works.
                var filesFound = defaultFileFilter.GetMatchingFiles(fileProvider, "/**.css").ToList();
                Assert.Equal(3, filesFound.Count);

                // The correct/idiomatic pattern must work regardless.
                var correctSyntax = defaultFileFilter.GetMatchingFiles(fileProvider, "/**/*.css").ToList();
                Assert.Equal(3, correctSyntax.Count);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}
