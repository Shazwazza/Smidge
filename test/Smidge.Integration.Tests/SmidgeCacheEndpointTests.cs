using System.Threading.Tasks;
using Xunit;

namespace Smidge.Integration.Tests
{
    /// <summary>
    /// Starts a single <see cref="SmidgeTestApp"/> shared by all tests in a class.
    /// </summary>
    public abstract class SmidgeAppFixture : IAsyncLifetime
    {
        private readonly bool _inMemory;

        protected SmidgeAppFixture(bool inMemory) => _inMemory = inMemory;

        public SmidgeTestApp App { get; private set; } = null!;

        public async Task InitializeAsync() => App = await SmidgeTestApp.StartAsync(_inMemory);

        public async Task DisposeAsync() => await App.DisposeAsync();
    }

    public sealed class InMemoryCacheAppFixture : SmidgeAppFixture
    {
        public InMemoryCacheAppFixture() : base(inMemory: true) { }
    }

    public sealed class PhysicalCacheAppFixture : SmidgeAppFixture
    {
        public PhysicalCacheAppFixture() : base(inMemory: false) { }
    }

    public sealed class InMemoryCacheEndpointTests : SmidgeEndpointTestsBase, IClassFixture<InMemoryCacheAppFixture>
    {
        public InMemoryCacheEndpointTests(InMemoryCacheAppFixture fixture) : base(fixture) { }
    }

    public sealed class PhysicalCacheEndpointTests : SmidgeEndpointTestsBase, IClassFixture<PhysicalCacheAppFixture>
    {
        public PhysicalCacheEndpointTests(PhysicalCacheAppFixture fixture) : base(fixture) { }
    }
}
