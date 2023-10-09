using System;
using Xunit;
using Smidge.CompositeFiles;
using Smidge;
using Moq;
using System.Collections.Generic;
using Smidge.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.Options;
using Smidge.Hashing;

namespace Smidge.Tests
{
    public class DefaultUrlManagerTests
    {
        [Fact]
        public void Parse_Path()
        {
            var path = "c61531b5.2512be3b.bb1214f7.a21bd1fd.js.v1";
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg" } };
            var manager = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),                
                Mock.Of<IHasher>(),
                Mock.Of<IRequestHelper>(),
                Mock.Of<ISmidgeConfig>());

            var result = manager.ParsePath(path);

            Assert.Equal("1", result.CacheBusterValue);
            Assert.Equal(4, result.Names.Count());
            Assert.Equal(WebFileType.Js, result.WebType);
        }

        [Fact]
        public void Make_Bundle_Url()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("blah");
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { BundleFilePath = "sg" } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                hasher.Object,
                urlHelper,
                Mock.Of<ISmidgeConfig>());

            var url = creator.GetUrl("my-bundle", ".js", false, "1");

            Assert.Equal("/sg/my-bundle.js.v1", url);
        }

        [Fact]
        public void Make_Bundle_Url_Keep_File_Extensions()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("blah");
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { BundleFilePath = "sg" } };
            var config = new Mock<ISmidgeConfig>();
            config.Setup(m => m.KeepFileExtensions).Returns(true);
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                hasher.Object,
                urlHelper,
                config.Object);

            var url = creator.GetUrl("my-bundle", ".js", false, "1");

            Assert.Equal("/sg/my-bundle.v1.js", url);
        }


        [Fact]
        public void Make_Composite_Url()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 100 } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                hasher.Object,
                urlHelper,
                Mock.Of<ISmidgeConfig>());

            var url = creator.GetUrls(
                new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js", "1");

            Assert.Single(url);
            Assert.Equal("/sg/Test1.Test2.js.v1", url.First().Url);
            Assert.Equal("test1.test2", url.First().Key);
        }

        [Fact]
        public void Make_Composite_Url_Keep_File_Extensions()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 100 } };
            var config = new Mock<ISmidgeConfig>();
            config.Setup(m => m.KeepFileExtensions).Returns(true);
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                hasher.Object,
                urlHelper,
                config.Object);

            var url = creator.GetUrls(
                new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js", "1");

            Assert.Single(url);
            Assert.Equal("/sg/Test1.Test2.v1.js", url.First().Url);
            Assert.Equal("test1.test2", url.First().Key);
        }

        [Fact]
        public void Make_Composite_Url_Splits()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 14 + 10 } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                hasher.Object,
                urlHelper,
                Mock.Of<ISmidgeConfig>());

            var url = creator.GetUrls(
                new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js", "1");

            Assert.Equal(2, url.Count());
            Assert.Equal("/sg/Test1.js.v1", url.ElementAt(0).Url);
            Assert.Equal("test1", url.ElementAt(0).Key);
            Assert.Equal("/sg/Test2.js.v1", url.ElementAt(1).Url);
            Assert.Equal("test2", url.ElementAt(1).Key);
        }

        [Fact]
        public void Throws_When_Single_Dependency_Too_Long()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 10 } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                hasher.Object,
                urlHelper,
                Mock.Of<ISmidgeConfig>());

            Assert.Throws<InvalidOperationException>(() => creator.GetUrls(
                new List<IWebFile> { new JavaScriptFile("Test1.js") }, ".js", "1"));

        }
    }
}
