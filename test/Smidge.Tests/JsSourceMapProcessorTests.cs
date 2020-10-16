using System;
using Xunit;
using Smidge.FileProcessors;
using Moq;
using Smidge.Models;
using System.Threading.Tasks;
using Smidge.CompositeFiles;

namespace Smidge.Tests
{
    public class JsSourceMapProcessorTests
    {
        [Fact]
        public async Task Source_Map_Removed()
        {
            var js = @"!function(e,t){""function""==typeof define&&define.amd?define([],function();
//# sourceMappingURL=tmhDynamicLocale.min.js.map;
   Testing 123
   //# sourceMappingURL=https://hello.com/blah.min.js.map;
 asdf asdf asd fasdf
 //# sourceMappingURL=../blah.min.js.map;
 asdf asdf asd fasdf";

            var removeMaps = GetJsSourceMapProcessor();
            using (var bc = BundleContext.CreateEmpty())
            {
                var fileProcessContext = new FileProcessContext(js, new JavaScriptFile("js/test.js"), bc);
                await removeMaps.ProcessAsync(fileProcessContext, ctx => Task.FromResult(0));

                Assert.Equal(
                    @"!function(e,t){""function""==typeof define&&define.amd?define([],function();
//# sourceMappingURL=/js/tmhDynamicLocale.min.js.map;
   Testing 123
//# sourceMappingURL=https://hello.com/blah.min.js.map;
 asdf asdf asd fasdf
//# sourceMappingURL=/blah.min.js.map;
 asdf asdf asd fasdf", 
                    fileProcessContext.FileContent);
            }
        }

        private JsSourceMapProcessor GetJsSourceMapProcessor()
        {
            var websiteInfo = GetWebsiteInfo();
            var cssImportProcessor = new JsSourceMapProcessor(websiteInfo, new RequestHelper(websiteInfo));
            return cssImportProcessor;
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
