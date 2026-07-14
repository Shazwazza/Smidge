using System.Threading.Tasks;
using Smidge.FileProcessors;

namespace Smidge.Tests
{
    internal sealed class ProcessorHeaderAndFooter : IPreProcessor
    {
        public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            await next(fileProcessContext);

            fileProcessContext.Update("WrappedHeader\n" + fileProcessContext.FileContent + "\nWrappedFooter");
        }
    }
}
