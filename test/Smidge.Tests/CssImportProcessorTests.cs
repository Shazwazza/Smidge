using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
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

            var cssImportProcessor = GetCssImportProcessor();
            var output = cssImportProcessor.ParseImportStatements(cssWithImport, out _, out _);

            Assert.Equal(output, cssWithImport);
        }

        [Fact]
        public void Can_Parse_Import_Statements()
        {
            var css = @"@import url('/css/typography.css');
@import '/css/layout.css' ;
/*@import ""bootstrap/variables"";*/
@import url('http://mysite/css/color.css');
 @import url(/css/blah.css);
@import ""css/blah2.css"";
@import ""https://mysite.com/css/blah2.css"";

body { color: black; }
div {display: block;}";

            var cssImportProcessor = GetCssImportProcessor();

            var output = cssImportProcessor.ParseImportStatements(css, out IEnumerable<string> importPaths, out _);

            Assert.Equal(@"/*@import ""bootstrap/variables"";*/
@import url('http://mysite/css/color.css');
body { color: black; }
div {display: block;}".Replace("\r\n", string.Empty).Replace("\n", string.Empty), output.Replace("\r\n", string.Empty).Replace("\n", string.Empty));

            Assert.Equal(4, importPaths.Count());
            Assert.Equal("/css/typography.css", importPaths.ElementAt(0));
            Assert.Equal("/css/layout.css", importPaths.ElementAt(1));
            //Assert.AreEqual("http://mysite/css/color.css", importPaths.ElementAt(2));
            Assert.Equal("/css/blah.css", importPaths.ElementAt(2));
            Assert.Equal("css/blah2.css", importPaths.ElementAt(3));
        }

        private CssImportProcessor GetCssImportProcessor()
        {
            var websiteInfo = GetWebsiteInfo();
            var cssImportProcessor = new CssImportProcessor(GetFileSystem(), websiteInfo, new RequestHelper(websiteInfo));
            return cssImportProcessor;
        }

        private ISmidgeFileSystem GetFileSystem()
        {
            var fileSystem = new SmidgeFileSystem(
                Mock.Of<IFileProvider>(),
                Mock.Of<IFileProviderFilter>(),
                Mock.Of<ICacheFileSystem>(),
                Mock.Of<IWebsiteInfo>(),
                Mock.Of<ILogger>());
            return fileSystem;
        }

        private IWebsiteInfo GetWebsiteInfo()
        {
            var websiteInfo = new Mock<IWebsiteInfo>();
            websiteInfo.Setup(x => x.GetBasePath()).Returns(string.Empty);
            websiteInfo.Setup(x => x.GetBaseUrl()).Returns(new Uri("http://test.com"));
            return websiteInfo.Object;
        }
    }
}
