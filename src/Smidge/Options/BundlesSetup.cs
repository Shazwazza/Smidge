using Microsoft.Framework.OptionsModel;
using System;

namespace Smidge.Options
{
    public class BundlesSetup : ConfigureOptions<Bundles>
    {
        public BundlesSetup() : base(ConfigureBundles)
        {

        }

        public static void ConfigureBundles(Bundles options)
        {
        }
    }
}