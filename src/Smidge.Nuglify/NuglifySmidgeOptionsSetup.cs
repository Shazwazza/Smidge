using Microsoft.Extensions.Options;
using Smidge.Options;

namespace Smidge.Nuglify
{
    /// <summary>
    /// Used to specify the type of options that can be configured
    /// </summary>
    public sealed class NuglifySmidgeOptionsSetup : ConfigureOptions<SmidgeOptions>
    {
        public NuglifySmidgeOptionsSetup() : base(ConfigureSmidge)
        {
        }

        public static void ConfigureSmidge(SmidgeOptions options)
        {
            var conventions = options.FileProcessingConventions ?? new FileProcessingConventionsCollection();
            conventions.Add(typeof(NuglifyMinifiedFilePathConvention));
            options.FileProcessingConventions = conventions;
        }
    }
}
