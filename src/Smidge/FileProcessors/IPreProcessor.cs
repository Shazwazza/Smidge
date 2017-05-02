using System;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    public interface IPreProcessor
    {
        Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next);
    }
}