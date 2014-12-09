using Microsoft.Framework.OptionsModel;
using System;

namespace Smidge.Options
{
    /// <summary>
    /// Used to specify the type of options that can be configured
    /// </summary>
    /// <remarks>
    /// This whole options thing is just strange... but i guess that is how they are doing it. By creating this class and adding it to DI,
    /// it means that developers can use the Configure{SmidgeOptions} extension method on start.
    /// </remarks>
    public sealed class SmidgeOptionsSetup : ConfigureOptions<SmidgeOptions>
    {
        public SmidgeOptionsSetup() : base(ConfigureSmidge)
        {

        }

        public static void ConfigureSmidge(SmidgeOptions options)
        {
            //By default the Smidge options ctor's include all default settings
        }
    }
}