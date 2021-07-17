using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class SearchTests
    {
        private static readonly List<string> _simpleList = new()
        {
            "Rich Crane",
            "Camilo Jaramillo",
            "Jose Juarez",
            "Shri Bhupathi",
            "James Pansarasa",
            "Steve Tarmey",
            "John Puksta",
            "Betsy Caruso",
            "Craig Bray",
            "Nicole Frawley",
            "Thomas Duclos",
            "Nathan Franz",
            "Sorey Garcia",
            "Michelle Frattaroli"
        };

        private static readonly List<string> _passwords;
        private static readonly List<string> _phoenixList;

        static SearchTests()
        {
            var limit = 10000;
            var passwords = new List<string>(10000000);

            using (var s = new StreamReader(@"10-million-password-list-top-1000000.txt"))
            {
                while (s.Peek() > 0)
                {
                    var line = s.ReadLineAsync().Result;

                    if (!string.IsNullOrWhiteSpace(line))
                        passwords.Add(line);
                }
            }

            _passwords = passwords.Take(limit).ToList();
            _phoenixList = _passwords.Where(x => x.ToLowerInvariant().Contains("phoenix")).ToList();
        }

        [TestMethod]
        public void SimpleTest()
        {
            var s1 = new HashSearch<string>(_simpleList) ;
            var s2 = new CharSequenceSearch<string>(_simpleList);

            var r1 = s1.Search("a");
            var r2 = s2.Search("a");

            Assert.AreEqual(r1.Count, r2.Count);

            var r3 = s1.Search("ar");
            var r4 = s2.Search("ar");

            Assert.AreEqual(r3.Count, r4.Count);

            var r5 = s1.Search("z");
            var r6 = s2.Search("z");

            Assert.AreEqual(r5.Count, r6.Count);

            var r7 = s1.Search("Tarmey");
            var r8 = s2.Search("Tarmey");

            Assert.AreEqual(r7.Count, r8.Count);
        }

        [TestMethod]
        public void ExpectedRows()
        {
            var s1 = new HashSearch<string>(_phoenixList);
            var s2 = new CharSequenceSearch<string>(_phoenixList);

            var r1 = s1.Search("x");
            var r2 = s2.Search("x");
            var union = r1.Intersect(r2).ToList();

            Assert.AreEqual(r1.Count, r2.Count, union.Count);
        }

        [TestMethod]
        public void IndexAndSearchTest1()
        {
            var list = new List<string> {
                                          "Moison Ace Hardware",
                                          "Lorden True Value Hardware",
                                          "Du Verre Hardware",
                                          "Gould NY Drapery Hardware",
                                          "Du Verre: The Hardware Co"
                                        };
            var s1 = new HashSearch<string>(list);
            var s2 = new CharSequenceSearch<string>(list);

            var r1 = s1.Search("Hardware");
            var r2 = s2.Search("Hardware");
            var union = r1.Intersect(r2).ToList();

            Assert.AreEqual(r1.Count, r2.Count, list.Count);
            Assert.AreEqual(r1.Count, r2.Count, union.Count);
        }

        [TestMethod]
        public void IndexAndSearchTest2()
        {
            IEnumerable<string> lines = File.ReadAllLines("ExcludedTestFile.txt");
            var list = lines.Where(x => x.Contains("Hardware", System.StringComparison.OrdinalIgnoreCase)).ToList();

            var s1 = new HashSearch<string>(lines);
            var s2 = new CharSequenceSearch<string>(lines);

            var r1 = s1.Search("Hardware");
            var r2 = s2.Search("Hardware");

            var union = r1.Intersect(r2).ToList();

            var found = new List<string>();
            foreach (var item in r2)
            {
                if (!r1.Contains(item))
                {
                    found.Add(item);
                }
            }

            Assert.AreEqual(r1.Count, r2.Count, list.Count);
            Assert.AreEqual(r1.Count, r2.Count, union.Count);
        }
    }
}
