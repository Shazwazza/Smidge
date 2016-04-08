using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smidge.Models;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Remove the minifiers from the pipeline if the file path matches the minified globbing pattern.
    /// </summary>
    public class MinifiedFilePathConvention : IFileProcessingConvention
    {
        public IWebFile Apply(IWebFile file)
        {
            var pattern = file.DependencyType == WebFileType.Css ? "min.css" : "min.js";
            if (file.FilePath.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                var found = file.Pipeline.Processors.Where(x => x is JsMin || x is CssMinifier).ToArray();
                foreach (var preProcessor in found)
                {
                    file.Pipeline.Processors.Remove(preProcessor);
                }
            }
            return file;
        }
    }
}
