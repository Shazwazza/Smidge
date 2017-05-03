using Smidge.Cache;

namespace Smidge.Models
{
    public interface IRequestModel
    {
        ICacheBuster CacheBuster { get; }
        CompressionType Compression { get; }
        bool Debug { get; }
        string Extension { get; }
        string FileKey { get; }
        string Mime { get; }
    }
}