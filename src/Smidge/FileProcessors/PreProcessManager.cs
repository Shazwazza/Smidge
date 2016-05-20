using Smidge.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Smidge.FileProcessors;

namespace Smidge
{
    public sealed class PreProcessManager
    {
        private FileSystemHelper _fileSystemHelper;
        private IHasher _hasher;

        public PreProcessManager(FileSystemHelper fileSystemHelper, IHasher hasher)
        {
            _hasher = hasher;
            _fileSystemHelper = fileSystemHelper;
        }

        /// <summary>
        /// If the current asset/request requires minification, this will check the cache for its existence, if it doesn't
        /// exist, it will process it and store the cache file. Lastly, it sets the file path for the JavaScript file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task ProcessAndCacheFileAsync(IWebFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
			if (file.Pipeline == null) throw new ArgumentNullException(string.Format("{0}.Pipeline", nameof(file)));

            switch (file.DependencyType)
            {
                case WebFileType.Js:
                    await ProcessJsFile(file);
                    break;
                case WebFileType.Css:
                    await ProcessCssFile(file);
                    break;
            }
        }

        private async Task ProcessCssFile(IWebFile file)
        {
            await ProcessFile(file, ".css");
        }

        private async Task ProcessJsFile(IWebFile file)
        {
            await ProcessFile(file, ".js");
        }

        private async Task ProcessFile(IWebFile file, string extension)
        {
            //If Its external throw an exception this is not allowed. 
            if (file.FilePath.Contains(Constants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            };

            //check if it's in cache

            //TODO: If we make the hash as part of the last write time of the file, then the hash will be different
            // which means it will be a new cached file which means we can have auto-changing of files. Versioning
            // will still be manual but that would just be up to the client cache, not the server cache. But,
            // before we do that we need to consider performance because this means that for every file that is hashed
            // we'd need to lookup it's last write time so that all hashes match which isn't really ideal.

            //var filePath = _fileSystemHelper.MapPath(file.FilePath);
            //var lastWrite = File.GetLastWriteTimeUtc(filePath);
            //var hashName = _hasher.Hash(file.FilePath + lastWrite) + extension;

            var hashName = _hasher.Hash(file.FilePath) + extension;
            var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            var cacheFile = Path.Combine(cacheDir, hashName);

            Directory.CreateDirectory(cacheDir);

            if (!File.Exists(cacheFile))
            {
                var filePath = _fileSystemHelper.MapPath(file.FilePath);
                
                //doesn't exist, throw as thsi shouldn't happen
                if (File.Exists(filePath) == false) throw new FileNotFoundException("No file found with path " + filePath);

                var contents = await _fileSystemHelper.ReadContentsAsync(filePath);

                //process the file
                var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file));

                //save it to the cache path
                await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);
            }
        }

    }
}