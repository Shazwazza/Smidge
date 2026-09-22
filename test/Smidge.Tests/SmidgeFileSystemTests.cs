using System;
using Moq;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Smidge.Cache;
using Smidge.Models;
using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;

namespace Smidge.Tests
{
    public class SmidgeFileSystemTests
    {
        private ISmidgeFileSystem Create(IWebsiteInfo websiteInfo, string url = "~/Js/Test1.js")
        {
            var webRootPath = $"C:{Path.DirectorySeparatorChar}MySolution{Path.DirectorySeparatorChar}MyProject";

            var cacheProvider = new Mock<ICacheFileSystem>();
            var fileProvider = new Mock<IFileProvider>();
            var fileProviderFilter = new DefaultFileProviderFilter();
            var file = new Mock<IFileInfo>();
            string filePath = Path.Combine(webRootPath, $"Js{Path.DirectorySeparatorChar}Test1.js");

            file.Setup(x => x.Exists).Returns(false);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(Path.GetFileName(url));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(file.Object);

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new SmidgeFileSystem(
                fileProvider.Object,
                fileProviderFilter,
                cacheProvider.Object,
                websiteInfo);


            return helper;
        }

        [Theory]
        [InlineData("~/test/file.css", "/test/file.css", null)]
        [InlineData("/", "/", null)]
        [InlineData("/test/file.css", "/test/file.css", null)]
        [InlineData("/sub-site/test/file.css", "/test/file.css", "sub-site")]
        [InlineData("/sub/site/test/file.css", "/test/file.css", "sub/site")]
        [InlineData("test/file.css", "/test/file.css", null)]
        [InlineData("file.css", "/file.css", null)]
        [InlineData("~/file.css", "/file.css", null)]
        public void ConvertToFileProviderPath(string from, string to, string pathBase)
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            if (pathBase != null)
            {
                websiteInfo.Setup(x => x.GetBasePath()).Returns(pathBase);
            }
            var fs = Create(websiteInfo.Object);

            var result = fs.ConvertToFileProviderPath(from);

            Assert.Equal(to, result);
        }

        [Fact]
        public void Get_File_Info_Non_Existent_File_Throws_Informative_Exception()
        {
            var url = "~/Js/Test1.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var helper = Create(websiteInfo.Object, url);

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() => helper.GetRequiredFileInfo(url));

            //    var result = helper.MapPath(url);

            Assert.Contains(url, ex.Message);
        }

        [Fact]
        public void Reverse_Map_Path()
        {
            var webRootPath = $"C:{Path.DirectorySeparatorChar}MySolution{Path.DirectorySeparatorChar}MyProject";
            var subPath = $"Js{Path.DirectorySeparatorChar}Test1.js";
            var filePath = Path.Combine(webRootPath, subPath);

            var file = new Mock<IFileInfo>();
            file.Setup(x => x.Exists).Returns(true);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(Path.GetFileName(filePath));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            var urlHelper = new Mock<IUrlHelper>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var fileProvider = new Mock<IFileProvider>();
            var fileProviderFilter = new DefaultFileProviderFilter();

            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new SmidgeFileSystem(
                fileProvider.Object,
                fileProviderFilter,
                cacheProvider.Object,
                Mock.Of<IWebsiteInfo>());

            var result = helper.ReverseMapPath(subPath, file.Object);

            //Expected: ~/Js/Test1.js
            //Actual:   ~/Js/Test1.js/Js\Test1.js

            Assert.Equal("~/Js/Test1.js", result);
        }

        [Fact]
        public void GetMatchingFiles_Directory_Constrains_To_FileType()
        {
            // A directory bundle should only pick up files that match its web file type,
            // not generated artifacts (.gz, .map) or files of the wrong type.
            var root = new InMemoryDirectory();
            var dir = root.GetOrAddFolder("Js").GetOrAddFolder("Bundle2");
            dir.AddFile(new StringFileInfo("var a=1;", "b1.js"));
            dir.AddFile(new StringFileInfo("var b=2;", "b2.js"));
            dir.AddFile(new StringFileInfo("gzip-bytes", "b1.js.gz"));
            dir.AddFile(new StringFileInfo("/*map*/", "b1.js.map"));
            dir.AddFile(new StringFileInfo(".x{}", "styles.css"));

            var fileProvider = new InMemoryFileProvider(root);
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);

            var fs = new SmidgeFileSystem(
                fileProvider,
                new DefaultFileProviderFilter(),
                Mock.Of<ICacheFileSystem>(),
                websiteInfo.Object);

            var jsFiles = fs.GetMatchingFiles("~/Js/Bundle2", WebFileType.Js).ToList();
            Assert.Equal(2, jsFiles.Count);
            Assert.Contains("~/Js/Bundle2/b1.js", jsFiles);
            Assert.Contains("~/Js/Bundle2/b2.js", jsFiles);

            var cssFiles = fs.GetMatchingFiles("~/Js/Bundle2", WebFileType.Css).ToList();
            Assert.Single(cssFiles);
            Assert.Contains("~/Js/Bundle2/styles.css", cssFiles);

            // Back-compat: the untyped overload still matches everything in the directory.
            var all = fs.GetMatchingFiles("~/Js/Bundle2").ToList();
            Assert.Equal(5, all.Count);
        }

        /// <summary>
        /// Regression test for https://github.com/Shazwazza/Smidge/issues/197 (RFC: globbing
        /// pattern support). Reproduces the reporter's exact bundle-file declaration scenario -
        /// a recursive glob such as <c>~/assets/css/**.css</c> or <c>~/assets/css/**/*.css</c> -
        /// against a nested directory tree with mixed file extensions, verified through
        /// <see cref="SmidgeFileSystem.GetMatchingFiles(string, WebFileType)"/> (the same method
        /// <c>BundleFileSetGenerator</c> uses to build a bundle's file list). Exercised against a
        /// non-physical (in-memory) provider, which is the fallback matching branch of
        /// <see cref="DefaultFileProviderFilter"/>.
        /// </summary>
        [Theory]
        [InlineData("~/assets/css/**.css")] // exact literal syntax from the reported issue
        [InlineData("~/assets/css/**/*.css")] // idiomatic Microsoft.Extensions.FileSystemGlobbing syntax
        public void GetMatchingFiles_Recursive_Glob_Matches_Nested_Css_Files_NonPhysical_Provider(string pattern)
        {
            var root = new InMemoryDirectory();
            var css = root.GetOrAddFolder("assets").GetOrAddFolder("css");
            css.AddFile(new StringFileInfo(".a{}", "a.css"));
            css.AddFile(new StringFileInfo("var a=1;", "a.js"));
            var sub = css.GetOrAddFolder("sub");
            sub.AddFile(new StringFileInfo(".b{}", "b.css"));
            sub.AddFile(new StringFileInfo("var b=1;", "b.js"));
            var subsub = sub.GetOrAddFolder("subsub");
            subsub.AddFile(new StringFileInfo(".c{}", "c.css"));

            var fileProvider = new InMemoryFileProvider(root);
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);

            var fs = new SmidgeFileSystem(
                fileProvider,
                new DefaultFileProviderFilter(),
                Mock.Of<ICacheFileSystem>(),
                websiteInfo.Object);

            var cssFiles = fs.GetMatchingFiles(pattern, WebFileType.Css).ToList();

            Assert.Equal(3, cssFiles.Count);
            Assert.Contains("~/assets/css/a.css", cssFiles);
            Assert.Contains("~/assets/css/sub/b.css", cssFiles);
            Assert.Contains("~/assets/css/sub/subsub/c.css", cssFiles);
            Assert.DoesNotContain(cssFiles, f => f.EndsWith(".js"));
        }

        /// <summary>
        /// Same scenario as <see cref="GetMatchingFiles_Recursive_Glob_Matches_Nested_Css_Files_NonPhysical_Provider"/>
        /// but against a real <see cref="PhysicalFileProvider"/>, which is the branch of
        /// <see cref="DefaultFileProviderFilter"/> that uses the built-in
        /// <see cref="Microsoft.Extensions.FileSystemGlobbing.Matcher"/> and is what real
        /// ASP.NET Core apps hit in production.
        /// </summary>
        [Theory]
        [InlineData("~/assets/css/**.css")]
        [InlineData("~/assets/css/**/*.css")]
        public void GetMatchingFiles_Recursive_Glob_Matches_Nested_Css_Files_Physical_Provider(string pattern)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "smidge-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(tempRoot, "assets", "css", "sub", "subsub"));
            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "assets", "css", "a.css"), ".a{}");
                File.WriteAllText(Path.Combine(tempRoot, "assets", "css", "a.js"), "var a=1;");
                File.WriteAllText(Path.Combine(tempRoot, "assets", "css", "sub", "b.css"), ".b{}");
                File.WriteAllText(Path.Combine(tempRoot, "assets", "css", "sub", "b.js"), "var b=1;");
                File.WriteAllText(Path.Combine(tempRoot, "assets", "css", "sub", "subsub", "c.css"), ".c{}");

                var fileProvider = new PhysicalFileProvider(tempRoot);
                var websiteInfo = new Mock<IWebsiteInfo>();
                websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);

                var fs = new SmidgeFileSystem(
                    fileProvider,
                    new DefaultFileProviderFilter(),
                    Mock.Of<ICacheFileSystem>(),
                    websiteInfo.Object);

                var cssFiles = fs.GetMatchingFiles(pattern, WebFileType.Css).ToList();

                Assert.Equal(3, cssFiles.Count);
                Assert.Contains("~/assets/css/a.css", cssFiles);
                Assert.Contains("~/assets/css/sub/b.css", cssFiles);
                Assert.Contains("~/assets/css/sub/subsub/c.css", cssFiles);
                Assert.DoesNotContain(cssFiles, f => f.EndsWith(".js"));
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        /// <summary>
        /// Confirms a single-level (non-recursive) glob like <c>~/assets/css/*.css</c> only
        /// matches files directly in that directory, and that a bare directory (no glob at
        /// all) still pulls in everything of the bundle's type at every depth (existing
        /// behavior from #225) - i.e. adding recursive glob support doesn't change either of
        /// these existing, adjacent behaviors.
        /// </summary>
        [Fact]
        public void GetMatchingFiles_NonRecursive_Glob_And_Bare_Directory_Behavior_Unchanged()
        {
            var root = new InMemoryDirectory();
            var css = root.GetOrAddFolder("assets").GetOrAddFolder("css");
            css.AddFile(new StringFileInfo(".a{}", "a.css"));
            var sub = css.GetOrAddFolder("sub");
            sub.AddFile(new StringFileInfo(".b{}", "b.css"));

            var fileProvider = new InMemoryFileProvider(root);
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);

            var fs = new SmidgeFileSystem(
                fileProvider,
                new DefaultFileProviderFilter(),
                Mock.Of<ICacheFileSystem>(),
                websiteInfo.Object);

            // Single-star, single directory level: only the top-level file.
            var singleLevel = fs.GetMatchingFiles("~/assets/css/*.css", WebFileType.Css).ToList();
            Assert.Single(singleLevel);
            Assert.Contains("~/assets/css/a.css", singleLevel);

            // Bare directory: matches everything of the bundle's type, but (like the explicit
            // single-star glob above) only at that directory's own level - it is normalized to
            // "{dir}/*.css", not a recursive "**/*.css" - so nested files are NOT included.
            var bareDir = fs.GetMatchingFiles("~/assets/css", WebFileType.Css).ToList();
            Assert.Single(bareDir);
            Assert.Contains("~/assets/css/a.css", bareDir);
        }
    }
}
