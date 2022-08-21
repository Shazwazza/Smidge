using System.Collections.Generic;
using Smidge.Cache;

namespace Smidge.Tests.Helpers
{
    public class FakeCacheBuster : ICacheBuster
    {

        public static readonly IEnumerable<ICacheBuster> Instances = new[] { new FakeCacheBuster() };


        public string GetValue() => "00000";
    }
}
