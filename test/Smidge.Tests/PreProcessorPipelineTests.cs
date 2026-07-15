using System.Threading.Tasks;
using Moq;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using Smidge.Models;
using Xunit;

namespace Smidge.Tests
{
    public class PreProcessorPipelineTests
    {
        [Fact]
        public async Task Can_Process_Pipeline()
        {
            var pipeline = new PreProcessPipeline(new IPreProcessor[]
            {
                new ProcessorHeaderAndFooter(),
                new ProcessorHeader(),
                new ProcessorFooter()
            });
            using (var bc = BundleContext.CreateEmpty("1"))
            {
                var result = await pipeline.ProcessAsync(new FileProcessContext("This is some content", Mock.Of<IWebFile>(), bc));

                Assert.Equal("WrappedHeader\nHeader\nThis is some content\nFooter\nWrappedFooter", result);
            }
        }
    }
}
