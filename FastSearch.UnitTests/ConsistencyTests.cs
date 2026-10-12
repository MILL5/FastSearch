using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shouldly;
using System;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class ConsistencyTests
    {
        private static readonly List<string> _passwords;

        static ConsistencyTests()
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
        }

        [TestMethod]
        public void SearchAllHashEntries()
        {
            var hs = new HashSearch<string>(_passwords);
            var css = new CharSequenceSearch<string>(_passwords);

            var entries = hs._rootmap.ToList();

            int count = 0;

            if (entries.Count > 0)
            {
                foreach (var hashIndexEntry in entries)
                {
                    foreach (var valueEntry in hashIndexEntry.Items)
                    {
                        var foundinhash = hs.Search(valueEntry.Value);
                        var foundincharseq = css.Search(valueEntry.Value);

                        Assert.AreEqual(foundinhash.Count, foundincharseq.Count);
                        Assert.AreEqual(valueEntry.Items.Count, foundinhash.Count);
                        Assert.AreEqual(valueEntry.Items.Count, foundincharseq.Count);
                        count++;
                    }
                }
            }

            count.ShouldBeGreaterThan(0);

            Console.WriteLine($"{count} searches performed");
        }

        [TestMethod]
        public void SearchForHashCollisions()
        {
            // A deliberately weak hash forces many substrings into each bucket, so collision handling is
            // always exercised (string.GetHashCode is randomized per process, which made this test flaky).
            var sample = _passwords.Take(1000).ToList();
            var hs = new HashSearch<string>(sample, null, null, s => s.Length * 31 + s[0] % 4);
            var oracle = new StringSearch<string>(sample);

            var collisions = hs._rootmap.Where(x => x.Items.Count > 1).ToList();
            collisions.Count.ShouldBeGreaterThan(0);

            foreach (var hashIndexEntry in collisions)
            {
                foreach (var valueEntry in hashIndexEntry.Items.Take(5))
                {
                    var found = hs.Search(valueEntry.Value);

                    found.Count.ShouldBe(valueEntry.Items.Count);
                    found.Count.ShouldBe(oracle.Search(valueEntry.Value).Count);
                }
            }
        }
    }
}
