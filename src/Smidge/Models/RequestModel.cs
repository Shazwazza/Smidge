using System;

namespace Smidge.Models
{
    /// <summary>
    /// Base class model for an inbound request
    /// </summary>
    public abstract class RequestModel
    {
        public CompressionType Compression { get; set; }
        public WebFileType FileType { get; set; }
        public string Extension { get; set; }
        public string Mime { get; set; }
    }
}