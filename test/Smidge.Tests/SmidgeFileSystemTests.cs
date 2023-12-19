using System;
using Moq;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;
using Smidge.Cache;
using System.Collections.Generic;

namespace Smidge.Tests
{
    public class SmidgeFileSystemTests
    {
        private ISmidgeFileSystem Create(IWebsiteInfo websiteInfo, out Mock<ILogger> logger, string url = "~/Js/Test1.js")
        {
            var webRootPath = $"C:{Path.DirectorySeparatorChar}MySolution{Path.DirectorySeparatorChar}MyProject";

            var cacheProvider = new Mock<ICacheFileSystem>();
            var fileProvider = new Mock<IFileProvider>();
            logger = new Mock<ILogger>();
            var fileProviderFilter = new DefaultFileProviderFilter();
            var file = new Mock<IFileInfo>();
            string filePath = Path.Combine(webRootPath, $"Js{Path.DirectorySeparatorChar}Test1.js");

            file.Setup(x => x.Exists).Returns(false);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(Path.GetFileName(url));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(file.Object);

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new SmidgeFileSystem(
                fileProvider.Object,
                fileProviderFilter,
                cacheProvider.Object,
                websiteInfo,
                logger.Object);


            return helper;
        }

        [Theory]
        [InlineData("~/test/file.css", "/test/file.css", null)]
        [InlineData("/", "/", null)]
        [InlineData("/test/file.css", "/test/file.css", null)]
        [InlineData("/sub-site/test/file.css", "/test/file.css", "sub-site")]
        [InlineData("/sub/site/test/file.css", "/test/file.css", "sub/site")]
        [InlineData("test/file.css", "/test/file.css", null)]
        [InlineData("file.css", "/file.css", null)]
        [InlineData("~/file.css", "/file.css", null)]
        public void ConvertToFileProviderPath(string from, string to, string pathBase)
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            if (pathBase != null)
            {
                websiteInfo.Setup(x => x.GetBasePath()).Returns(pathBase);
            }
            var fs = Create(websiteInfo.Object, out _);

            var result = fs.ConvertToFileProviderPath(from);

            Assert.Equal(to, result);
        }

        [Fact]
        public void Get_File_Info_Non_Existent_File_Logs_Informative_Error()
        {
            var url = "~/Js/Test1.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var helper = Create(websiteInfo.Object, out var logger, url);

            helper.GetRequiredFileInfo(url);

            logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    0,
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(url)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                )
            );
        }

        [Fact]
        public void Reverse_Map_Path()
        {
            var webRootPath = $"C:{Path.DirectorySeparatorChar}MySolution{Path.DirectorySeparatorChar}MyProject";
            var subPath = $"Js{Path.DirectorySeparatorChar}Test1.js";
            var filePath = Path.Combine(webRootPath, subPath);

            var file = new Mock<IFileInfo>();
            file.Setup(x => x.Exists).Returns(true);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(Path.GetFileName(filePath));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            var urlHelper = new Mock<IUrlHelper>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger>();
            var fileProvider = new Mock<IFileProvider>();
            var fileProviderFilter = new DefaultFileProviderFilter();

            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new SmidgeFileSystem(
                fileProvider.Object,
                fileProviderFilter,
                cacheProvider.Object,
                Mock.Of<IWebsiteInfo>(),
                logger.Object);

            var result = helper.ReverseMapPath(subPath, file.Object);

            //Expected: ~/Js/Test1.js
            //Actual:   ~/Js/Test1.js/Js\Test1.js

            Assert.Equal("~/Js/Test1.js", result);
        }
    }
}
