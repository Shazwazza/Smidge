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
            Processors = new List<IPreProcessor>(processors);
        }
        
        public List<IPreProcessor> Processors { get; }

        public async Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            var queue = new Queue<IPreProcessor>(Processors);
            while (queue.Count > 0)
            {
                var executed = await ProcessNext(queue, fileProcessContext);
                //The next item wasn't executed which means the processor terminated
                // the pipeline.
                if (!executed) break;
            }

            //return output.Result;
            return fileProcessContext.FileContent;
        }

        /// <summary>
        /// Recursively process the next pre-processor until the queue is completed or until the pipeline is terminated
        /// </summary>        
        /// <param name="queue"></param>
        /// <param name="fileProcessContext"></param>
        /// <returns></returns>
        private static async Task<bool> ProcessNext(Queue<IPreProcessor> queue, FileProcessContext fileProcessContext)
        {
            //Check if there are no more, if not then we're all done
            if (queue.Count == 0)
                return true;

            var p = queue.Dequeue();
            var executed = false;

            await p.ProcessAsync(fileProcessContext, async ctx =>
            {
                executed = await ProcessNext(queue, ctx);                
            });
            
            //The next item wasn't executed which means the processor terminated the pipeline.
            return executed;
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