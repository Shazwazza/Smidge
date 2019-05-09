using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Moq;
using Smidge.Cache;
using Smidge.FileProcessors;
using Smidge.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Smidge.Tests
{
    public class CssImportProcessorTests
    {
        [Fact]
        public void Retain_External_Imports()
        {
            var cssWithImport = @"@import url(""//fonts.googleapis.com/css?subset=latin,cyrillic-ext,latin-ext,cyrillic&family=Open+Sans+Condensed:300|Open+Sans:400,600,400italic,600italic|Merriweather:400,300,300italic,400italic,700,700italic|Roboto+Slab:400,300"");
@import url(""//netdna.bootstrapcdn.com/font-awesome/4.0.3/css/font-awesome.css"");";
                        
            IEnumerable<string> importPaths;
            var fileSystemHelper = new SmidgeFileSystem(Mock.Of<IFileProvider>(), Mock.Of<ICacheFileSystem>());

            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));
            var cssImportProcessor = new CssImportProcessor(fileSystemHelper, websiteInfo.Object, new RequestHelper(websiteInfo.Object));
            var output = cssImportProcessor.ParseImportStatements(cssWithImport, out importPaths);

            Assert.Equal(output, cssWithImport);
        }

        [Fact]
        public void Can_Parse_Import_Statements()
        {
            var css = @"@import url('/css/typography.css');
@import '/css/layout.css';
@import url('http://mysite/css/color.css');
@import url(/css/blah.css);
@import ""css/blah2.css"";
@import ""https://mysite.com/css/blah2.css"";

body { color: black; }
div {display: block;}";

            IEnumerable<string> importPaths;
            var fileSystemHelper = new SmidgeFileSystem(Mock.Of<IFileProvider>(), Mock.Of<ICacheFileSystem>());
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns("/");
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));
            var cssImportProcessor = new CssImportProcessor(fileSystemHelper, websiteInfo.Object, new RequestHelper(websiteInfo.Object));
            var output = cssImportProcessor.ParseImportStatements(css, out importPaths);

            Assert.Equal(@"@import url('http://mysite/css/color.css');
body { color: black; }
div {display: block;}".Replace("\r\n", string.Empty).Replace("\n", string.Empty), output.Replace("\r\n", string.Empty).Replace("\n", string.Empty));

            Assert.Equal(4, importPaths.Count());
            Assert.Equal("/css/typography.css", importPaths.ElementAt(0));
            Assert.Equal("/css/layout.css", importPaths.ElementAt(1));
            //Assert.AreEqual("http://mysite/css/color.css", importPaths.ElementAt(2));
            Assert.Equal("/css/blah.css", importPaths.ElementAt(2));
            Assert.Equal("css/blah2.css", importPaths.ElementAt(3));
        }
    }
}