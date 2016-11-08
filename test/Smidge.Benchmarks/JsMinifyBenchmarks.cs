using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.NodeServices;
using Smidge.FileProcessors;
using Smidge.JavaScriptServices;
using Smidge.Models;
using Smidge.Nuglify;

namespace Smidge.Benchmarks
{
    [Config(typeof(Config))]
    public class JsMinifyBenchmarks
    {
        /// <summary>
        /// initialize one time
        /// </summary>
        static JsMinifyBenchmarks()
        {            
            _assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(JsMinifyBenchmarks).Assembly.Location), "temp");
            Directory.CreateDirectory(_assemblyPath);

            if (!Directory.Exists(Path.Combine(_assemblyPath, "node_modules")))
            {
                //copy over all of the node modules
                var dirInfo = new DirectoryInfo(_assemblyPath);
                while (dirInfo != null && !dirInfo.Name.Equals("test", StringComparison.OrdinalIgnoreCase))
                {
                    dirInfo = dirInfo.Parent;
                }
                dirInfo = dirInfo.Parent; //this will be the sln root
                dirInfo = dirInfo.GetDirectories("src").Single();
                dirInfo = dirInfo.GetDirectories("Smidge.Web").Single();
                dirInfo = dirInfo.GetDirectories("node_modules").Single();

                DirectoryCopy(dirInfo.FullName, Path.Combine(_assemblyPath, "node_modules"), true);
            }

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Smidge.Benchmarks.jQuery.js")))
            {
                _jQuery = reader.ReadToEnd();
            }
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new MemoryDiagnoser());
        
            }
        }

        private JsMinifier _jsMin;
        private NuglifyJs _nuglify;
        private UglifyNodeMinifier _jsUglify;
        private static readonly string _jQuery;
        private static readonly string _assemblyPath;

        private class NullServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }

        [Setup]
        public void Setup()
        {
            _jsMin = new JsMinifier();
            _nuglify = new NuglifyJs();
            
            var nodeServices = new SmidgeJavaScriptServices(NodeServicesFactory.CreateNodeServices(
                new NodeServicesOptions(new NullServiceProvider())
                {
                    ProjectPath = _assemblyPath,
                    WatchFileExtensions = new string[] {}
                }));
            _jsUglify = new UglifyNodeMinifier(nodeServices);            
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }


        [Cleanup]
        public void Cleanup()
        {         
        }

        [Benchmark(Baseline = true)]
        public async Task JsMin()
        {
            var result = await _jsMin.ProcessAsync(new FileProcessContext(_jQuery, new JavaScriptFile()));
        }

        [Benchmark]
        public async Task Nuglify()
        {
            var result = await _nuglify.ProcessAsync(new FileProcessContext(_jQuery, new JavaScriptFile()));
        }

        [Benchmark]
        public async Task JsUglify()
        {           
            var result = await _jsUglify.ProcessAsync(new FileProcessContext(_jQuery, new JavaScriptFile()));
        }


    }
}