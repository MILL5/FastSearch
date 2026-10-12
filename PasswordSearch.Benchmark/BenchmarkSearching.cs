using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FastSearch;
using BenchmarkDotNet.Attributes;
using static PasswordSearch.Benchmark.BenchmarkShared;

namespace PasswordSearch.Benchmark
{
    [MemoryDiagnoser]
    public class BenchmarkSearching
    {
        private static StringSearch<string> _stringSearch;
        private static CharSequenceSearch<string> _charSequenceSearch;
        private static HashSearch<string> _hashSearch;
        private static LinqSearch<string> _linqSearch;
        private static SuffixArraySearch<string> _suffixArraySearch;
        private static NGramSearch<string> _nGramSearch;

        static BenchmarkSearching()
        {
            Initialize();

            _stringSearch = new StringSearch<string>(Passwords);
            _charSequenceSearch = new CharSequenceSearch<string>(Passwords);
            _hashSearch = new HashSearch<string>(Passwords);
            _linqSearch = new LinqSearch<string>(Passwords);
            _suffixArraySearch = new SuffixArraySearch<string>(Passwords);
            _nGramSearch = new NGramSearch<string>(Passwords);
        }

        [Benchmark]
        public ICollection<string> StringSearchFora()
        {
            return _stringSearch.Search("a");
        }

        [Benchmark]
        public ICollection<string> StringSearchForcatherine()
        {
            return _stringSearch.Search("catherine");
        }

        [Benchmark]
        public ICollection<string> StringSearchForphoenix()
        {
            return _stringSearch.Search("phoenix");
        }

        [Benchmark]
        public ICollection<string> StringSearchForPHOENIX()
        {
            return _stringSearch.Search("PHOENIX");
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

        [Benchmark]
        public ICollection<string> SuffixArraySearchFora()
        {
            return _suffixArraySearch.Search("a");
        }

        [Benchmark]
        public ICollection<string> SuffixArraySearchForcatherine()
        {
            return _suffixArraySearch.Search("catherine");
        }

        [Benchmark]
        public ICollection<string> SuffixArraySearchForphoenix()
        {
            return _suffixArraySearch.Search("phoenix");
        }

        [Benchmark]
        public ICollection<string> SuffixArraySearchForPHOENIX()
        {
            return _suffixArraySearch.Search("PHOENIX");
        }

        // NGramSearch answers 1-3 character queries with a lazy view that decodes on enumeration, so these
        // numbers time the Search call only. HeapBenchmark ("heap ngram <items>") includes enumeration.
        [Benchmark]
        public ICollection<string> NGramSearchFora()
        {
            return _nGramSearch.Search("a");
        }

        [Benchmark]
        public ICollection<string> NGramSearchForcatherine()
        {
            return _nGramSearch.Search("catherine");
        }

        [Benchmark]
        public ICollection<string> NGramSearchForphoenix()
        {
            return _nGramSearch.Search("phoenix");
        }

        [Benchmark]
        public ICollection<string> NGramSearchForPHOENIX()
        {
            return _nGramSearch.Search("PHOENIX");
        }
    }
}
