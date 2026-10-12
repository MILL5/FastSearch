using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;

namespace FastSearch
{
    /// <summary>
    /// Case-insensitive substring search backed by compressed n-gram posting lists.
    /// <para>
    /// Every distinct 1-, 2- and 3-character substring of an item (and every 4-character one shared by 16+ items) maps to the sorted list of item ids that
    /// contain it, delta + varint encoded (~1–2 bytes per entry). Queries of 1–3 characters (and frequent 4-character ones) are answered
    /// exactly by a single list: no sorting, no de-duplication, no cache, and the result is a lazy read-only
    /// view whose <see cref="ICollection{T}.Count"/> is known up front and whose items are decoded on enumeration.
    /// Longer queries intersect the rarest gram lists and verify the survivors against a compact
    /// lowercased copy of the text (1 byte per character when the text is Latin-1).
    /// </para>
    /// Results are in input order with no duplicates. Safe for concurrent searches.
    /// </summary>
    public sealed class NGramSearch<T> : ISearch<T> where T : class
    {
        private const int MaxGram = 4;
        private const int AlwaysGram = 3;          // every 1..3-gram gets a list; 4-grams only when frequent
        private const int MinLongGramCount = 16;    // rarer 4-grams are cheaper to answer from 3-grams + verify
        private const int VerifyThreshold = 16;     // stop intersecting once this few candidates remain
        private const int IntersectRatio = 8;       // merge a list only if it is at most this many times longer than the candidates
        private static readonly T[] Empty = Array.Empty<T>();

        private readonly T[] _items;
        private readonly Dictionary<ulong, int> _grams;   // packed gram -> gram id
        private readonly int[] _postingStart;              // byte offset of each gram's list in _postings (length G + 1)
        private readonly int[] _postingCount;              // number of item ids in each gram's list
        private readonly byte[] _postings;                 // delta-varint item ids (ascending), or a bitmap for dense lists
        private readonly bool[] _dense;                    // true when a gram's list is stored as an N-bit bitmap

        // Lowercased text per item (multiple index strings separated by '\0'). Exactly one of these is non-null.
        private readonly byte[]? _text8;
        private readonly char[]? _text16;
        private readonly int[] _textStart;                 // length N + 1

        private static string?[] IndexThis(T instance) => new[] { instance.ToString() };

        public NGramSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null)
        {
            ArgumentNullException.ThrowIfNull(items);
            indexFunc ??= IndexThis;
            _items = new List<T>(items).ToArray();
            int n = _items.Length;

            // 1. Lowercase and concatenate each item's strings.
            var text = new List<char>();
            _textStart = new int[n + 1];
            bool latin1 = true;
            for (int i = 0; i < n; i++)
            {
                _textStart[i] = text.Count;
                bool first = true;
                foreach (var raw in indexFunc(_items[i]))
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    if (!first) text.Add('\0');
                    first = false;
                    foreach (var ch in raw.ToLowerInvariant())
                    {
                        text.Add(ch);
                        if (ch > 0xFF) latin1 = false;
                    }
                }
            }
            _textStart[n] = text.Count;
            var all = text.ToArray();
            text = null;

            // 2. Pass 1: assign gram ids and size each posting list (count + varint bytes).
            _grams = new Dictionary<ulong, int>(GramComparer.Instance);
            var last = new List<int>();
            var counts = new List<int>();
            var bytes = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int s = _textStart[i], e = _textStart[i + 1];
                for (int p = s; p < e; p++)
                {
                    ulong key = 0;
                    for (int k = 1; k <= MaxGram && p + k <= e; k++)
                    {
                        char c = all[p + k - 1];
                        if (c == '\0') break;
                        key = (key << 16) | c;
                        if (!_grams.TryGetValue(key, out int id))
                        {
                            id = _grams.Count;
                            _grams.Add(key, id);
                            last.Add(-1); counts.Add(0); bytes.Add(0);
                        }
                        if (last[id] == i) continue;
                        bytes[id] += VarSize((uint)(i - last[id] - 1));
                        counts[id]++;
                        last[id] = i;
                    }
                }
            }

            // Drop rare 4-grams: most distinct 4-grams occur in a handful of items, and their directory entries
            // would cost more than the 3-gram intersection they save.
            var all1 = _grams;
            _grams = new Dictionary<ulong, int>(GramComparer.Instance);
            var keptCount = new List<int>();
            var keptBytes = new List<int>();
            foreach (var kv in all1)
            {
                if ((kv.Key >> (16 * AlwaysGram)) != 0 && counts[kv.Value] < MinLongGramCount) continue;
                _grams.Add(kv.Key, keptCount.Count);
                keptCount.Add(counts[kv.Value]);
                keptBytes.Add(bytes[kv.Value]);
            }
            all1 = null;

            // Lists that would take more bytes than a bitmap over all items are stored as bitmaps: smaller,
            // O(1) membership tests when intersecting, and branch-free decoding.
            int gramCount = _grams.Count;
            int bitmapBytes = ((n + 63) >> 6) * 8;
            _postingCount = keptCount.ToArray();
            _postingStart = new int[gramCount + 1];
            _dense = new bool[gramCount];
            long total = 0;
            for (int g = 0; g < gramCount; g++)
            {
                _postingStart[g] = checked((int)total);
                _dense[g] = keptBytes[g] > bitmapBytes;
                total += _dense[g] ? bitmapBytes : keptBytes[g];
            }
            _postingStart[gramCount] = checked((int)total);
            counts = null; bytes = null; keptCount = null; keptBytes = null;

            // 3. Pass 2: write the posting lists.
            _postings = new byte[total];
            var cursor = new int[gramCount];
            Array.Copy(_postingStart, cursor, gramCount);
            last = null;
            var prev = new int[gramCount];
            Array.Fill(prev, -1);
            for (int i = 0; i < n; i++)
            {
                int s = _textStart[i], e = _textStart[i + 1];
                for (int p = s; p < e; p++)
                {
                    ulong key = 0;
                    for (int k = 1; k <= MaxGram && p + k <= e; k++)
                    {
                        char c = all[p + k - 1];
                        if (c == '\0') break;
                        key = (key << 16) | c;
                        if (!_grams.TryGetValue(key, out int id)) continue;
                        if (prev[id] == i) continue;
                        if (_dense[id]) _postings[_postingStart[id] + (i >> 3)] |= (byte)(1 << (i & 7));
                        else cursor[id] = WriteVar(_postings, cursor[id], (uint)(i - prev[id] - 1));
                        prev[id] = i;
                    }
                }
            }
            _grams.TrimExcess();

            // 4. Keep the text for verifying long queries, 1 byte per char when possible.
            if (latin1)
            {
                _text8 = new byte[all.Length];
                for (int i = 0; i < all.Length; i++) _text8[i] = (byte)all[i];
            }
            else
            {
                _text16 = all;
            }
        }

        /// <summary>Number of indexed items.</summary>
        public int Count => _items.Length;

        public ICollection<T> Search(string? search)
        {
            if (string.IsNullOrEmpty(search)) return Empty;

            Span<char> pattern = search.Length <= 256 ? stackalloc char[search.Length] : new char[search.Length];
            search.AsSpan().ToLowerInvariant(pattern);
            if (pattern.IndexOf('\0') >= 0) return Empty; // '\0' is the internal separator

            if (pattern.Length <= AlwaysGram)
                return TryGram(pattern, out int g) ? new PostingView(this, g) : (ICollection<T>)Empty;
            if (pattern.Length == MaxGram && TryGram(pattern, out int g4))
                return new PostingView(this, g4);

            // Every trigram of the pattern must exist; frequent 4-grams are used too when indexed.
            int m3 = pattern.Length - AlwaysGram + 1, m4 = pattern.Length - MaxGram + 1;
            Span<int> order = m3 + m4 <= 128 ? stackalloc int[m3 + m4] : new int[m3 + m4];
            int m = 0;
            for (int i = 0; i < m3; i++)
                if (!TryGram(pattern.Slice(i, AlwaysGram), out order[m++])) return Empty;
            for (int i = 0; i < m4; i++)
                if (TryGram(pattern.Slice(i, MaxGram), out order[m])) m++;
            order = order.Slice(0, m);

            // Rarest first.
            SortByCount(order);

            int[] rented = ArrayPool<int>.Shared.Rent(_postingCount[order[0]]);
            try
            {
                int c = Decode(order[0], rented);
                for (int j = 1; j < order.Length && c > VerifyThreshold; j++)
                {
                    if (order[j] == order[j - 1]) continue;
                    if (!_dense[order[j]] && _postingCount[order[j]] > (long)c * IntersectRatio) continue;
                    int before = c;
                    c = Intersect(order[j], rented, c);
                    if (c > before - (before >> 3)) break;  // < 12.5% pruned: verifying the rest is cheaper
                }
                if (c == 0) return Empty;

                // Verify candidates against the text (removes trigram false positives).
                int r = 0;
                if (_text8 != null)
                {
                    Span<byte> p8 = pattern.Length <= 256 ? stackalloc byte[pattern.Length] : new byte[pattern.Length];
                    for (int i = 0; i < pattern.Length; i++)
                    {
                        if (pattern[i] > 0xFF) return Empty;
                        p8[i] = (byte)pattern[i];
                    }
                    for (int i = 0; i < c; i++)
                    {
                        int id = rented[i];
                        if (Contains(_text8, _textStart[id], _textStart[id + 1], p8)) rented[r++] = id;
                    }
                }
                else
                {
                    for (int i = 0; i < c; i++)
                    {
                        int id = rented[i];
                        if (Contains(_text16!, _textStart[id], _textStart[id + 1], pattern)) rented[r++] = id;
                    }
                }
                if (r == 0) return Empty;

                var res = new T[r];
                for (int i = 0; i < r; i++) res[i] = _items[rented[i]];
                return res;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }

        // Scalar substring test: item texts are short, so this beats the vectorized IndexOf setup cost.
        private static bool Contains<TChar>(TChar[] text, int start, int end, ReadOnlySpan<TChar> p) where TChar : unmanaged, IEquatable<TChar>
        {
            TChar first = p[0];
            for (int i = start, last = end - p.Length; i <= last; i++)
            {
                if (!text[i].Equals(first)) continue;
                int k = 1;
                while (k < p.Length && text[i + k].Equals(p[k])) k++;
                if (k == p.Length) return true;
            }
            return false;
        }

        private bool TryGram(ReadOnlySpan<char> gram, out int id)
        {
            ulong key = 0;
            for (int i = 0; i < gram.Length; i++) key = (key << 16) | gram[i];
            return _grams.TryGetValue(key, out id);
        }

        private void SortByCount(Span<int> order)
        {
            for (int i = 1; i < order.Length; i++)
            {
                int x = order[i], cx = _postingCount[x], j = i - 1;
                while (j >= 0 && _postingCount[order[j]] > cx) { order[j + 1] = order[j]; j--; }
                order[j + 1] = x;
            }
        }

        private int Decode(int gram, int[] into)
        {
            var cursor = new Cursor(this, gram);
            return cursor.Fill(this, into);
        }

        // Keep only the candidates that also appear in the gram's list (both ascending).
        private int Intersect(int gram, int[] cand, int c)
        {
            int w = 0;
            if (_dense[gram])
            {
                int start = _postingStart[gram];
                var buf = _postings;
                for (int i = 0; i < c; i++)
                {
                    int id = cand[i];
                    if ((buf[start + (id >> 3)] & (1 << (id & 7))) != 0) cand[w++] = id;
                }
                return w;
            }

            int pos = _postingStart[gram], count = _postingCount[gram], cur = -1;
            var bytes = _postings;
            int ci = 0, next = cand[0];
            for (int i = 0; i < count; i++)
            {
                cur += 1 + (int)ReadVar(bytes, ref pos);
                if (cur < next) continue;
                while (next < cur)
                {
                    if (++ci == c) return w;
                    next = cand[ci];
                }
                if (next == cur)
                {
                    cand[w++] = cur;
                    if (++ci == c) return w;
                    next = cand[ci];
                }
            }
            return w;
        }

        /// <summary>Incremental decoder over one posting list (varint or bitmap), filling ids a block at a time.</summary>
        private struct Cursor
        {
            private readonly bool _dense;
            private readonly int _end;   // bitmap: end byte offset
            private int _pos;            // varint: byte offset; bitmap: offset of the next 64-bit word
            private int _left;           // ids not yet produced
            private int _id;             // varint: last id; bitmap: id of bit 0 of the current word
            private ulong _bits;         // bitmap: unread bits of the current word

            public Cursor(NGramSearch<T> owner, int gram)
            {
                _dense = owner._dense[gram];
                _pos = owner._postingStart[gram];
                _end = owner._postingStart[gram + 1];
                _left = owner._postingCount[gram];
                _id = _dense ? -64 : -1;
                _bits = 0;
            }

            public int Fill(NGramSearch<T> owner, Span<int> into)
            {
                int n = Math.Min(into.Length, _left);
                var buf = owner._postings;
                if (!_dense)
                {
                    int id = _id, pos = _pos;
                    for (int i = 0; i < n; i++)
                    {
                        id += 1 + (int)ReadVar(buf, ref pos);
                        into[i] = id;
                    }
                    _id = id; _pos = pos;
                }
                else
                {
                    int k = 0;
                    ulong bits = _bits;
                    while (k < n)
                    {
                        while (bits == 0)
                        {
                            _id += 64;
                            bits = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(_pos, 8));
                            _pos += 8;
                        }
                        into[k++] = _id + System.Numerics.BitOperations.TrailingZeroCount(bits);
                        bits &= bits - 1;
                    }
                    _bits = bits;
                }
                _left -= n;
                return n;
            }
        }

        private static int VarSize(uint v) => v < 0x80 ? 1 : v < 0x4000 ? 2 : v < 0x200000 ? 3 : v < 0x10000000 ? 4 : 5;

        private static int WriteVar(byte[] buf, int pos, uint v)
        {
            while (v >= 0x80) { buf[pos++] = (byte)(v | 0x80); v >>= 7; }
            buf[pos++] = (byte)v;
            return pos;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static uint ReadVar(byte[] buf, ref int pos)
        {
            uint b = buf[pos++];
            if (b < 0x80) return b;
            uint v = b & 0x7F;
            int shift = 7;
            while (true)
            {
                b = buf[pos++];
                v |= (b & 0x7F) << shift;
                if (b < 0x80) return v;
                shift += 7;
            }
        }

        // ulong.GetHashCode (hi ^ lo) collides heavily on packed 16-bit chars; mix the bits instead.
        private sealed class GramComparer : IEqualityComparer<ulong>
        {
            public static readonly GramComparer Instance = new GramComparer();
            public bool Equals(ulong x, ulong y) => x == y;
            public int GetHashCode(ulong x)
            {
                x *= 0x9E3779B97F4A7C15UL;
                return (int)(x >> 32) ^ (int)x;
            }
        }

        /// <summary>Read-only, lazily decoded view of one posting list.</summary>
        private sealed class PostingView : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly NGramSearch<T> _owner;
            private readonly int _gram;

            public PostingView(NGramSearch<T> owner, int gram) { _owner = owner; _gram = gram; }

            public int Count => _owner._postingCount[_gram];
            public bool IsReadOnly => true;

            public Enumerator GetEnumerator() => new Enumerator(_owner, _gram);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(T item)
            {
                var cmp = EqualityComparer<T>.Default;
                foreach (var x in this) if (cmp.Equals(x, item)) return true;
                return false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);
                if (arrayIndex < 0 || array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is too small.", nameof(array));
                var block = new int[Math.Min(Count, 256)];
                var cursor = new Cursor(_owner, _gram);
                var items = _owner._items;
                int n;
                while ((n = cursor.Fill(_owner, block)) > 0)
                    for (int i = 0; i < n; i++) array[arrayIndex++] = items[block[i]];
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            public struct Enumerator : IEnumerator<T>
            {
                private const int BlockSize = 256;
                private readonly NGramSearch<T> _owner;
                private readonly T[] _items;
                private readonly int[] _block;
                private Cursor _cursor;
                private int _index, _filled;

                internal Enumerator(NGramSearch<T> owner, int gram)
                {
                    _owner = owner;
                    _items = owner._items;
                    _block = new int[Math.Min(BlockSize, Math.Max(1, owner._postingCount[gram]))];
                    _cursor = new Cursor(owner, gram);
                    _index = _filled = 0;
                }

                // Read on demand rather than stored per MoveNext: storing a reference into the (usually boxed)
                // enumerator costs a GC write barrier per item.
                public T Current => _items[_block[_index - 1]];
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_index == _filled)
                    {
                        _filled = _cursor.Fill(_owner, _block);
                        _index = 0;
                        if (_filled == 0) return false;
                    }
                    _index++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
