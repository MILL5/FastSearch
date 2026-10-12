using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FastSearch
{
    /// <summary>
    /// Case-insensitive substring search backed by a generalized suffix array.
    /// <para>
    /// Memory is O(N·L): ~10 bytes per indexed character (lowercased text + suffix array + owner id),
    /// versus O(N·L²) for the suffix trie used by <see cref="CharSequenceSearch{T}"/>.
    /// </para>
    /// <para>
    /// Search: a 1/2/3-character prefix table gives the exact match range for short queries with no
    /// binary search; longer queries binary-search only inside their 3-character bucket and skip the
    /// known prefix. Results are de-duplicated and returned in original input order. Broad results
    /// (64+ hits) are memoized within a fixed budget (one reference per suffix; reset when full).
    /// Safe for concurrent searches.
    /// </para>
    /// </summary>
    public sealed class SuffixArraySearch<T> : ISearch<T> where T : class
    {
        private const char Sep = '\0';
        private const int DefaultCacheThreshold = 64;
        private static readonly T[] Empty = Array.Empty<T>();

        private readonly T[] _items;
        private readonly char[] _text;      // all lowercased strings, each terminated by '\0'
        private readonly int[] _sa;         // suffix start positions, lexicographically sorted
        private readonly int[] _saItem;     // owning item index for each suffix-array entry
        private readonly Dictionary<ulong, Range> _prefix1;
        private readonly Dictionary<ulong, Range> _prefix2;
        private readonly Dictionary<ulong, Range> _prefix3;
        private readonly int _cacheThreshold;
        private readonly long _cacheBudget;   // max item references held by the cache (= suffix count)
        private long _cachedRefs;
        private readonly ConcurrentDictionary<string, ICollection<T>> _cache = new ConcurrentDictionary<string, ICollection<T>>();

        private readonly struct Range
        {
            public Range(int lo, int hi) { Lo = lo; Hi = hi; }
            public readonly int Lo;
            public readonly int Hi;
        }

        private static string?[] IndexThis(T instance) => new[] { instance.ToString() };

        public SuffixArraySearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null, int cacheThreshold = DefaultCacheThreshold)
        {
            ArgumentNullException.ThrowIfNull(items);
            indexFunc ??= IndexThis;
            _cacheThreshold = cacheThreshold;
            _items = new List<T>(items).ToArray();

            // 1. Lowercase and concatenate every indexed string.
            var strings = new List<(string s, int item)>(_items.Length);
            long total = 0, suffixes = 0;
            for (int i = 0; i < _items.Length; i++)
            {
                foreach (var raw in indexFunc(_items[i]))
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    var s = raw.ToLowerInvariant();
                    strings.Add((s, i));
                    total += s.Length + 1;
                    suffixes += s.Length;
                }
            }

            _text = new char[total];
            var sa = new int[suffixes];
            var owner = new int[total];
            int p = 0, q = 0;
            foreach (var (s, item) in strings)
            {
                s.CopyTo(0, _text, p, s.Length);
                for (int k = 0; k < s.Length; k++) { sa[q++] = p + k; owner[p + k] = item; }
                p += s.Length;
                _text[p++] = Sep;
            }

            // 2. Sort suffixes: primitive sort on a 4-char (64-bit) key, then refine tied runs.
            var keys = new ulong[sa.Length];
            for (int i = 0; i < sa.Length; i++) keys[i] = Key(sa[i], 4);
            Array.Sort(keys, sa);

            var cmp = new TailComparer(_text, 4);
            for (int start = 0, i = 1; i <= sa.Length; i++)
            {
                if (i == sa.Length || keys[i] != keys[start])
                {
                    if (i - start > 1 && !KeyHasSep(keys[start]))
                        Array.Sort(sa, start, i - start, cmp);
                    start = i;
                }
            }
            keys = null;

            _sa = sa;
            _saItem = new int[sa.Length];
            for (int i = 0; i < sa.Length; i++) _saItem[i] = owner[sa[i]];
            owner = null;

            // 3. Prefix tables: exact SA ranges for every 1-, 2- and 3-character prefix.
            _prefix1 = BuildPrefixTable(1);
            _prefix2 = BuildPrefixTable(2);
            _prefix3 = BuildPrefixTable(3);
            _cacheBudget = _sa.Length;
        }

        /// <summary>Number of indexed items.</summary>
        public int Count => _items.Length;

        public ICollection<T> Search(string? search)
        {
            if (string.IsNullOrEmpty(search)) return Empty;

            // Lowercase without allocating for typical (short) queries.
            Span<char> buffer = search.Length <= 256 ? stackalloc char[search.Length] : new char[search.Length];
            search.AsSpan().ToLowerInvariant(buffer);
            ReadOnlySpan<char> pattern = buffer;

            int k = Math.Min(pattern.Length, 3);
            var table = k == 1 ? _prefix1 : k == 2 ? _prefix2 : _prefix3;
            if (!table.TryGetValue(PatternKey(pattern, k), out var range)) return Empty;

            int lo = range.Lo, hi = range.Hi;
            if (pattern.Length > 3)
            {
                lo = LowerBound(pattern, lo, hi);
                hi = UpperBound(pattern, lo, hi);
            }

            int hits = hi - lo;
            if (hits <= 0) return Empty;
            if (hits < _cacheThreshold) return Collect(lo, hi);

            var key = pattern.ToString();
            if (_cache.TryGetValue(key, out var cached)) return cached;
            var collected = Collect(lo, hi);
            var result = new ReadOnlyCollection<T>(collected);

            // Memoize within a fixed budget (one reference per suffix, i.e. at most ~8 bytes per indexed
            // character). When the budget is exceeded the cache is reset, so hot queries re-enter quickly.
            if (collected.Length <= _cacheBudget)
            {
                if (System.Threading.Interlocked.Add(ref _cachedRefs, collected.Length) > _cacheBudget)
                {
                    _cache.Clear();
                    System.Threading.Interlocked.Exchange(ref _cachedRefs, collected.Length);
                }
                if (!_cache.TryAdd(key, result)) System.Threading.Interlocked.Add(ref _cachedRefs, -collected.Length);
            }
            return result;
        }

        private T[] Collect(int lo, int hi)
        {
            int hits = hi - lo;

            // Small result: sort + dedupe ids. Large result: bitmap over item ids, O(N/64 + hits).
            if (hits < _items.Length >> 6)
            {
                var ids = new int[hits];
                Array.Copy(_saItem, lo, ids, 0, hits);
                Array.Sort(ids);
                int n = 0;
                for (int i = 0; i < ids.Length; i++)
                    if (i == 0 || ids[i] != ids[i - 1]) ids[n++] = ids[i];
                var res = new T[n];
                for (int i = 0; i < n; i++) res[i] = _items[ids[i]];
                return res;
            }
            else
            {
                var bits = new ulong[(_items.Length + 63) >> 6];
                int n = 0;
                for (int i = lo; i < hi; i++)
                {
                    int id = _saItem[i];
                    ulong m = 1UL << (id & 63);
                    ref ulong w = ref bits[id >> 6];
                    if ((w & m) == 0) { w |= m; n++; }
                }
                var res = new T[n];
                int r = 0;
                for (int wi = 0; wi < bits.Length; wi++)
                {
                    ulong w = bits[wi];
                    while (w != 0)
                    {
                        res[r++] = _items[(wi << 6) + TrailingZeroCount(w)];
                        w &= w - 1;
                    }
                }
                return res;
            }
        }

        private Dictionary<ulong, Range> BuildPrefixTable(int k)
        {
            var table = new Dictionary<ulong, Range>();
            int start = -1;
            ulong current = 0;
            for (int i = 0; i < _sa.Length; i++)
            {
                ulong key = Key(_sa[i], k);
                if (start >= 0 && key == current) continue;
                if (start >= 0 && !KeyHasSep(current, k)) table[current] = new Range(start, i);
                current = key;
                start = i;
            }
            if (start >= 0 && !KeyHasSep(current, k)) table[current] = new Range(start, _sa.Length);
            table.TrimExcess();
            return table;
        }

        // Compare suffix at pos with pattern, skipping the first 3 chars (already matched by the bucket).
        private int ComparePrefix(int pos, ReadOnlySpan<char> pattern)
        {
            for (int k = 3; k < pattern.Length; k++)
            {
                char c = _text[pos + k];
                if (c == Sep) return -1;
                int d = c - pattern[k];
                if (d != 0) return d;
            }
            return 0;
        }

        private int LowerBound(ReadOnlySpan<char> pattern, int lo, int hi)
        {
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (ComparePrefix(_sa[mid], pattern) < 0) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        private int UpperBound(ReadOnlySpan<char> pattern, int lo, int hi)
        {
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (ComparePrefix(_sa[mid], pattern) <= 0) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        // Pack up to k chars (16 bits each) of the suffix at pos; pads with separator past the end.
        private ulong Key(int pos, int k)
        {
            ulong key = 0;
            for (int i = 0; i < k; i++)
            {
                char c = _text[pos];
                key = (key << 16) | c;
                if (c != Sep) pos++;
            }
            return key;
        }

        private static ulong PatternKey(ReadOnlySpan<char> pattern, int k)
        {
            ulong key = 0;
            for (int i = 0; i < k; i++) key = (key << 16) | pattern[i];
            return key;
        }

        private static bool KeyHasSep(ulong key, int k = 4)
        {
            for (int i = 0; i < k; i++) { if ((key & 0xFFFF) == 0) return true; key >>= 16; }
            return false;
        }

        private static int TrailingZeroCount(ulong w) => System.Numerics.BitOperations.TrailingZeroCount(w);

        private sealed class TailComparer : IComparer<int>
        {
            private readonly char[] _t;
            private readonly int _skip;
            public TailComparer(char[] t, int skip) { _t = t; _skip = skip; }

            public int Compare(int a, int b)
            {
                if (a == b) return 0;
                int i = a + _skip, j = b + _skip;
                while (true)
                {
                    char x = _t[i], y = _t[j];
                    if (x != y) return x - y;
                    if (x == Sep) return a - b; // identical text: order by position for determinism
                    i++; j++;
                }
            }
        }
    }
}
