using System;
using Microsoft.AspNetCore.Hosting;

using Moq;
using Microsoft.Extensions.FileProviders;
using Xunit;

using Smidge.Models;
using Smidge.Hashing;
using Smidge.Options;
using System.Linq;
using Smidge.FileProcessors;
using Microsoft.Extensions.Options;

namespace Smidge.Tests
{
    public class BundleFileSetGeneratorTests
    {
        [Fact]
        public void Get_Ordered_File_Set_No_Duplicates()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var fileProvider = new Mock<IFileProvider>();

            var config = Mock.Of<ISmidgeConfig>();
            var hasher = Mock.Of<IHasher>();
            var hostingEnv = Mock.Of<IHostingEnvironment>();
            var fileSystemHelper = new FileSystemHelper(hostingEnv, config, fileProvider.Object, hasher);
            var pipeline = new PreProcessPipeline(Enumerable.Empty<IPreProcessor>());
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions());

            var generator = new BundleFileSetGenerator(fileSystemHelper, urlHelper,
                new FileProcessingConventions(smidgeOptions.Object, Enumerable.Empty<IFileProcessingConvention>()));
            
            var result = generator.GetOrderedFileSet(new IWebFile[] {
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "~/test/test.js"),
                    Mock.Of<IWebFile>(f => f.FilePath == "hello/world.js")
                }, pipeline);
            
            Assert.Equal(2, result.Count());
        }
    }
}