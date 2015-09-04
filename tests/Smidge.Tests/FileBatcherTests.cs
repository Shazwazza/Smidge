using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Dnx.Runtime;
using Moq;
using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Linq;
using Xunit;

namespace Smidge.Tests
{
    public class FileBatcherTests
    {
        [Fact]
        public void Get_Composite_File_Collection_For_Url_Generation()
        {
            var files = new[] { "", "" };

            //var options = new SmidgeOptions();
            var appEnv = Mock.Of<IApplicationEnvironment>();
            var config = Mock.Of<ISmidgeConfig>();
            var hostingEnv = Mock.Of<IHostingEnvironment>();
            var fileSystemHelper = new FileSystemHelper(appEnv, hostingEnv, config);
            //var helper = new SmidgeHelper(
            //    new SmidgeContext(Mock.Of<IUrlManager>()),
            //    config,
            //    new FileMinifyManager(fileSystemHelper, options),
            //    new FileSystemHelper(appEnv, hostingEnv, config),
            //    Mock.Of<IHasher>(),
            //    new BundleManager(fileSystemHelper),
            //    Mock.Of<IContextAccessor<HttpRequest>>(x => x.Value == Mock.Of<HttpRequest>()));

            var batcher = new FileBatcher(fileSystemHelper, Mock.Of<HttpRequest>(), Mock.Of<IHasher>());

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