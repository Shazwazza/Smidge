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
        public static async Task<Stream> CompressAsync(CompressionType type, Stream original)
        {            
            using (var ms = new MemoryStream())
            {
                Stream compressedStream = null;

                if (type == CompressionType.deflate)
                {
                    compressedStream = new DeflateStream(ms, CompressionMode.Compress);
                }
                else if (type == CompressionType.gzip)
                {
                    compressedStream = new GZipStream(ms, CompressionMode.Compress);
                }

                if (type != CompressionType.none)
                {
                    using (compressedStream)
                    {
                        await original.CopyToAsync(compressedStream);
                    }

                    //NOTE: If we just try to return the ms instance, it will simply not work
                    // a new stream needs to be returned that contains the compressed bytes.
                    // I've tried every combo and this appears to be the only thing that works.
                    //byte[] output = ms.ToArray();
                    return new MemoryStream(ms.ToArray());
                }

                //not compressed
                return original;
            }
        }
    }

    
}