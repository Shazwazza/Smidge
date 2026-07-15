using Xunit;

namespace Smidge.Integration.Tests
{
    public sealed class InMemoryCacheEndpointTests : SmidgeEndpointTestsBase, IClassFixture<InMemoryCacheAppFixture>
    {
        public InMemoryCacheEndpointTests(InMemoryCacheAppFixture fixture) : base(fixture) { }
    }
}
