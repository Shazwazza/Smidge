using System;
using Microsoft.AspNetCore.Hosting;

using Moq;
using Microsoft.Extensions.FileProviders;
using Xunit;

using Smidge.Models;
using Smidge.Hashing;
using Smidge.Options;
using System.Linq;
using Microsoft.Extensions.Logging;
using Smidge.FileProcessors;
using Microsoft.Extensions.Options;
using Smidge.Cache;

namespace Smidge.Tests
{
    public class BundleFileSetGeneratorTests
    {
        [Fact]
        public void Get_Ordered_File_Set_No_Duplicates()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var fileProvider = new Mock<IFileProvider>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var logger = new Mock<ILogger>();
            var fileProviderFilter = new DefaultFileProviderFilter();

            var fileSystemHelper = new SmidgeFileSystem(fileProvider.Object, fileProviderFilter, cacheProvider.Object, Mock.Of<IWebsiteInfo>(), logger.Object);
            var pipeline = new PreProcessPipeline(Enumerable.Empty<IPreProcessor>());
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions());

            var generator = new BundleFileSetGenerator(fileSystemHelper,
                                                       new FileProcessingConventions(smidgeOptions.Object, Enumerable.Empty<IFileProcessingConvention>()));

            var result = generator.GetOrderedFileSet(new IWebFile[] {
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js")
            }, pipeline);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Get_Ordered_File_External_Set()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var fileProvider = new Mock<IFileProvider>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var logger = new Mock<ILogger>();
            var fileProviderFilter = new DefaultFileProviderFilter();

            var fileSystemHelper = new SmidgeFileSystem(fileProvider.Object, fileProviderFilter, cacheProvider.Object, Mock.Of<IWebsiteInfo>(), logger.Object);
            var pipeline = new PreProcessPipeline(Enumerable.Empty<IPreProcessor>());
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions());

            var generator = new BundleFileSetGenerator(fileSystemHelper,
                                                       new FileProcessingConventions(smidgeOptions.Object, Enumerable.Empty<IFileProcessingConvention>()));

            var result = generator.GetOrderedFileSet(new IWebFile[] {
                Mock.Of<IWebFile>(f => f.FilePath == "http://test-site.com/test.js"),
                Mock.Of<IWebFile>(f => f.FilePath == "https://external-site.com/external.js")
            }, pipeline);

            Assert.Equal(2, result.Count());
            Assert.Equal("http://test-site.com/test.js", result.ElementAt(0).FilePath);
            Assert.Equal("https://external-site.com/external.js", result.ElementAt(1).FilePath);
        }

        [Fact]
        public void Get_Ordered_File_Set_Correct_Order()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var fileProvider = new Mock<IFileProvider>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var logger = new Mock<ILogger>();
            var fileProviderFilter = new DefaultFileProviderFilter();

            var fileSystemHelper = new SmidgeFileSystem(fileProvider.Object, fileProviderFilter, cacheProvider.Object, Mock.Of<IWebsiteInfo>(), logger.Object);
            var pipeline = new PreProcessPipeline(Enumerable.Empty<IPreProcessor>());
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions());

            var generator = new BundleFileSetGenerator(fileSystemHelper,
                                                       new FileProcessingConventions(smidgeOptions.Object, Enumerable.Empty<IFileProcessingConvention>()));

            var result = generator.GetOrderedFileSet(new IWebFile[] {
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test_2.js" && f.Order == 1),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test_3.js")
            }, pipeline).ToList();

            Assert.Equal(1, result[2].Order);

            result = generator.GetOrderedFileSet(new IWebFile[] {
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js" && f.Order == 2),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test_2.js" && f.Order == 1),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test_3.js")
            }, pipeline).ToList();
            
            Assert.Equal(1, result[1].Order);
            Assert.Equal(2, result[2].Order);

            result = generator.GetOrderedFileSet(new IWebFile[] {
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test_2.js"),
                Mock.Of<IWebFile>(f => f.FilePath == "~/test/test_3.js")
            }, pipeline).ToList();
            
            Assert.Equal("~/test/test.js", result[0].FilePath);
            Assert.Equal("~/test/test_2.js", result[1].FilePath);
            Assert.Equal("~/test/test_3.js", result[2].FilePath);
        }
    }
}
