using System.Threading.Tasks;
using Smidge.FileProcessors;

namespace Smidge.Tests
{
    internal sealed class ProcessorHeader : IPreProcessor
    {
        public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            await next(fileProcessContext);
            fileProcessContext.Update("Header\n" + fileProcessContext.FileContent);
        }
    }
}
