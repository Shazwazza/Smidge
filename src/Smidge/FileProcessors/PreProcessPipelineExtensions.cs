using Smidge.Models;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Extension methods for working with the pipeline
    /// </summary>
    public static class PreProcessPipelineExtensions
    {
        public static PreProcessPipeline Create<T1>(this PreProcessPipelineFactory pipelineFactory)
            where T1 : IPreProcessor
        {
            return pipelineFactory.Create(typeof(T1));
        }

        public static PreProcessPipeline Create<T1, T2>(this PreProcessPipelineFactory pipelineFactory)
            where T1 : IPreProcessor
            where T2 : IPreProcessor
        {
            return pipelineFactory.Create(typeof(T1), typeof(T2));
        }

        public static PreProcessPipeline Create<T1, T2, T3>(this PreProcessPipelineFactory pipelineFactory)
            where T1 : IPreProcessor
            where T2 : IPreProcessor
            where T3 : IPreProcessor
        {
            return pipelineFactory.Create(typeof(T1), typeof(T2), typeof(T3));
        }

        public static PreProcessPipeline Create<T1, T2, T3, T4>(this PreProcessPipelineFactory pipelineFactory)
            where T1 : IPreProcessor
            where T2 : IPreProcessor
            where T3 : IPreProcessor
            where T4 : IPreProcessor
        {
            return pipelineFactory.Create(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        }

        public static PreProcessPipeline Create<T1, T2, T3, T4, T5>(this PreProcessPipelineFactory pipelineFactory)
            where T1 : IPreProcessor
            where T2 : IPreProcessor
            where T3 : IPreProcessor
            where T4 : IPreProcessor
            where T5 : IPreProcessor
        {
            return pipelineFactory.Create(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        }

        public static PreProcessPipeline DefaultJs(this PreProcessPipelineFactory pipelineFactory)
        {
            return pipelineFactory.CreateDefault(WebFileType.Js);
        }

        public static PreProcessPipeline DefaultCss(this PreProcessPipelineFactory pipelineFactory)
        {
            return pipelineFactory.CreateDefault(WebFileType.Css);
        }        

        /// <summary>
        /// Resolves an instance of a pre processor based on type
        /// </summary>
        /// <returns></returns>
        public static IPreProcessor Resolve<T>(this PreProcessPipelineFactory pipelineFactory)
            where T : IPreProcessor
        {
            return pipelineFactory.Resolve(typeof(T));
        }

        /// <summary>
        /// Replaces a pre processor type with another at the same index
        /// </summary>
        /// <typeparam name="TRemove"></typeparam>
        /// <typeparam name="TAdd"></typeparam>
        /// <param name="pipeline"></param>
        /// <param name="pipelineFactory"></param>
        /// <returns></returns>
        public static PreProcessPipeline Replace<TRemove, TAdd>(this PreProcessPipeline pipeline, PreProcessPipelineFactory pipelineFactory)
            where TRemove : IPreProcessor
            where TAdd : IPreProcessor
        {
            for (int i = 0; i < pipeline.Processors.Count; i++)
            {
                if (pipeline.Processors[i].GetType() == typeof(TRemove))
                {
                    pipeline.Processors.RemoveAt(i);
                    pipeline.Processors.Insert(i, pipelineFactory.Resolve<TAdd>());
                }
            }
            return pipeline;
        }
    }
}