using System.Collections.Generic;
using Smidge.Models;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Used to apply global conventions to the pre-processor pipeline for a given file
    /// </summary>
    /// <remarks>
    /// This could take the form of adding or removing items from the files pipeline or excluding
    /// the file alltogether by returning null
    /// </remarks>
    public interface IFileProcessingConvention
    {
        IWebFile Apply(IWebFile file);
    }
}