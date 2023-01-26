namespace Smidge.Models
{

    public sealed class WebFilePair
    {
        public WebFilePair(IWebFile original, IWebFile hashed)
        {
            Original = original;
            Hashed = hashed;
        }
        public IWebFile Original { get; }
        public IWebFile Hashed { get; }

        /// <summary>
        /// Determines if they are equal based on the original file path
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is WebFilePair pair && WebFilePairEqualityComparer.Instance.Equals(Original, pair.Original);
        }

        /// <summary>
        /// Returns the hash code of the original file path
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = -1429085014;
            return hashCode * -1521134295 + WebFilePairEqualityComparer.Instance.GetHashCode(Original);
        }

       
    }
}