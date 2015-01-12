using Microsoft.Framework.OptionsModel;
using Smidge.FileProcessors;
using System;

namespace Smidge.Options
{
    public class BundlesSetup : ConfigureOptions<Bundles>
    {
        public BundlesSetup(PreProcessPipelineFactory pipelineFactory) : base(ConfigureBundles)
        {
            PipelineFactory = pipelineFactory;
        }

        public PreProcessPipelineFactory PipelineFactory { get; private set; }

        /// <summary>
        /// Set the default bundle options
        /// </summary>
        /// <param name="options"></param>
        public static void ConfigureBundles(Bundles options)
        {
        }

        /// <summary>
        /// Allows for configuring the options instance before options are set
        /// </summary>
        /// <param name="options"></param>
        /// <param name="name"></param>
        public override void Configure(Bundles options, string name = "")
        {
            options.PipelineFactory = PipelineFactory;

            base.Configure(options, name);
        }
    }
}