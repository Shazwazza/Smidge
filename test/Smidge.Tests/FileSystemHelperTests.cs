﻿using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Moq;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Smidge.Hashing;
using Smidge.Cache;

namespace Smidge.Tests
{

    public class FileSystemHelperTests
    {
        [Fact]
        public void No()
        {

            var url = "~/test/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var result = urlHelper.Content(url);

            Assert.Equal("/test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Virtual_Path()
        {

            var url = "~/test/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
           
            var result = urlHelper.Content(url);

            Assert.Equal("/test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Relative()
        {

            var url = "test/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var result = urlHelper.Content(url);

            Assert.Equal("test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Absolute()
        {

            var url = "/test/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var result = urlHelper.Content(url);

            Assert.Equal("/test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_External_Schemaless()
        {

            var url = "//test.com/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);

            var result = urlHelper.Content(url);

            Assert.Equal("http://test.com/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_External_With_Schema()
        {

            var url = "http://test.com/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);


            var result = urlHelper.Content(url);

            Assert.Equal("http://test.com/hello.js", result);
        }
        
        
        

        [Fact]
        public void Get_File_Info_Non_Existent_File_Throws_Informative_Exception()
        {

            var webRootPath = "C:\\MySolution\\MyProject";

            var url = "~/Js/Test1.js";

            var cacheProvider = new Mock<ICacheFileSystem>();
            var fileProvider = new Mock<IFileProvider>();
            var file = new Mock<IFileInfo>();
            string filePath = Path.Combine(webRootPath, "Js\\Test1.js");

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

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() => helper.SourceFileProvider.GetRequiredFileInfo(url));

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

            Assert.Equal("~/Js/Test1.js", result);
        }
    }
}