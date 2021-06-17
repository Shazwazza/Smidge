using System.Linq;
using System.Text;

namespace Smidge.Hashing
{
    /// <summary>
    /// A Hasher that uses crc32 hashing
    /// </summary>
    public class Crc32Hasher : IHasher
    {
        public string Hash(string input)
        {
            using (var crc = new Crc32())
            {
                var byteArray = crc.ComputeHash(Encoding.Unicode.GetBytes(input));
                return byteArray.Aggregate("", (current, b) => current + b.ToString("x2"));
            }
        }
    }
}