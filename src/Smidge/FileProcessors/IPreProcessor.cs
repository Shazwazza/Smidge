using System;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    public interface IPreProcessor
    {
        Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task> next);
    }
}