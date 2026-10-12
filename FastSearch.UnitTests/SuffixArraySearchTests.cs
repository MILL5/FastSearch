using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class SuffixArraySearchTests
    {
        private static readonly List<string> _passwords;

        static SuffixArraySearchTests()
        {
            _passwords = File.ReadLines("10-million-password-list-top-1000000.txt")
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Take(10000)
                             .ToList();
        }

        private static void AssertSameItems(ICollection<string> expected, ICollection<string> actual, string query)
        {
            var e = new HashSet<string>(expected, new ReferenceEqualityComparer<string>());
            Assert.AreEqual(e.Count, actual.Count, $"count mismatch for '{query}'");
            foreach (var x in actual) Assert.IsTrue(e.Contains(x), $"unexpected item '{x}' for '{query}'");
        }

        [TestMethod]
        public void MatchesBruteForceForEverySubstringPrefix()
        {
            var oracle = new StringSearch<string>(_passwords);
            var sa = new SuffixArraySearch<string>(_passwords);
            var css = new CharSequenceSearch<string>(_passwords);
            var ng = new NGramSearch<string>(_passwords);

            var rnd = new Random(1234);
            var queries = new HashSet<string> { "a", "e", "1", "12", "123", "1234", "love", "phoenix", "PHOENIX", "PhOeNiX", "zzzzqqq", "!" };
            for (int i = 0; i < 2000; i++)
            {
                var s = _passwords[rnd.Next(_passwords.Count)];
                int len = rnd.Next(1, Math.Min(10, s.Length) + 1);
                var q = s.Substring(rnd.Next(0, s.Length - len + 1), len);
                queries.Add(rnd.Next(2) == 0 ? q : q.ToUpperInvariant());
            }

            foreach (var q in queries)
            {
                var expected = oracle.Search(q);
                AssertSameItems(expected, sa.Search(q), q);
                AssertSameItems(expected, css.Search(q), q);
                CollectionAssert.AreEqual(expected.ToArray(), ng.Search(q).ToArray(), q);
            }
        }

        [TestMethod]
        public void ResultsAreInInputOrderAndDistinct()
        {
            var list = new List<string> { "banana", "bandana", "cabana", "anna", "xyz" };
            var sa = new SuffixArraySearch<string>(list);

            CollectionAssert.AreEqual(new[] { "banana", "bandana", "cabana", "anna" }, sa.Search("an").ToArray());
            CollectionAssert.AreEqual(new[] { "banana", "bandana", "cabana" }, sa.Search("ana").ToArray());
            Assert.AreEqual(0, sa.Search("q").Count);
            Assert.AreEqual(0, sa.Search("").Count);
            Assert.AreEqual(0, sa.Search(null).Count);
        }

        [TestMethod]
        public void SupportsMultipleIndexStrings()
        {
            var names = new List<string> { "Rich Crane", "Steve Tarmey", "Betsy Caruso" };
            Func<string, string[]> indexFunc = s => new[] { s, new string(s.Reverse().ToArray()) };
            var sa = new SuffixArraySearch<string>(names, indexFunc);

            Assert.AreEqual("Steve Tarmey", sa.Search("evetS").Single());
            Assert.AreEqual("Steve Tarmey", sa.Search("tarm").Single());
        }

        [TestMethod]
        public void BroadQueriesAreCachedAndReadOnly()
        {
            var sa = new SuffixArraySearch<string>(_passwords, cacheThreshold: 10);
            var first = sa.Search("a");
            var second = sa.Search("A");

            Assert.AreSame(first, second);
            Assert.IsTrue(first.IsReadOnly);
            Assert.ThrowsExactly<NotSupportedException>(() => first.Clear());
        }

        [TestMethod]
        public void LongQueriesUseBinarySearchInsideBucket()
        {
            var list = new List<string> { "abcdefgh", "abcdxyz", "abc", "zabcdef", "abcde" };
            var sa = new SuffixArraySearch<string>(list);

            CollectionAssert.AreEqual(new[] { "abcdefgh", "zabcdef", "abcde" }, sa.Search("abcde").ToArray());
            CollectionAssert.AreEqual(new[] { "abcdefgh", "zabcdef" }, sa.Search("BCDEF").ToArray());
            Assert.AreEqual(0, sa.Search("abcdefghi").Count);
        }

        [TestMethod]
        public void CharSequenceExactMatchIsCaseInsensitiveOnSource()
        {
            var s = new CharSequenceSearch<string>(new List<string> { "Corperation" }, enableExactMatch: true);
            Assert.AreEqual("Corperation", s.Search("corpERATION").Single());
        }

        [TestMethod]
        public void TrieDoesNotExposeInternalLists()
        {
            var s = new CharSequenceSearch<string>(new List<string> { "abc" });
            var r = s.Search("a");
            Assert.IsTrue(r.IsReadOnly);
        }
    }
}
