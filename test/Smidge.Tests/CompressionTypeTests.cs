using Xunit;

namespace Smidge.Tests
{
    public class CompressionTypeTests
    {
        [Fact]
        public void Equals_String()
        {
            Assert.True(CompressionType.Brotli == "br");
            Assert.True(CompressionType.GZip == "gzip");
            Assert.True(CompressionType.Deflate == "deflate");
            Assert.True("br" == CompressionType.Brotli);
            Assert.True("gzip" == CompressionType.GZip);
            Assert.True("deflate" == CompressionType.Deflate);

            Assert.True(CompressionType.Brotli == "Br");
            Assert.True(CompressionType.GZip == "Gzip");
            Assert.True(CompressionType.Deflate == "Deflate");
            Assert.True("Br" == CompressionType.Brotli);
            Assert.True("Gzip" == CompressionType.GZip);
            Assert.True("Deflate" == CompressionType.Deflate);
        }

        [Fact]
        public void Parse()
        {
            Assert.Equal(CompressionType.Brotli, CompressionType.Parse("br"));
            Assert.Equal(CompressionType.GZip, CompressionType.Parse("gzip"));
            Assert.Equal(CompressionType.Deflate, CompressionType.Parse("deflate"));
            Assert.Equal(CompressionType.Brotli, CompressionType.Parse("Br"));
            Assert.Equal(CompressionType.GZip, CompressionType.Parse("Gzip"));
            Assert.Equal(CompressionType.Deflate, CompressionType.Parse("Deflate"));
        }
    }
}
