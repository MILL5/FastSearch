#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class ReviewRegressionTests
    {
        private sealed record SearchItem(string? First, string? Second);

        private static string?[] Fields(SearchItem item) => new[] { item.First, item.Second };

        private static void AssertMatchesOracle(SearchItem[] items, string query)
        {
            var expected = new StringSearch<SearchItem>(items, Fields).Search(query);
            var actual = new LinqSearch<SearchItem>(items, Fields).Search(query);

            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var item in items)
            {
                Assert.AreEqual(
                    expected.Count(match => ReferenceEquals(match, item)),
                    actual.Count(match => ReferenceEquals(match, item)));
            }
        }

        [TestMethod]
        public void Linq_MultipleMatchingFields_ReturnsEachInputOccurrenceOnce()
        {
            AssertMatchesOracle(new[] { new SearchItem("Alpha", "alphabet") }, "ALPHA");
        }

        [TestMethod]
        public void Linq_RepeatedReference_PreservesBothInputOccurrences()
        {
            var item = new SearchItem("Alpha", "alphabet");

            AssertMatchesOracle(new[] { item, item }, "alpha");
        }

        [TestMethod]
        public void Linq_DistinctValueEqualObjects_PreservesBothIdentities()
        {
            var first = new SearchItem("Alpha", "alphabet");
            var second = new SearchItem("Alpha", "alphabet");
            Assert.AreEqual(first, second);
            Assert.AreNotSame(first, second);

            AssertMatchesOracle(new[] { first, second }, "alpha");
        }

        [TestMethod]
        public void Linq_SingleNonNullField_PreservesOracleResults()
        {
            var item = new SearchItem("Alpha", null);
            var equalItem = new SearchItem("Alpha", null);
            var items = new[] { item, item, equalItem, new SearchItem(null, "BETA"), new SearchItem(null, null) };

            foreach (var query in new[] { "alpha", "beta", "missing", "" })
            {
                AssertMatchesOracle(items, query);
            }
        }

        [TestMethod]
        public void CharSequence_ExactMode_SkipsEmptyAndNullFields()
        {
            var valid = new SearchItem("", "Alpha");
            var items = new[] { new SearchItem("", null), new SearchItem(null, null), valid };
            var search = new CharSequenceSearch<SearchItem>(items, Fields, enableExactMatch: true);

            var matches = search.Search("ALPHA");
            Assert.HasCount(1, matches);
            Assert.AreSame(valid, matches.Single());
            Assert.IsEmpty(search.Search("alp"));
            Assert.IsEmpty(search.Search(""));
            Assert.IsEmpty(search.Search(null));
        }
    }
}
