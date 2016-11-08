using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace Smidge.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<JsMinifyBenchmarks>();
            //var summary = BenchmarkRunner.Run<BulkInsertBenchmarks>();

            Console.ReadLine();
        }
    }
}
