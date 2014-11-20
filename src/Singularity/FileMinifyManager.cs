using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Singularity.Files;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Runtime;

namespace Singularity
{
    public sealed class FileMinifyManager
    {
        private FileSystemHelper _fileSystemHelper;

        public FileMinifyManager(FileSystemHelper fileSystemHelper)
        {
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
                case WebFileType.Javascript:
                    await ProcessJsFile(file);
                    break;
                case WebFileType.Css:
                    break;
                default:
                    break;
            }
        }

        private async Task ProcessJsFile(IWebFile file)
        {
            //check if it's in cache
            var hashName = file.FilePath.GenerateHash() + ".js";
            var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            var cacheFile = Path.Combine(cacheDir, hashName);

            Directory.CreateDirectory(cacheDir);

            if (!File.Exists(cacheFile))
            {
                var filePath = _fileSystemHelper.MapPath(file.FilePath);
                var contents = await _fileSystemHelper.ReadContentsAsync(filePath);

                if (file.Minify)
                {
                    var minify = JsMin.CompressJS(contents);

                    //save it to the cache path
                    await _fileSystemHelper.WriteContentsAsync(cacheFile, minify);
                }
                else
                {
                    //save the raw content to the cache path
                    await _fileSystemHelper.WriteContentsAsync(cacheFile, contents);
                }
            }

            file.FilePath = hashName;
        }

    }
}