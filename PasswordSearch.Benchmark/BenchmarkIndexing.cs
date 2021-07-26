using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FastSearch;
using BenchmarkDotNet.Attributes;
using static PasswordSearch.Benchmark.BenchmarkShared;

namespace PasswordSearch.Benchmark
{
    public class BenchmarkIndexing
    {
        static BenchmarkIndexing()
        {
            Initialize();
        }

        [Benchmark]
        public CharSequenceSearch<string> CharSequenceSearchIndexing()
        {
            return new CharSequenceSearch<string>(Passwords);
        }

        [Benchmark]
        public HashSearch<string> HashSearchIndexing()
        {
            return new HashSearch<string>(Passwords);
        }

        [Benchmark]
        public LinqSearch<string> LinqSearchIndexing()
        {
            return new LinqSearch<string>(Passwords);
        }
    }
}
