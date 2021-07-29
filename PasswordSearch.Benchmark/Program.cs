using BenchmarkDotNet.Running;
using System;

namespace PasswordSearch.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
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
