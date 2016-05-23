using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Smidge.Tests
{
    public class FileBatcherTests
    {
        [Fact]
        public void Get_Composite_File_Collection_For_Url_Generation()
        {
            var files = new[] { "", "" };

            var urlHelper = new Mock<IVirtualPathTranslator>();
            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);

            var fileProvider = new Mock<IFileProvider>();

            //var options = new SmidgeOptions();
            var config = Mock.Of<ISmidgeConfig>();
            var hostingEnv = Mock.Of<IHostingEnvironment>();
            var fileSystemHelper = new FileSystemHelper(hostingEnv, config, fileProvider.Object);
            //var helper = new SmidgeHelper(
            //    new SmidgeContext(Mock.Of<IUrlManager>()),
            //    config,
            //    new FileMinifyManager(fileSystemHelper, options),
            //    new FileSystemHelper(appEnv, hostingEnv, config),
            //    Mock.Of<IHasher>(),
            //    new BundleManager(fileSystemHelper),
            //    Mock.Of<IContextAccessor<HttpRequest>>(x => x.Value == Mock.Of<HttpRequest>()));

            var batcher = new FileBatcher(fileSystemHelper, urlHelper.Object, Mock.Of<IHasher>());

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

            Assert.Equal(1, result.Count());

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