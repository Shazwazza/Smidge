using System;

namespace Smidge.CompositeFiles
{
    public class UrlManagerOptions
    {
        public UrlManagerOptions()
        {
            //defaults
            MaxUrlLength = 2048;
            CompositeFilePath = "sc";
            BundleFilePath = "sb";

            UrlPattern = "~/{Path}/{Name}.{Ext}.v{Version}";
        }

        public int MaxUrlLength { get; set; }
        public string CompositeFilePath { get; set; }
        public string BundleFilePath { get; set; }

        public string UrlPattern { get; set; }
    }
}