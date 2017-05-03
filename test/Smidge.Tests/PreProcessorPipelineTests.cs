using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Moq;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using Smidge.Models;
using Xunit;
using Xunit.Abstractions;

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
            using (var bc = new BundleContext())
            {
                var result = await pipeline.ProcessAsync(new FileProcessContext("This is some content", Mock.Of<IWebFile>(), bc));

                Assert.Equal("WrappedHeader\nHeader\nThis is some content\nFooter\nWrappedFooter", result);
            }
            
        }

        private class ProcessorHeaderAndFooter : IPreProcessor
        {
            public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
            {   
                await next(fileProcessContext);

                fileProcessContext.Update("WrappedHeader\n" + fileProcessContext.FileContent + "\nWrappedFooter");
            }
        }

        private class ProcessorHeader : IPreProcessor
        {
            public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
            {                
                await next(fileProcessContext);
                fileProcessContext.Update("Header\n" + fileProcessContext.FileContent);
            }
        }

        private class ProcessorFooter : IPreProcessor
        {
            public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
            {
                await next(fileProcessContext);
                fileProcessContext.Update(fileProcessContext.FileContent + "\nFooter");                
            }
        }

        
    }
}