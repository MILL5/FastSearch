using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FastSearch;

namespace PasswordSearch.Benchmark
{
    /// <summary>
    /// Apples-to-apples heap and latency harness. Run ONE algorithm per process so each
    /// measurement starts from the same clean heap:
    ///   dotnet run -c Release -- heap &lt;string|linq|hash|charseq|suffixarray|ngram&gt; &lt;items&gt;
    /// Heap = GC.GetTotalMemory(forceFullCollection: true) after build minus before build.
    /// The input strings are loaded before the "before" snapshot, so they are excluded for every algorithm.
    /// </summary>
    public static class HeapBenchmark
    {
        public static void Run(string[] args)
        {
            var algorithm = args[1];
            int count = int.Parse(args[2]);

            var items = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "10-million-password-list-top-1000000.txt"))
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Take(count)
                            .ToList();
            long chars = items.Sum(x => (long)x.Length);

            var rnd = new Random(42);
            var mix = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                var s = items[rnd.Next(items.Count)];
                int len = rnd.Next(1, Math.Min(8, s.Length) + 1);
                mix.Add(s.Substring(rnd.Next(0, s.Length - len + 1), len));
            }

            long before = Heap();
            var sw = Stopwatch.StartNew();
            ISearch<string> search = algorithm switch
            {
                "string" => new StringSearch<string>(items),
                "linq" => new LinqSearch<string>(items),
                "hash" => new HashSearch<string>(items),
                "charseq" => new CharSequenceSearch<string>(items),
                "suffixarray" => new SuffixArraySearch<string>(items),
                "ngram" => new NGramSearch<string>(items),
                _ => throw new ArgumentException(algorithm)
            };
            sw.Stop();
            long heap = Heap() - before;

            // Cold random mix first (realistic: nothing cached yet), then warm named queries.
            var cold = Stopwatch.StartNew();
            foreach (var q in mix) search.Search(q);
            cold.Stop();

            var named = new[] { "a", "12", "love", "phoenix", "catherine" };
            var latencies = new List<string>();
            foreach (var q in named)
            {
                search.Search(q);
                int iterations = algorithm is "string" or "linq" ? 20 : 2000;
                var t = Stopwatch.StartNew();
                int hits = 0;
                for (int i = 0; i < iterations; i++)
                {
                    var r = search.Search(q);
                    hits = 0;
                    foreach (var _ in r) hits++; // enumerate: every algorithm pays to hand back results
                }
                t.Stop();
                latencies.Add($"{q}={t.Elapsed.TotalMilliseconds * 1000 / iterations:F2}us/{hits}");
            }
            long heapAfterQueries = Heap() - before;

            Console.WriteLine(string.Join("\t", new object[]
            {
                "RESULT", algorithm, items.Count, chars,
                sw.Elapsed.TotalMilliseconds.ToString("F0"),
                heap, heapAfterQueries,
                (cold.Elapsed.TotalMilliseconds * 1000 / mix.Count).ToString("F2"),
                string.Join(",", latencies)
            }));
            GC.KeepAlive(search);
        }

        private static long Heap()
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            return GC.GetTotalMemory(forceFullCollection: true);
        }
    }
}
