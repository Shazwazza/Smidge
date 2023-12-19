using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;
using Smidge.Hashing;
using Smidge.Cache;

namespace Smidge.Tests
{
    public class FileBatcherTests
    {
        [Fact]
        public void Get_Composite_File_Collection_For_Url_Generation_No_Duplicates()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var fileProvider = new Mock<IFileProvider>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var logger = new Mock<ILogger>();
            var fileProviderFilter = new DefaultFileProviderFilter();

            var hasher = Mock.Of<IHasher>();
            var fileSystemHelper = new SmidgeFileSystem(fileProvider.Object, fileProviderFilter, cacheProvider.Object, Mock.Of<IWebsiteInfo>(), logger.Object);
            var batcher = new FileBatcher(fileSystemHelper, urlHelper, hasher);

            var file = new Mock<IFileInfo>();
            file.Setup(a => a.IsDirectory).Returns(false);
            file.SetupAllProperties();
            fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(file.Object);

            var result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js")
                });

            Assert.Equal(2, result.First().Files.Count());
        }

        [Fact]
        public void Get_Composite_File_Collection_For_Url_Generation()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var fileProvider = new Mock<IFileProvider>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var logger = new Mock<ILogger>();
            var fileProviderFilter = new DefaultFileProviderFilter();
            var hasher = Mock.Of<IHasher>();
            var fileSystemHelper = new SmidgeFileSystem(fileProvider.Object, fileProviderFilter, cacheProvider.Object, Mock.Of<IWebsiteInfo>(), logger.Object);          
            var batcher = new FileBatcher(fileSystemHelper, urlHelper, hasher);

            var file = new Mock<IFileInfo>();
            file.Setup(a => a.IsDirectory).Returns(false);
            file.SetupAllProperties();
            fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(file.Object);

            //test a mix start/ending with external
            var result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/jquery/2.1.1/jquery.min.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test.min.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "http://cdnjs.cloudflare.com/ajax/libs/test2.min.js"),
                });

            Assert.Equal(4, result.Count());

            //start/end with internal
            result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test.min.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test2.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world2.js"),
                });

            Assert.Equal(3, result.Count());

            //all internal
            result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test2.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world2.js"),
                });

            Assert.Single(result);

            //start internal/end external
            result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test2.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test.min.js"),
                });

            Assert.Equal(2, result.Count());

            //start external/end internal
            result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test.min.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test2.js"),                    
                });

            Assert.Equal(2, result.Count());

            //all external
            result = batcher.GetCompositeFileCollectionForUrlGeneration(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test.min.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test2.min.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "//cdnjs.cloudflare.com/ajax/libs/test3.min.js")
                });

            Assert.Equal(3, result.Count());
        }
    }
}
