using System;
using Xunit;
using Smidge.CompositeFiles;
using Smidge;
using Moq;
using System.Collections.Generic;
using Smidge.Files;
using System.Linq;

namespace CoolTests
{
    public class DefaultUrlCreatorTests
    {
        [Fact]
        public void Make_Bundle_Url()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("blah");
            var creator = new DefaultUrlCreator(
                new UrlCreatorOptions { RequestHandlerPath = "sg" },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            var url = creator.GetUrl("my-bundle", WebFileType.Js);

            Assert.Equal("sg?s=my-bundle.b&t=Js&v=1", url);
        }

        [Fact]
        public void Make_Composite_Url()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());

            var creator = new DefaultUrlCreator(
                new UrlCreatorOptions { RequestHandlerPath = "sg", MaxUrlLength = 100 },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            var url = creator.GetUrls(WebFileType.Js, new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") });

            Assert.Equal(1, url.Count());
            Assert.Equal("sg?s=Test1.jsTest2.js&t=Js&v=1", url.First().Url);
            Assert.Equal("test1.jstest2.js", url.First().Key);
        }

        [Fact]
        public void Make_Composite_Url_Splits()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());

            var creator = new DefaultUrlCreator(
                new UrlCreatorOptions { RequestHandlerPath = "sg", MaxUrlLength = 14  + 10 },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            var url = creator.GetUrls(WebFileType.Js, new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") });

            Assert.Equal(2, url.Count());
            Assert.Equal("sg?s=Test1.js&t=Js&v=1", url.ElementAt(0).Url);
            Assert.Equal("test1.js", url.ElementAt(0).Key);
            Assert.Equal("sg?s=Test2.js&t=Js&v=1", url.ElementAt(1).Url);
            Assert.Equal("test2.js", url.ElementAt(1).Key);
        }

        [Fact]
        public void Throws_When_Single_Dependency_Too_Long()
        {
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());

            var creator = new DefaultUrlCreator(
                new UrlCreatorOptions { RequestHandlerPath = "sg", MaxUrlLength = 10 },
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object);

            Assert.Throws<InvalidOperationException>(() => creator.GetUrls(WebFileType.Js, new List<IWebFile> { new JavaScriptFile("Test1.js") }));

        }
    }
}
