namespace Smidge.Integration.Tests
{
    public sealed class PhysicalCacheAppFixture : SmidgeAppFixture
    {
        public PhysicalCacheAppFixture() : base(inMemory: false) { }
    }
}
