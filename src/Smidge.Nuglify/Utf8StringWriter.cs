using System.IO;
using System.Text;

namespace Smidge.Nuglify
{
    internal class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter(StringBuilder builder)
            : base(builder)
        {
        }

        public override Encoding Encoding { get; } = new UTF8Encoding(false);
    }
}