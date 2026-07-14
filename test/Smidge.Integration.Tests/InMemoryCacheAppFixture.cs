namespace Smidge.Integration.Tests
{
    public sealed class InMemoryCacheAppFixture : SmidgeAppFixture
    {
        public InMemoryCacheAppFixture() : base(inMemory: true) { }
    }
}
