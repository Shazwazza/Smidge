using System;
using System.Collections.Generic;
using System.IO;

namespace Fuze.CompositeFiles
{
    /// <summary>
	/// Deserialized structure of the XML stored in the map file
	/// </summary>
	public class CompositeFileMap
    {

        public CompositeFileMap(string key, string compressionType, string file, IEnumerable<string> filePaths, int version)
        {
            DependentFiles = filePaths;
            FileKey = key;
            CompositeFileName = file;
            CompressionType = compressionType;
            Version = version;
        }

        public string FileKey { get; private set; }
        public string CompositeFileName { get; private set; }
        public string CompressionType { get; private set; }
        public int Version { get; private set; }
        public IEnumerable<string> DependentFiles { get; private set; }

        private byte[] _fileBytes;

        /// <summary>
        /// If for some reason the file doesn't exist any more or we cannot read the file, this will return false.
        /// </summary>
        public bool HasFileBytes
        {
            get
            {
                GetCompositeFileBytes();
                return _fileBytes != null;
            }
        }

        /// <summary>
        /// Returns the file's bytes
        /// </summary>
        public byte[] GetCompositeFileBytes()
        {
            if (_fileBytes == null)
            {
                if (string.IsNullOrEmpty(CompositeFileName))
                {
                    return null;
                }

                try
                {
                    FileInfo fi = new FileInfo(CompositeFileName);
                    using (FileStream fs = fi.OpenRead())
                    {
                        byte[] fileBytes = new byte[fs.Length];
                        fs.Read(fileBytes, 0, fileBytes.Length);
                        _fileBytes = fileBytes;
                    }                        
                }
                catch
                {
                    _fileBytes = null;
                }
            }
            return _fileBytes;
        }



    }
}