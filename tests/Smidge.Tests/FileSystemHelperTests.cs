using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;

using Moq;
using System;
using Xunit;
using Microsoft.Extensions.PlatformAbstractions;

namespace Smidge.Tests
{
    public class FileSystemHelperTests
    {
        [Fact]
        public void Normalize_Web_Path_Virtual_Path()
        {
            var url = "~/test/hello.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.NormalizeWebPath(url, Mock.Of<HttpRequest>(x => x.Scheme == "http"));

            Assert.Equal("test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Relative()
        {
            var url = "test/hello.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.NormalizeWebPath(url, Mock.Of<HttpRequest>(x => x.Scheme == "http"));

            Assert.Equal("test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_Absolute()
        {
            var url = "/test/hello.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.NormalizeWebPath(url, Mock.Of<HttpRequest>(x => x.Scheme == "http"));

            Assert.Equal("test/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_External_Schemaless()
        {
            var url = "//test.com/hello.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.NormalizeWebPath(url, Mock.Of<HttpRequest>(x => x.Scheme == "http"));

            Assert.Equal("http://test.com/hello.js", result);
        }

        [Fact]
        public void Normalize_Web_Path_External_With_Schema()
        {
            var url = "http://test.com/hello.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.NormalizeWebPath(url, Mock.Of<HttpRequest>(x => x.Scheme == "http"));

            Assert.Equal("http://test.com/hello.js", result);
        }

        [Fact]
        public void Map_Path_Absolute()
        {
            var url = "/Js/Test1.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.MapPath(url);

            Assert.Equal("C:\\MySolution\\MyProject\\Js\\Test1.js", result);
        }

        [Fact]
        public void Map_Path_Virtual_Path()
        {
            var url = "~/Js/Test1.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.MapPath(url);

            Assert.Equal("C:\\MySolution\\MyProject\\Js\\Test1.js", result);
        }

        [Fact]
        public void Map_Path_Relative_Path()
        {
            var url = "Js/Test1.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.MapPath(url);

            Assert.Equal("C:\\MySolution\\MyProject\\Js\\Test1.js", result);
        }

        [Fact]
        public void Reverse_Map_Path()
        {
            var url = "C:\\MySolution\\MyProject\\Js\\Test1.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRootPath == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.ReverseMapPath(url);

            Assert.Equal("~/Js/Test1.js", result);
        }
    }
}