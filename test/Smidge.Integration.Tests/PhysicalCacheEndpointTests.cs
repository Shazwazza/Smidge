using Xunit;

namespace Smidge.Integration.Tests
{
    public sealed class PhysicalCacheEndpointTests : SmidgeEndpointTestsBase, IClassFixture<PhysicalCacheAppFixture>
    {
        public PhysicalCacheEndpointTests(PhysicalCacheAppFixture fixture) : base(fixture) { }
    }
}
