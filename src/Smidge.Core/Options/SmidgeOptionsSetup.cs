using Microsoft.Extensions.Options;
using Smidge.CompositeFiles;
using Smidge.FileProcessors;

namespace Smidge.Options
{
    /// <summary>
    /// Used to specify the type of options that can be configured
    /// </summary>
    public sealed class SmidgeOptionsSetup : ConfigureOptions<SmidgeOptions>
    {
        public SmidgeOptionsSetup(PreProcessPipelineFactory pipelineFactory) : base(ConfigureSmidge)
        {
            PipelineFactory = pipelineFactory;
        }

        public PreProcessPipelineFactory PipelineFactory { get; }

        /// <summary>
        /// Set the default options
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>
        /// By default the Smidge options ctor's include all default settings
        /// </remarks>
        public static void ConfigureSmidge(SmidgeOptions options)
        {
            //create the default options
            options.UrlOptions = new UrlManagerOptions();
            options.CacheOptions = new SmidgeCacheOptions();
            options.FileProcessingConventions = new FileProcessingConventionsCollection { typeof(MinifiedFilePathConvention) };
            options.DefaultBundleOptions = new BundleEnvironmentOptions
            {
                DebugOptions = new BundleOptions { FileWatchOptions = new FileWatchOptions { Enabled = false }, ProcessAsCompositeFile = false, CompressResult = false },
                ProductionOptions = new BundleOptions { FileWatchOptions = new FileWatchOptions { Enabled = false } }
            };
        }

        /// <summary>
        /// Allows for configuring the options instance before options are set
        /// </summary>
        /// <param name="options"></param>
        public override void Configure(SmidgeOptions options)
        {
            options.PipelineFactory = PipelineFactory;
            base.Configure(options);
        }
    }
}
