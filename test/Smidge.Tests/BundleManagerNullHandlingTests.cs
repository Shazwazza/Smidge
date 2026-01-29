using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Smidge.Models;
using Smidge.Options;
using Xunit;

namespace Smidge.Tests
{
    public class BundleManagerNullHandlingTests
    {
        private BundleManager CreateBundleManager()
        {
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions());
            var logger = new Mock<ILogger<BundleManager>>();
            return new BundleManager(smidgeOptions.Object, logger.Object);
        }

        [Fact]
        public void Create_Js_Bundle_Filters_Null_Files()
        {
            // Arrange
            var bundleManager = CreateBundleManager();
            var file1 = new JavaScriptFile("~/test1.js");
            var file2 = new JavaScriptFile("~/test2.js");

            // Act
            var bundle = bundleManager.Create("test-bundle", file1, null, file2);

            // Assert
            Assert.NotNull(bundle);
            Assert.Equal(2, bundle.Files.Count);
            Assert.Equal("~/test1.js", bundle.Files[0].FilePath);
            Assert.Equal("~/test2.js", bundle.Files[1].FilePath);
        }

        [Fact]
        public void Create_Css_Bundle_Filters_Null_Files()
        {
            // Arrange
            var bundleManager = CreateBundleManager();
            var file1 = new CssFile("~/test1.css");
            var file2 = new CssFile("~/test2.css");

            // Act
            var bundle = bundleManager.Create("test-bundle", file1, null, file2);

            // Assert
            Assert.NotNull(bundle);
            Assert.Equal(2, bundle.Files.Count);
            Assert.Equal("~/test1.css", bundle.Files[0].FilePath);
            Assert.Equal("~/test2.css", bundle.Files[1].FilePath);
        }

        [Fact]
        public void Create_Js_Bundle_With_All_Null_Files_Creates_Empty_Bundle()
        {
            // Arrange
            var bundleManager = CreateBundleManager();

            // Act
            var bundle = bundleManager.Create("test-bundle", (JavaScriptFile)null, (JavaScriptFile)null);

            // Assert
            Assert.NotNull(bundle);
            Assert.Empty(bundle.Files);
        }

        [Fact]
        public void Create_Css_Bundle_With_All_Null_Files_Creates_Empty_Bundle()
        {
            // Arrange
            var bundleManager = CreateBundleManager();

            // Act
            var bundle = bundleManager.Create("test-bundle", (CssFile)null, (CssFile)null);

            // Assert
            Assert.NotNull(bundle);
            Assert.Empty(bundle.Files);
        }

        [Fact]
        public void AddToBundle_Css_Ignores_Null_File()
        {
            // Arrange
            var bundleManager = CreateBundleManager();
            bundleManager.Create("test-bundle", new CssFile("~/test1.css"));

            // Act - should not throw, just log warning and skip
            bundleManager.AddToBundle("test-bundle", (CssFile)null);

            // Assert
            var bundle = bundleManager.GetBundle("test-bundle");
            Assert.Equal(1, bundle.Files.Count);
        }

        [Fact]
        public void AddToBundle_Js_Ignores_Null_File()
        {
            // Arrange
            var bundleManager = CreateBundleManager();
            bundleManager.Create("test-bundle", new JavaScriptFile("~/test1.js"));

            // Act - should not throw, just log warning and skip
            bundleManager.AddToBundle("test-bundle", (JavaScriptFile)null);

            // Assert
            var bundle = bundleManager.GetBundle("test-bundle");
            Assert.Equal(1, bundle.Files.Count);
        }
    }
}
