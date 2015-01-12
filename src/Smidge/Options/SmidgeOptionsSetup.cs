using Microsoft.Framework.OptionsModel;
using System;

namespace Smidge.Options
{
    /// <summary>
    /// Used to specify the type of options that can be configured
    /// </summary>
    public sealed class SmidgeOptionsSetup : ConfigureOptions<SmidgeOptions>
    {
        public SmidgeOptionsSetup() : base(ConfigureSmidge)
        {

        }

        /// <summary>
        /// Set the default options
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>
        /// By default the Smidge options ctor's include all default settings
        /// </remarks>
        public static void ConfigureSmidge(SmidgeOptions options)
        {
            
        }

        /// <summary>
        /// Allows for configuring the options instance before options are set
        /// </summary>
        /// <param name="options"></param>
        /// <param name="name"></param>
        public override void Configure(SmidgeOptions options, string name = "")
        {
            base.Configure(options, name);
        }
    }
}