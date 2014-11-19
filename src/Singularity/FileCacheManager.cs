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
    public sealed class FileCacheManager
    {
        private SingularityConfig _config;
        private IHostingEnvironment _env;
        private string _dataFolder;
        private IApplicationEnvironment _appEnv;

        public FileCacheManager(SingularityConfig config, IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            _appEnv = appEnv;
            _config = config;
            _env = env;
            _dataFolder = config.DataFolder;
        }

        public async Task SetFilePathAsync(JavaScriptFile file)
        {
            if (!_config.IsDebug && file.Minify)
            {
                //check if it's in cache
                var hashName = file.FilePath.GenerateHash() + ".js";
                var cacheDir = Path.Combine(_appEnv.ApplicationBasePath, _dataFolder, "Cache", _config.ServerName, _config.Version);
                var cacheFile = Path.Combine(cacheDir, hashName);

                Directory.CreateDirectory(cacheDir);

                if (!File.Exists(cacheFile))
                {
                    var filePath = Path.Combine(_env.WebRoot, file.FilePath).Replace("/", "\\");
                    string contents;
                    using (var fileStream = File.OpenRead(filePath))
                    using (var reader = new StreamReader(fileStream))
                    {
                        contents = await reader.ReadToEndAsync();
                    }
                    var minify = JsMin.CompressJS(contents);

                    //save it to the cache path
                    using (var writer = File.CreateText(cacheFile))
                    {
                        await writer.WriteAsync(minify);
                    }
                }               

                file.FilePath = hashName;
            }
        }

    }
}