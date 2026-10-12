#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class EmptyQueryTests
    {
        [TestMethod]
        public void NullAndEmptyQueriesReturnNoResultsAcrossAllAlgorithms()
        {
            var items = new[] { "banana", "", "cabana", "null field" };
            Func<string, string?[]> selector = item =>
                new string?[] { item == "null field" ? null : item };
            ISearch<string>[] algorithms =
            {
                new StringSearch<string>(items, selector),
                new LinqSearch<string>(items, selector),
                new HashSearch<string>(items, selector),
                new CharSequenceSearch<string>(items, selector),
                new SuffixArraySearch<string>(items, selector),
                new NGramSearch<string>(items, selector)
            };

            foreach (var algorithm in algorithms)
            {
                var name = algorithm.GetType().Name;
                Assert.AreEqual(0, algorithm.Search(null).Count, $"{name}: null query");
                Assert.AreEqual(0, algorithm.Search("").Count, $"{name}: empty query");
                CollectionAssert.AreEquivalent(new[] { "banana", "cabana" },
                    algorithm.Search("BANA").ToArray(), $"{name}: non-empty query");
                Assert.AreEqual(0, algorithm.Search("").Count, $"{name}: empty query after a match");
            }
        }
    }
}
