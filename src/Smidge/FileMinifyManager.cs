using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Smidge.Files;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Runtime;

namespace Smidge
{
    public sealed class FileMinifyManager
    {
        private FileSystemHelper _fileSystemHelper;
        private SmidgeOptions _options;

        public FileMinifyManager(FileSystemHelper fileSystemHelper, SmidgeOptions options)
        {
            _options = options;
            _fileSystemHelper = fileSystemHelper;
        }

        /// <summary>
        /// If the current asset/request requires minification, this will check the cache for its existence, if it doesn't
        /// exist, it will minify it and store the cache file. Lastly, it sets the file path for the JavaScript file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task MinifyAndCacheFileAsync(IWebFile file)
        {
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
            await ProcessFile(file, ".css", s => _options.DefaultCssMinifier.Minify(s));
        }

        private async Task ProcessJsFile(IWebFile file)
        {
            await ProcessFile(file, ".js", s => _options.DefaultJavaScriptMinifier.Minify(s));
        }

        private async Task ProcessFile(IWebFile file, string extension, Func<string, string> processor)
        {
            //check if it's in cache
            var hashName = _options.Hasher.Hash(file.FilePath) + extension;
            var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            var cacheFile = Path.Combine(cacheDir, hashName);

            Directory.CreateDirectory(cacheDir);

            if (!File.Exists(cacheFile))
            {
                var filePath = _fileSystemHelper.MapPath(file.FilePath);
                var contents = await _fileSystemHelper.ReadContentsAsync(filePath);

                if (file.Minify)
                {
                    var minify = processor(contents);

                    //save it to the cache path
                    await _fileSystemHelper.WriteContentsAsync(cacheFile, minify);
                }
                else
                {
                    //save the raw content to the cache path
                    await _fileSystemHelper.WriteContentsAsync(cacheFile, contents);
                }
            }
        }

    }
}