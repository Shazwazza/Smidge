using System;
using System.Collections.Generic;
using Smidge.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// The pre-processor pipeline defined for a file(s)
    /// </summary>
    public class PreProcessPipeline 
    {
        public PreProcessPipeline(IEnumerable<IPreProcessor> processors)
        {
            Processors = new List<IPreProcessor>();
            Processors.AddRange(processors);
        }
        
        public List<IPreProcessor> Processors { get; private set; }

        public async Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            foreach (var p in Processors)
            {
                fileProcessContext.FileContent = await p.ProcessAsync(fileProcessContext);
            }

            return fileProcessContext.FileContent;
        }

        /// <summary>
        /// Copies the current pipeline
        /// </summary>
        /// <returns></returns>
        public PreProcessPipeline Copy()
        {
            return new PreProcessPipeline(new List<IPreProcessor>(Processors));
        }
    }

    
}