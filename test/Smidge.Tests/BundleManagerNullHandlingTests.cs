using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Smidge.FileProcessors;
using Smidge.Models;
using Smidge.Options;
using Xunit;

namespace Smidge.Tests
{
    public class BundleManagerNullHandlingTests
    {
        private static (BundleManager BundleManager, Mock<ILogger<BundleManager>> Logger) CreateBundleManager()
        {
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions
            {
                DefaultBundleOptions = new BundleEnvironmentOptions()
            });

            var logger = new Mock<ILogger<BundleManager>>();
            return (new BundleManager(smidgeOptions.Object, logger.Object), logger);
        }

        [Fact]
        public void Create_Js_Bundle_Filters_Null_Files_And_Logs_Warning()
        {
            var (bundleManager, logger) = CreateBundleManager();
            var file1 = new JavaScriptFile("~/test1.js");
            var file2 = new JavaScriptFile("~/test2.js");

            var bundle = bundleManager.Create("test-bundle", new JavaScriptFile[] { file1, null, file2 });

            Assert.Equal(new[] { "~/test1.js", "~/test2.js" }, bundle.Files.Select(x => x.FilePath));
            Assert.DoesNotContain(bundle.Files, f => f == null);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public void Create_Css_Bundle_Filters_Null_Files_And_Logs_Warning()
        {
            var (bundleManager, logger) = CreateBundleManager();
            var file1 = new CssFile("~/test1.css");
            var file2 = new CssFile("~/test2.css");

            var bundle = bundleManager.Create("test-bundle", new CssFile[] { file1, null, file2 });

            Assert.Equal(new[] { "~/test1.css", "~/test2.css" }, bundle.Files.Select(x => x.FilePath));
            Assert.DoesNotContain(bundle.Files, f => f == null);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public void Create_Js_Bundle_With_Pipeline_Filters_Null_Files_And_Assigns_Pipeline()
        {
            var (bundleManager, logger) = CreateBundleManager();
            var pipeline = new PreProcessPipeline(Enumerable.Empty<IPreProcessor>());
            var file = new JavaScriptFile("~/test.js");

            var bundle = bundleManager.Create("test-bundle", pipeline, new JavaScriptFile[] { null, file });

            var result = Assert.Single(bundle.Files);
            Assert.Same(pipeline, result.Pipeline);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public void Create_Css_Bundle_With_Pipeline_Filters_Null_Files_And_Assigns_Pipeline()
        {
            var (bundleManager, logger) = CreateBundleManager();
            var pipeline = new PreProcessPipeline(Enumerable.Empty<IPreProcessor>());
            var file = new CssFile("~/test.css");

            var bundle = bundleManager.Create("test-bundle", pipeline, new CssFile[] { null, file });

            var result = Assert.Single(bundle.Files);
            Assert.Same(pipeline, result.Pipeline);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public void Create_Bundle_With_Null_Files_Does_Not_Break_Bundle_Enumeration()
        {
            var (bundleManager, logger) = CreateBundleManager();

            bundleManager.Create("test-bundle", new JavaScriptFile[] { null, new JavaScriptFile("~/test.js") });

            Assert.Equal(new[] { "test-bundle" }, bundleManager.GetBundleNames(WebFileType.Js));
            Assert.Equal(new[] { "test-bundle" }, bundleManager.GetBundles(WebFileType.Js).Select(x => x.Name));
            VerifyWarningLogged(logger);
        }

        [Fact]
        public void AddToBundle_Still_Rejects_Null_File()
        {
            var (bundleManager, logger) = CreateBundleManager();

            Assert.Throws<ArgumentNullException>(() => bundleManager.AddToBundle("test-bundle", (JavaScriptFile)null));
            VerifyWarningLogged(logger, Times.Never());
        }

        private static void VerifyWarningLogged(Mock<ILogger<BundleManager>> logger)
            => VerifyWarningLogged(logger, Times.Once());

        private static void VerifyWarningLogged(Mock<ILogger<BundleManager>> logger, Times times)
        {
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
    }
}
