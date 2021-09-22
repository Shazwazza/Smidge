using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    public delegate Task PreProcessorDelegate(FileProcessContext context);
}