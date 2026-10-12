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
        public void EmptyResultMutationCannotCorruptLaterSearchesOrOtherInstances()
        {
            var items = new[] { "banana", "cabana" };
            Func<ISearch<string>>[] factories =
            {
                () => new LinqSearch<string>(items),
                () => new HashSearch<string>(items),
                () => new CharSequenceSearch<string>(items)
            };

            foreach (var factory in factories)
            {
                var algorithm = factory();
                var otherInstance = factory();
                var name = algorithm.GetType().Name;
                foreach (var query in new string?[] { null, "", "no match" })
                {
                    var result = algorithm.Search(query);
                    Assert.AreEqual(0, result.Count, $"{name}: initially empty");
                    if (string.IsNullOrEmpty(query))
                        Assert.IsTrue(result.IsReadOnly, $"{name}: shared empty result must reject mutation");
                    if (result.IsReadOnly)
                        Assert.ThrowsExactly<NotSupportedException>(() => result.Add("injected"));
                    else
                        result.Add("injected"); // LinqSearch misses return a fresh mutable list.

                    foreach (var instance in new[] { algorithm, otherInstance })
                    {
                        Assert.AreEqual(0, instance.Search(null).Count, $"{name}: null after mutation attempt");
                        Assert.AreEqual(0, instance.Search("").Count, $"{name}: empty after mutation attempt");
                        Assert.AreEqual(0, instance.Search("no match").Count, $"{name}: miss after mutation attempt");
                        CollectionAssert.AreEquivalent(items, instance.Search("bana").ToArray(),
                            $"{name}: normal results remain intact");
                    }
                }
            }
        }

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
