using System;
using Xunit;
using Smidge.CompositeFiles;
using Smidge;
using Moq;
using System.Collections.Generic;
using Smidge.Models;
using System.Linq;

namespace Smidge.Tests
{
    public class DefaultUrlManagerTests
    {
        [Fact]
        public void Parse_Path()
        {
            var path = "c61531b5.2512be3b.bb1214f7.a21bd1fd.js.v1";
            var manager = new DefaultUrlManager(
                new UrlManagerOptions { CompositeFilePath = "sg" },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                Mock.Of<IHasher>());

            var result = manager.ParsePath(path);

            Assert.Equal("1", result.Version);
            Assert.Equal(4, result.Names.Count());
            Assert.Equal(WebFileType.Js, result.WebType);
        }

        [Fact]
        public void Make_Bundle_Url()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("blah");
            var creator = new DefaultUrlManager(
                new UrlManagerOptions { BundleFilePath = "sg" },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            var url = creator.GetUrl("my-bundle", ".js");

            Assert.Equal("sg/my-bundle.js.v1", url);
        }

        [Fact]
        public void Make_Composite_Url()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());

            var creator = new DefaultUrlManager(
                new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 100 },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            var url = creator.GetUrls(new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js");

            Assert.Equal(1, url.Count());
            Assert.Equal("sg/Test1.Test2.js.v1", url.First().Url);
            Assert.Equal("test1.test2", url.First().Key);
        }

        [Fact]
        public void Make_Composite_Url_Splits()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());

            var creator = new DefaultUrlManager(
                new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 14  + 10 },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            var url = creator.GetUrls(new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js");

            Assert.Equal(2, url.Count());
            Assert.Equal("sg/Test1.js.v1", url.ElementAt(0).Url);
            Assert.Equal("test1", url.ElementAt(0).Key);
            Assert.Equal("sg/Test2.js.v1", url.ElementAt(1).Url);
            Assert.Equal("test2", url.ElementAt(1).Key);
        }

        [Fact]
        public void Throws_When_Single_Dependency_Too_Long()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());

            var creator = new DefaultUrlManager(
                new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 10 },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            Assert.Throws<InvalidOperationException>(() => creator.GetUrls(new List<IWebFile> { new JavaScriptFile("Test1.js") }, ".js"));

        }
    }
}
