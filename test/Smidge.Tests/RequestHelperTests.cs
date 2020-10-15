using System;

using Moq;
using Xunit;

namespace Smidge.Tests
{
    public class RequestHelperTests
    {
        [Fact]
        public void No()
        {

            var url = "~/test/hello.js";

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
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
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
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
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
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
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
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
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
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
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));

            var urlHelper = new RequestHelper(websiteInfo.Object);


            var result = urlHelper.Content(url);

            Assert.Equal("http://test.com/hello.js", result);
        }

        
    }
}