using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FastSearch;
using BenchmarkDotNet.Attributes;
using static PasswordSearch.Benchmark.BenchmarkShared;

namespace PasswordSearch.Benchmark
{
    public class BenchmarkSearching
    {
        private static CharSequenceSearch<string> _charSequenceSearch;
        private static HashSearch<string> _hashSearch;
        private static LinqSearch<string> _linqSearch;

        static BenchmarkSearching()
        {
            Initialize();

            _charSequenceSearch = new CharSequenceSearch<string>(Passwords);
            _hashSearch = new HashSearch<string>(Passwords);
            _linqSearch = new LinqSearch<string>(Passwords);
        }

        [Benchmark]
        public ICollection<string> CharSearchFora()
        {
            return _charSequenceSearch.Search("a");
        }

        [Benchmark]
        public ICollection<string> CharSearchForcatherine()
        {
            return _charSequenceSearch.Search("catherine");
        }

        [Benchmark]
        public ICollection<string> CharSearchForphoenix()
        {
            return _charSequenceSearch.Search("phoenix");
        }

        [Benchmark]
        public ICollection<string> CharSearchForPHOENIX()
        {
            return _charSequenceSearch.Search("PHOENIX");
        }

        [Benchmark]
        public ICollection<string> HashSearchFora()
        {
            return _hashSearch.Search("a");
        }

        [Benchmark]
        public ICollection<string> HashSearchForcatherine()
        {
            return _hashSearch.Search("catherine");
        }

        [Benchmark]
        public ICollection<string> HashSearchForphoenix()
        {
            return _hashSearch.Search("phoenix");
        }

        [Benchmark]
        public ICollection<string> HashSearchForPHOENIX()
        {
            return _hashSearch.Search("PHOENIX");
        }

        [Benchmark]
        public ICollection<string> LinqSearchFora()
        {
            return _linqSearch.Search("a");
        }
        [Benchmark]
        public ICollection<string> LinqSearchForcatherine()
        {
            return _linqSearch.Search("catherine");
        }

        [Benchmark]
        public ICollection<string> LinqSearchForphoenix()
        {
            return _linqSearch.Search("phoenix");
        }

        [Benchmark]
        public ICollection<string> LinqSearchForPHOENIX()
        {
            return _linqSearch.Search("PHOENIX");
        }
    }
}
