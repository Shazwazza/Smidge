using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Smidge.CompositeFiles;
using Smidge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.OptionsModel;
using Smidge.FileProcessors;
using Smidge.Options;
using Xunit;

namespace Smidge.Tests
{
    public class SmidgeHelperTests
    {
        private ISmidgeConfig _config = Mock.Of<ISmidgeConfig>();
        private IUrlManager _urlManager = Mock.Of<IUrlManager>();
        private IUrlHelper _urlHelper = Mock.Of<IUrlHelper>();
        private IApplicationEnvironment _appEnvironment = Mock.Of<IApplicationEnvironment>();
        private IHostingEnvironment _hostingEnvironment = Mock.Of<IHostingEnvironment>();
        private IFileProvider _fileProvider = Mock.Of<IFileProvider>();
        private IHasher _hasher = Mock.Of<IHasher>();
        private IEnumerable<IPreProcessor> _preProcessors = Mock.Of<IEnumerable<IPreProcessor>>();

        private SmidgeContext _smidgeContext;
        private FileSystemHelper _fileSystemHelper;
        private PreProcessManager _preProcessManager;
        private Bundles _bundles;
        private Mock<IOptions<Bundles>> _bundlesOptions;
        private PreProcessPipelineFactory _processorFactory;
        private BundleManager _bundleManager;
        private Mock<IHttpContextAccessor> _httpContextAccessor;
        private Mock<HttpContext> _httpContext;


        public SmidgeHelperTests()
        {
            //  var config = Mock.Of<ISmidgeConfig>();

            _smidgeContext = new SmidgeContext(_urlManager);
            _fileSystemHelper = new FileSystemHelper(_appEnvironment, _hostingEnvironment, _config, _urlHelper, _fileProvider);
            _preProcessManager = new PreProcessManager(_fileSystemHelper, _hasher);

            _bundles = new Bundles();
            _bundlesOptions = new Mock<IOptions<Bundles>>();
            _bundlesOptions.Setup(opt => opt.Value).Returns(_bundles);

            _processorFactory = new PreProcessPipelineFactory(_preProcessors);
            _bundleManager = new BundleManager(_fileSystemHelper, _bundlesOptions.Object, _processorFactory);

            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _httpContext = new Mock<HttpContext>();
            _httpContext.Setup(context => context.Request).Returns(Mock.Of<HttpRequest>());
            _httpContextAccessor.Setup(context => context.HttpContext).Returns(_httpContext.Object);
        }


        [Fact]
        public async void Generate_Css_Urls_For_Non_Existent_Bundle_Throws_Exception()
        {


            var sut = new SmidgeHelper(_smidgeContext, _config, _preProcessManager, _fileSystemHelper, _hasher, _bundleManager, _httpContextAccessor.Object, _processorFactory);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () => await sut.GenerateCssUrlsAsync("DoesntExist", true)

                    );

        }


        [Fact]
        public async void Generate_Js_Urls_For_Non_Existent_Bundle_Throws_Exception()
        {

            var sut = new SmidgeHelper(_smidgeContext, _config, _preProcessManager, _fileSystemHelper, _hasher, _bundleManager, _httpContextAccessor.Object, _processorFactory);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () => await sut.GenerateJsUrlsAsync("DoesntExist", true)
                    );


        }

        [Fact]
        public async void CssHere_HtmlString_For_Non_Existent_Css_Bundle_Throws_Exception()
        {

            var sut = new SmidgeHelper(_smidgeContext, _config, _preProcessManager, _fileSystemHelper, _hasher, _bundleManager, _httpContextAccessor.Object, _processorFactory);

            var exception = await Assert.ThrowsAsync<BundleNotFoundException>
                    (
                        async () =>
                        {
                            var result = await sut.CssHereAsync("doesn't exist");
                        }

                    );


        }


        [Fact]
        public async void JsHere_HtmlString_For_Non_Existent_Css_Bundle_Throws_Exception()
        {

            var sut = new SmidgeHelper(_smidgeContext, _config, _preProcessManager, _fileSystemHelper, _hasher, _bundleManager, _httpContextAccessor.Object, _processorFactory);

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