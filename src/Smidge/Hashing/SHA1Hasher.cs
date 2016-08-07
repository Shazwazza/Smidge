using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Smidge.Hashing
{
    /// <summary>
    /// A Hasher that uses sha1 hashgin
    /// </summary>
    public class SHA1Hasher : IHasher
    {
        public string Hash(string input)
        {
            using (var sha = SHA1.Create())
            {
                var byteArray = sha.ComputeHash(Encoding.Unicode.GetBytes(input));
                return byteArray.Aggregate("", (current, b) => current + b.ToString("x2"));
            }
        }
    }
}