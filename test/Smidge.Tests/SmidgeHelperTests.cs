using System;
using Microsoft.AspNetCore.Http;
using Moq;
using Smidge.CompositeFiles;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.Hashing;
using Smidge.FileProcessors;
using Smidge.Options;
using Smidge.Tests.Helpers;
using Xunit;

namespace Smidge.Tests
{
    public class SmidgeHelperTests
    {
        private readonly IFileProvider _fileProvider = Mock.Of<IFileProvider>();
        private readonly ICacheFileSystem _cacheProvider = Mock.Of<ICacheFileSystem>();
        private readonly IHasher _hasher = Mock.Of<IHasher>();
        private readonly IEnumerable<IPreProcessor> _preProcessors = new List<IPreProcessor>();
        private readonly IBundleFileSetGenerator _fileSetGenerator;
        private readonly DynamicallyRegisteredWebFiles _dynamicallyRegisteredWebFiles;
        private readonly SmidgeFileSystem _fileSystemHelper;
        private readonly PreProcessManager _preProcessManager;
        private readonly PreProcessPipelineFactory _processorFactory;
        private readonly IBundleManager _bundleManager;
        private readonly IRequestHelper _requestHelper;
        private readonly IUrlManager _urlManager;
        private readonly CacheBusterResolver _cacheBusterResolver;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public SmidgeHelperTests()
        {
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(Mock.Of<HttpContext>);
            _httpContextAccessor = httpContextAccessor.Object;
            
            _dynamicallyRegisteredWebFiles = new DynamicallyRegisteredWebFiles();
            _fileSystemHelper = new SmidgeFileSystem(_fileProvider, new DefaultFileProviderFilter(), _cacheProvider, new FakeWebsiteInfo());
            
            var smidgeConfig = new Mock<ISmidgeConfig>();
            smidgeConfig.Setup(c => c.KeepFileExtensions).Returns(false);

            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(() =>
            {
                var options = new SmidgeOptions
                {
                    UrlOptions = new UrlManagerOptions(),
                    DefaultBundleOptions = new BundleEnvironmentOptions()
                };
                options.DefaultBundleOptions.DebugOptions.SetCacheBusterType<FakeCacheBuster>();
                options.DefaultBundleOptions.ProductionOptions.SetCacheBusterType<FakeCacheBuster>();
                return options;
            });

            _requestHelper = new RequestHelper(new FakeWebsiteInfo());
            _urlManager = new DefaultUrlManager(smidgeOptions.Object, _hasher, _requestHelper, smidgeConfig.Object);

            _cacheBusterResolver = new CacheBusterResolver(FakeCacheBuster.Instances);
            
            _processorFactory = new PreProcessPipelineFactory(new Lazy<IEnumerable<IPreProcessor>>(() => _preProcessors));
            _bundleManager = new BundleManager(smidgeOptions.Object, Mock.Of<ILogger<BundleManager>>());
            _preProcessManager = new PreProcessManager(
                _fileSystemHelper,
                _bundleManager,
                Mock.Of<ILogger<PreProcessManager>>());
            _fileSetGenerator = new BundleFileSetGenerator(
                _fileSystemHelper,
                new FileProcessingConventions(smidgeOptions.Object, new List<IFileProcessingConvention>()));
        }

        [Fact]
        public async Task JsHereAsync_Returns_Empty_String_Result_When_No_Files_Found()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DefaultProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateJs("empty", Array.Empty<string>());

            var result = (await sut.JsHereAsync("empty")).ToString();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task Generate_Css_Urls_For_Non_Existent_Bundle_Throws_Exception()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DebugProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () => await sut.GenerateCssUrlsAsync("DoesntExist")

                    );

        }



        [Fact]
        public async Task Generate_Css_Urls_Returns_SingleBundleUrl_When_Default_Profile_Is_Used()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DefaultProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateCss("test", new[]
            {
                "file1.css",
                "file2.css"
            });

            var dir = new InMemoryDirectory();
            dir.AddFile("", new StringFileInfo("File1", "file1.css"));
            dir.AddFile("", new StringFileInfo("File2", "file2.css"));
            var fileProvider = new InMemoryFileProvider(dir);

            // Configure the mock file provider to use the temporary file provider we've just configured
            Mock.Get(_fileProvider).Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string s) => fileProvider.GetFileInfo(s));
            Mock.Get(_fileProvider).Setup(f => f.GetDirectoryContents(It.IsAny<string>())).Returns((string s) => fileProvider.GetDirectoryContents(s));

            var urls = await sut.GenerateCssUrlsAsync("test");

            Assert.Equal("/sb/test.css.v00000", urls.FirstOrDefault());
        }


        [Fact]
        public async Task Generate_Css_Urls_Returns_Multiple_Urls_When_Debug_Profile_Is_Used()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DebugProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateCss("test", new[]
            {
                "file1.css",
                "file2.css"
            });

            var dir = new InMemoryDirectory();
            dir.AddFile("", new StringFileInfo("File1", "file1.css"));
            dir.AddFile("", new StringFileInfo("File2", "file2.css"));
            var fileProvider = new InMemoryFileProvider(dir);

            // Configure the mock file provider to use the temporary file provider we've just configured
            Mock.Get(_fileProvider).Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string s) => fileProvider.GetFileInfo(s));
            Mock.Get(_fileProvider).Setup(f => f.GetDirectoryContents(It.IsAny<string>())).Returns((string s) => fileProvider.GetDirectoryContents(s));

            var urls = await sut.GenerateJsUrlsAsync("test");

            Assert.Equal("/file1.css?v=00000", urls.ElementAtOrDefault(0));
            Assert.Equal("/file2.css?v=00000", urls.ElementAtOrDefault(1));
        }


        [Fact]
        public async Task Generate_Css_Urls_Returns_Urls_With_Debug_Token_When_Debug_Parameter_Overrides_Profile()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DefaultProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateCss("test", new[]
            {
                "file1.css",
                "file2.css"
            });

            var dir = new InMemoryDirectory();
            dir.AddFile("", new StringFileInfo("File1", "file1.css"));
            dir.AddFile("", new StringFileInfo("File2", "file2.css"));
            var fileProvider = new InMemoryFileProvider(dir);

            // Configure the mock file provider to use the temporary file provider we've just configured
            Mock.Get(_fileProvider).Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string s) => fileProvider.GetFileInfo(s));
            Mock.Get(_fileProvider).Setup(f => f.GetDirectoryContents(It.IsAny<string>())).Returns((string s) => fileProvider.GetDirectoryContents(s));

            var urls = await sut.GenerateJsUrlsAsync("test", debug: true);

            Assert.Equal("/file1.css?d=00000", urls.ElementAtOrDefault(0));
            Assert.Equal("/file2.css?d=00000", urls.ElementAtOrDefault(1));
        }


        [Fact]
        public async Task Generate_Js_Urls_For_Non_Existent_Bundle_Throws_Exception()
        {

            var sut = new SmidgeHelper(
                FakeProfileStrategy.DebugProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () => await sut.GenerateJsUrlsAsync("DoesntExist")
                    );
        }




        [Fact]
        public async Task Generate_Js_Urls_Returns_SingleBundleUrl_When_Default_Profile_Is_Used()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DefaultProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateJs("test", new[]
            {
                "file1.js",
                "file2.js"
            });

            var dir = new InMemoryDirectory();
            dir.AddFile("", new StringFileInfo("File1", "file1.js"));
            dir.AddFile("", new StringFileInfo("File2", "file2.js"));
            var fileProvider = new InMemoryFileProvider(dir);

            // Configure the mock file provider to use the temporary file provider we've just configured
            Mock.Get(_fileProvider).Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string s) => fileProvider.GetFileInfo(s));
            Mock.Get(_fileProvider).Setup(f => f.GetDirectoryContents(It.IsAny<string>())).Returns((string s) => fileProvider.GetDirectoryContents(s));

            var urls = await sut.GenerateJsUrlsAsync("test");

            Assert.Equal("/sb/test.js.v00000", urls.FirstOrDefault());
        }


        [Fact]
        public async Task Generate_Js_Urls_Returns_Multiple_Urls_When_Debug_Profile_Is_Used()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DebugProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateJs("test", new[]
            {
                "file1.js",
                "file2.js"
            });

            var dir = new InMemoryDirectory();
            dir.AddFile("", new StringFileInfo("File1", "file1.js"));
            dir.AddFile("", new StringFileInfo("File2", "file2.js"));
            var fileProvider = new InMemoryFileProvider(dir);

            // Configure the mock file provider to use the temporary file provider we've just configured
            Mock.Get(_fileProvider).Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string s) => fileProvider.GetFileInfo(s));
            Mock.Get(_fileProvider).Setup(f => f.GetDirectoryContents(It.IsAny<string>())).Returns((string s) => fileProvider.GetDirectoryContents(s));

            var urls = await sut.GenerateJsUrlsAsync("test");

            Assert.Equal("/file1.js?v=00000", urls.ElementAtOrDefault(0));
            Assert.Equal("/file2.js?v=00000", urls.ElementAtOrDefault(1));
        }


        [Fact]
        public async Task Generate_Js_Urls_Returns_Urls_With_Debug_Token_When_Debug_Parameter_Overrides_Profile()
        {
            var sut = new SmidgeHelper(
                FakeProfileStrategy.DefaultProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            _bundleManager.CreateJs("test", new[]
            {
                "file1.js",
                "file2.js"
            });

            var dir = new InMemoryDirectory();
            dir.AddFile("", new StringFileInfo("File1", "file1.js"));
            dir.AddFile("", new StringFileInfo("File2", "file2.js"));
            var fileProvider = new InMemoryFileProvider(dir);

            // Configure the mock file provider to use the temporary file provider we've just configured
            Mock.Get(_fileProvider).Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string s) => fileProvider.GetFileInfo(s));
            Mock.Get(_fileProvider).Setup(f => f.GetDirectoryContents(It.IsAny<string>())).Returns((string s) => fileProvider.GetDirectoryContents(s));

            var urls = await sut.GenerateJsUrlsAsync("test", debug: true);

            Assert.Equal("/file1.js?d=00000", urls.ElementAtOrDefault(0));
            Assert.Equal("/file2.js?d=00000", urls.ElementAtOrDefault(1));
        }



        [Fact]
        public async Task CssHere_HtmlString_For_Non_Existent_Css_Bundle_Throws_Exception()
        {

            var sut = new SmidgeHelper(
                FakeProfileStrategy.DebugProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () =>
                        {
                            var result = await sut.CssHereAsync("doesn't exist");
                        }

                    );


        }


        [Fact]
        public async Task JsHere_HtmlString_For_Non_Existent_Css_Bundle_Throws_Exception()
        {

            var sut = new SmidgeHelper(
                FakeProfileStrategy.DebugProfileStrategy,
                _fileSetGenerator,
                _dynamicallyRegisteredWebFiles, _preProcessManager, _fileSystemHelper,
                _hasher, _bundleManager, _processorFactory, _urlManager, _requestHelper,
                _httpContextAccessor, _cacheBusterResolver);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () =>
                        {
                            var result = await sut.JsHereAsync("doesn't exist");
                        }

                    );


        }



    }
}
