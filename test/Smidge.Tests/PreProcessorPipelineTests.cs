using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Moq;
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
                new ProcessorHeader(), 
                new ProcessorFooter()
            });

            var result = await pipeline.ProcessAsync(new FileProcessContext("This is some content", Mock.Of<IWebFile>()));

            Assert.Equal("Header\nThis is some content\nFooter", result);
        }

        private class ProcessorHeader : IPreProcessor
        {
            public Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task<string>> next)
            {         
                var result = "Header\n" + fileProcessContext.FileContent;
                return next(result);
            }
        }

        private class ProcessorFooter : IPreProcessor
        {
            public async Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task<string>> next)
            {
                var processed = await next(fileProcessContext.FileContent);

                var result = processed + "\nFooter";
                
                await next(result);
            }
        }
    }
}