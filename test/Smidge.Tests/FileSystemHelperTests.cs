using System;
using Microsoft.AspNetCore.Hosting;

using Moq;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Smidge.Hashing;
using Smidge.Cache;
using System.Threading.Tasks;

namespace Smidge.Tests
{
    public class FileSystemHelperTests
    {
        private ISmidgeFileSystem Create(string url = "~/Js/Test1.js")
        {
            var webRootPath = $"C:{Path.DirectorySeparatorChar}MySolution{Path.DirectorySeparatorChar}MyProject";

            var cacheProvider = new Mock<ICacheFileSystem>();
            var fileProvider = new Mock<IFileProvider>();
            var file = new Mock<IFileInfo>();
            string filePath = Path.Combine(webRootPath, $"Js{Path.DirectorySeparatorChar}Test1.js");

            file.Setup(x => x.Exists).Returns(false);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(System.IO.Path.GetFileName(url));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(file.Object);

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new SmidgeFileSystem(
                fileProvider.Object,
                cacheProvider.Object);


            return helper;
        }

        [Fact]
        public void Get_File_Info_Non_Existent_File_Throws_Informative_Exception()
        {
            var url = "~/Js/Test1.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var helper = Create(url);

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() => helper.SourceFileProvider.GetRequiredFileInfo(url));

            //    var result = helper.MapPath(url);

            Assert.Contains(url, ex.Message);
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
            file.Setup(x => x.Name).Returns(System.IO.Path.GetFileName(filePath));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            var urlHelper = new Mock<IUrlHelper>();
            var cacheProvider = new Mock<ICacheFileSystem>();
            var fileProvider = new Mock<IFileProvider>();

            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new SmidgeFileSystem(
                fileProvider.Object,
                cacheProvider.Object);

            var result = helper.ReverseMapPath(subPath, file.Object);

            //Expected: ~/Js/Test1.js
            //Actual:   ~/Js/Test1.js/Js\Test1.js

            Assert.Equal("~/Js/Test1.js", result);
        }
    }
}