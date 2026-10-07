using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Smidge.CompositeFiles;
using NUglify.Css;
using NUglify.JavaScript;
using Smidge.FileProcessors;
using Smidge.Models;
using Smidge.Nuglify;
using Xunit;

namespace Smidge.Tests
{
    public class NuglifyProcessorTests
    {
        // NUglify reports CSS1036 "Expected expression, found ';'" for an empty declaration value
        private const string InvalidCss = ".a { color: red; }\n.b { width: ; }";
        private const string InvalidJs = "var a = 1;\nvar b = ;";

        [Fact]
        public async Task Css_With_Errors_Uses_Original_Content_By_Default()
        {
            var logger = new Mock<ILogger<NuglifyCss>>();
            var processor = new NuglifyCss(new NuglifySettings(), GetRequestHelper(), logger.Object);

            var content = await ProcessAsync(processor, InvalidCss, new CssFile("css/site.css"));

            Assert.Equal(InvalidCss, content);
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("css/site.css") && v.ToString().Contains("CSS1036")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Css_With_Errors_Throws_With_File_And_Position_When_Configured()
        {
            var settings = new NuglifySettings { ErrorBehavior = NuglifyErrorBehavior.Throw };
            var processor = new NuglifyCss(settings, GetRequestHelper());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ProcessAsync(processor, InvalidCss, new CssFile("css/site.css")));

            Assert.Contains("css/site.css", ex.Message);
            Assert.Contains("CSS1036", ex.Message);
            Assert.Contains("Expected expression, found ';'", ex.Message);
            Assert.Contains("line 2", ex.Message);
        }

        [Fact]
        public async Task Css_Is_Minified_When_Valid()
        {
            var processor = new NuglifyCss(new NuglifySettings(), GetRequestHelper());

            var content = await ProcessAsync(processor, ".a { color: red; }", new CssFile("css/site.css"));

            Assert.Equal(".a{color:#f00}", content);
        }

        [Fact]
        public async Task Css_Is_Not_Minified_When_Minification_Disabled()
        {
            var settings = new NuglifySettings { EnableMinification = false };
            var processor = new NuglifyCss(settings, GetRequestHelper());

            Assert.Equal(".a { color: red; }", await ProcessAsync(processor, ".a { color: red; }", new CssFile("css/site.css")));
            Assert.Equal(InvalidCss, await ProcessAsync(processor, InvalidCss, new CssFile("css/site.css")));
        }

        [Fact]
        public void Css_Settings_Can_Be_Replaced()
        {
            var cssSettings = new CssSettings();
            var settings = new NuglifySettings { CssCodeSettings = cssSettings };

            Assert.Same(cssSettings, settings.CssCodeSettings);
            Assert.NotNull(settings.JsCodeSettings);
        }

        [Fact]
        public async Task Js_With_Errors_Uses_Original_Content_By_Default()
        {
            var logger = new Mock<ILogger<NuglifyJs>>();
            var processor = new NuglifyJs(GetJsSettings(), Mock.Of<ISourceMapDeclaration>(), GetRequestHelper(), logger.Object);

            var content = await ProcessAsync(processor, InvalidJs, new JavaScriptFile("js/site.js"));

            Assert.Equal(InvalidJs, content);
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("js/site.js")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Js_With_Errors_Throws_With_File_And_Position_When_Configured()
        {
            var settings = new NuglifySettings(new NuglifyCodeSettings { SourceMapType = SourceMapType.None }, new CssSettings())
            {
                ErrorBehavior = NuglifyErrorBehavior.Throw
            };
            var processor = new NuglifyJs(settings, Mock.Of<ISourceMapDeclaration>(), GetRequestHelper());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ProcessAsync(processor, InvalidJs, new JavaScriptFile("js/site.js")));

            Assert.Contains("js/site.js", ex.Message);
            Assert.Contains("line 2", ex.Message);
        }

        [Fact]
        public async Task Js_Is_Not_Minified_When_Minification_Disabled()
        {
            const string js = "var someVariable = 1;\nalert(someVariable);";
            var settings = new NuglifySettings { EnableMinification = false };
            var processor = new NuglifyJs(settings, Mock.Of<ISourceMapDeclaration>(), GetRequestHelper());

            Assert.Equal(js, await ProcessAsync(processor, js, new JavaScriptFile("js/site.js")));
        }

        private static NuglifySettings GetJsSettings()
            => new NuglifySettings(new NuglifyCodeSettings { SourceMapType = SourceMapType.None }, new CssSettings());

        private static async Task<string> ProcessAsync(IPreProcessor processor, string content, IWebFile file)
        {
            using (var bc = BundleContext.CreateEmpty("1"))
            {
                var context = new FileProcessContext(content, file, bc);
                await processor.ProcessAsync(context, ctx => Task.CompletedTask);
                return context.FileContent;
            }
        }

        private static IRequestHelper GetRequestHelper()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));
            return new RequestHelper(websiteInfo.Object);
        }
    }
}
