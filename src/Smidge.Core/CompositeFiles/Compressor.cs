using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Smidge.CompositeFiles
{
    /// <summary>
    /// Performs byte compression
    /// </summary>
    public static class Compressor
    {
        public static async Task<Stream> CompressAsync(CompressionType type, Stream original) => await CompressAsync(type, CompressionLevel.Optimal, original);

        public static async Task<Stream> CompressAsync(CompressionType type, CompressionLevel level, Stream original)
        {            
            using (var ms = new MemoryStream())
            {
                Stream compressedStream = null;

                if (type == CompressionType.Deflate)
                {
                    compressedStream = new DeflateStream(ms, level);
                }
                else if (type == CompressionType.GZip)
                {
                    compressedStream = new GZipStream(ms, level);
                }
                else if (type == CompressionType.Brotli)
                {
                    compressedStream = new BrotliStream(ms, level);
                }

                if (type != CompressionType.None)
                {
                    using (compressedStream)
                    {
                        await original.CopyToAsync(compressedStream);
                    }
                }
                else
                {
                    await original.CopyToAsync(ms);
                }

                //NOTE: If we just try to return the ms instance, it will simply not work
                // a new stream needs to be returned that contains the compressed bytes.
                // I've tried every combo and this appears to be the only thing that works.
                //byte[] output = ms.ToArray();
                return new MemoryStream(ms.ToArray());
            }
        }
    }
}
