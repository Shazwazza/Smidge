using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Moq;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Microsoft.Extensions.PlatformAbstractions;
using Smidge.Models;
using Smidge.Hashing;

namespace Smidge.Tests
{

    public class FileSystemHelperTests
    {
        [Fact]
        public void Normalize_Web_Path_Virtual_Path()
        {

            var url = "~/test/hello.js";

            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());
           
            var result = urlHelper.Content(url);

            Assert.Equal("/test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Relative()
        {

            var url = "test/hello.js";

            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());

            var result = urlHelper.Content(url);

            Assert.Equal("test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Absolute()
        {

            var url = "/test/hello.js";
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());

            var result = urlHelper.Content(url);

            Assert.Equal("/test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_External_Schemaless()
        {

            var url = "//test.com/hello.js";
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());

            var result = urlHelper.Content(url);

            Assert.Equal("http://test.com/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_External_With_Schema()
        {

            var url = "http://test.com/hello.js";
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());


            var result = urlHelper.Content(url);

            Assert.Equal("http://test.com/hello.js", result);
        }
        
        
        

        [Fact]
        public void Get_File_Info_Non_Existent_File_Throws_Informative_Exception()
        {

            var webRootPath = "C:\\MySolution\\MyProject";

            var url = "~/Js/Test1.js";

            var fileProvider = new Mock<IFileProvider>();
            var file = new Mock<IFileInfo>();
            string filePath = Path.Combine(webRootPath, "Js\\Test1.js");

            file.Setup(x => x.Exists).Returns(false);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(System.IO.Path.GetFileName(url));
            file.Setup(x => x.PhysicalPath).Returns(filePath);

            fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(file.Object);

            var urlHelper = new Mock<IUrlHelper>();
            var hasher = Mock.Of<IHasher>();
            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new FileSystemHelper(
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == webRootPath && x.WebRootFileProvider == fileProvider.Object),
                Mock.Of<ISmidgeConfig>(),
                hasher);

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() => helper.GetFileInfo(url));

            //    var result = helper.MapPath(url);

            Assert.Contains(url, ex.Message);
        }

        [Fact]
        public void Reverse_Map_Path()
        {
            var webRootPath = "C:\\MySolution\\MyProject";
            var subPath = "Js\\Test1.js";
            var filePath = Path.Combine(webRootPath, subPath);

            var file = new Mock<IFileInfo>();

            var urlHelper = new Mock<IUrlHelper>();
            var hostingEnv = new Mock<IHostingEnvironment>();
            var fileProvider = new Mock<IFileProvider>();

            hostingEnv.Setup(x => x.WebRootFileProvider).Returns(fileProvider.Object);
            file.Setup(x => x.Exists).Returns(true);
            file.Setup(x => x.IsDirectory).Returns(false);
            file.Setup(x => x.Name).Returns(System.IO.Path.GetFileName(filePath));
            file.Setup(x => x.PhysicalPath).Returns(filePath);


            urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
            var helper = new FileSystemHelper(
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == webRootPath && x.WebRootFileProvider == fileProvider.Object),
                Mock.Of<ISmidgeConfig>(),
                Mock.Of<IHasher>());

            var result = helper.ReverseMapPath(subPath, file.Object);

            Assert.Equal("~/Js/Test1.js", result);
        }
    }
}