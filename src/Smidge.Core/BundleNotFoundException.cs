using System;

namespace Smidge
{
    public class BundleNotFoundException : Exception
    {
        public BundleNotFoundException(string bundleName)
        {
            BundleName = bundleName;
        }

        public string BundleName { get; set; }

        public override string Message
        {
            get { return $"A bundle with the name:{BundleName} could not be found. "; }
        }
    }
}