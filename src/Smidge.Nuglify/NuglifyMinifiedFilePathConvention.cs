using System;
using System.Linq;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Nuglify
{

    /// <summary>
    /// Remove the minifiers from the pipeline if the file path matches the minified globbing pattern.
    /// </summary>
    public class NuglifyMinifiedFilePathConvention : IFileProcessingConvention
    {
        public IWebFile Apply(IWebFile file)
        {
            var pattern = file.DependencyType == WebFileType.Css ? "min.css" : "min.js";
            if (file.FilePath.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                var found = file.Pipeline.Processors.Where(x => x is NuglifyJs || x is NuglifyCss).ToList();
                if (found.Count > 0)
                {
                    // copy the pipeline 
                    file.Pipeline = file.Pipeline.Copy();

                    // now modify the pipeline
                    foreach (var preProcessor in found)
                    {
                        file.Pipeline.Processors.Remove(preProcessor);
                    }
                }
            }
            return file;
        }
    }
}
