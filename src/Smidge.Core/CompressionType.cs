using System;
using System.Collections.Generic;

namespace Smidge
{
    public readonly struct CompressionType : IEquatable<CompressionType>, IEquatable<string>
    {
        private readonly string _compressionType;

        private CompressionType(string compressionType) => _compressionType = compressionType;

        public static CompressionType Deflate { get; } = new("deflate");

        public static CompressionType GZip { get; } = new("gzip");

        public static CompressionType Brotli { get; } = new("br");

        public static CompressionType None { get; } = new("");

        public static IReadOnlyCollection<CompressionType> All { get; } = new[] { Brotli, GZip, Deflate, None };

        public static CompressionType Parse(string compressionType)
        {
            if (compressionType == Deflate)
                return Deflate;

            if ((compressionType == GZip) || (compressionType == "x-gzip"))
                return GZip;

            return compressionType == Brotli ? Brotli : None;
        }

        public override string ToString() => _compressionType;

        public override bool Equals(object obj) => obj is CompressionType type && Equals(type);

        public bool Equals(CompressionType other) => _compressionType.Equals(other._compressionType, StringComparison.OrdinalIgnoreCase);

        public bool Equals(string other) => _compressionType.Equals(other, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(_compressionType);

        public static bool operator ==(CompressionType left, CompressionType right) => left.Equals(right);

        public static bool operator !=(CompressionType left, CompressionType right) => !(left == right);

        public static bool operator ==(CompressionType left, string right) => left.Equals(right);

        public static bool operator !=(CompressionType left, string right) => !(left == right);

        public static bool operator ==(string left, CompressionType right) => right.Equals(left);

        public static bool operator !=(string left, CompressionType right) => !(left == right);
    }
}
