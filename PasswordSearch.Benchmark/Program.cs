using BenchmarkDotNet.Running;
using System;

namespace PasswordSearch.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            // dotnet run -c Release -- heap <algorithm> <items>   : heap/latency harness (see HeapBenchmark.cs)
            if (args.Length > 0 && args[0] == "heap")
            {
                HeapBenchmark.Run(args);
                return;
            }

            var config = new BenchmarkConfig();

            //var b = new BenchmarkIndexing();
            //var o = b.HashSearchIndexing();

            var summarys = BenchmarkRunner.Run(typeof(Program).Assembly);

            foreach (var s in summarys)
            {
                Console.WriteLine(s);
            }
        }
    }
}
