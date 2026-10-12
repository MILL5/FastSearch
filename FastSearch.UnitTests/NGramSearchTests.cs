using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class NGramSearchTests
    {
        private static readonly List<string> _passwords;

        static NGramSearchTests()
        {
            _passwords = File.ReadLines("10-million-password-list-top-1000000.txt")
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Take(20000)
                             .ToList();
        }

        [TestMethod]
        public void MatchesBruteForceInOrder()
        {
            var oracle = new StringSearch<string>(_passwords);
            var ng = new NGramSearch<string>(_passwords);

            var rnd = new Random(99);
            var queries = new HashSet<string> { "a", "12", "123", "1234", "12345", "love", "LOVE", "phoenix", "catherine", "1q2w3e", "zzzzqqq", "~" };
            for (int i = 0; i < 5000; i++)
            {
                var s = _passwords[rnd.Next(_passwords.Count)];
                int len = rnd.Next(1, Math.Min(12, s.Length) + 1);
                var q = s.Substring(rnd.Next(0, s.Length - len + 1), len);
                queries.Add(rnd.Next(2) == 0 ? q : q.ToUpperInvariant());
                // Near misses: mutate one char so grams exist but the full pattern may not.
                var chars = q.ToCharArray();
                chars[rnd.Next(chars.Length)] = (char)('a' + rnd.Next(26));
                queries.Add(new string(chars));
            }

            foreach (var q in queries)
            {
                var expected = oracle.Search(q).ToArray();
                var actual = ng.Search(q);
                Assert.AreEqual(expected.Length, actual.Count, $"count for '{q}'");
                CollectionAssert.AreEqual(expected, actual.ToArray(), $"items for '{q}'");
            }
        }

        [TestMethod]
        public void ResultsAreInInputOrderDistinctAndReadOnly()
        {
            var list = new List<string> { "banana", "bandana", "cabana", "anna", "xyz" };
            var ng = new NGramSearch<string>(list);

            CollectionAssert.AreEqual(new[] { "banana", "bandana", "cabana", "anna" }, ng.Search("an").ToArray());
            CollectionAssert.AreEqual(new[] { "banana", "bandana", "cabana" }, ng.Search("ANA").ToArray());
            CollectionAssert.AreEqual(new[] { "banana", "cabana" }, ng.Search("bana").ToArray());
            CollectionAssert.AreEqual(new[] { "banana" }, ng.Search("banan").ToArray());
            Assert.AreEqual(0, ng.Search("q").Count);
            Assert.AreEqual(0, ng.Search("").Count);
            Assert.AreEqual(0, ng.Search(null).Count);

            var view = ng.Search("a");
            Assert.IsTrue(view.IsReadOnly);
            Assert.IsTrue(view.Contains("anna"));
            Assert.IsFalse(view.Contains("xyz"));
            Assert.ThrowsExactly<NotSupportedException>(() => view.Clear());
            var copy = new string[view.Count + 1];
            view.CopyTo(copy, 1);
            CollectionAssert.AreEqual(new[] { null, "banana", "bandana", "cabana", "anna" }, copy);
        }

        [TestMethod]
        public void MultipleIndexStringsDoNotMatchAcrossBoundaries()
        {
            var people = new List<string> { "ab|cd", "abcd", "xx|ab" };
            var ng = new NGramSearch<string>(people, s => s.Split('|'));

            CollectionAssert.AreEqual(new[] { "abcd" }, ng.Search("bc").ToArray());
            CollectionAssert.AreEqual(new[] { "abcd" }, ng.Search("abcd").ToArray());
            CollectionAssert.AreEqual(new[] { "ab|cd", "abcd", "xx|ab" }, ng.Search("ab").ToArray());
            Assert.AreEqual(0, ng.Search("b\0c").Count);
        }

        [TestMethod]
        public void HandlesNonLatin1Text()
        {
            var list = new List<string> { "Ελληνικά κείμενα", "日本語のテキスト", "plain ascii", "ΚΕΊΜΕΝΑ" };
            var ng = new NGramSearch<string>(list);

            CollectionAssert.AreEqual(new[] { "Ελληνικά κείμενα", "ΚΕΊΜΕΝΑ" }, ng.Search("κείμενα").ToArray());
            CollectionAssert.AreEqual(new[] { "日本語のテキスト" }, ng.Search("のテキス").ToArray());
            CollectionAssert.AreEqual(new[] { "plain ascii" }, ng.Search("n asc").ToArray());
        }

        [TestMethod]
        public void LongQueriesOnFrequentGramsAreVerified()
        {
            // "xbcabcx" contains every gram of "abcab" (abc, bca, cab) but not "abcab" itself; padding makes "abc" frequent.
            var list = Enumerable.Range(0, 40).Select(i => "abcd" + i).ToList();
            list.Add("xbcabcx");
            list.Add("zabcabz");
            var ng = new NGramSearch<string>(list);

            CollectionAssert.AreEqual(new[] { "zabcabz" }, ng.Search("abcab").ToArray());
            Assert.AreEqual(40, ng.Search("abcd").Count);
        }
    }
}
