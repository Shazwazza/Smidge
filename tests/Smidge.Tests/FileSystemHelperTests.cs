using Microsoft.AspNet.Hosting;
using Microsoft.Framework.Runtime;
using Moq;
using System;
using Xunit;

namespace Smidge.Tests
{
    public class FileSystemHelperTests
    {
        [Fact]
        public void Map_Path_Absolute()
        {
            var url = "/Js/Test1.js";

            var helper = new FileSystemHelper(
                Mock.Of<IApplicationEnvironment>(),
                Mock.Of<IHostingEnvironment>(x => x.WebRoot == "C:\\MySolution\\MyProject"),
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
                Mock.Of<IHostingEnvironment>(x => x.WebRoot == "C:\\MySolution\\MyProject"),
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
                Mock.Of<IHostingEnvironment>(x => x.WebRoot == "C:\\MySolution\\MyProject"),
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
                Mock.Of<IHostingEnvironment>(x => x.WebRoot == "C:\\MySolution\\MyProject"),
                Mock.Of<ISmidgeConfig>());

            var result = helper.ReverseMapPath(url);

            Assert.Equal("~/Js/Test1.js", result);
        }
    }
}