using System;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    public interface IPreProcessor
    {
        Task<string> ProcessAsync(FileProcessContext fileProcessContext);
    }
}