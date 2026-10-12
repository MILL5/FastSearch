# NGramSearch — Detailed Specification

## Overview

`NGramSearch<T>` is FastSearch's recommended algorithm. It answers case-insensitive substring ("contains") queries over an in-memory collection, returning each matching input position once, in input order.

Instead of storing every possible answer (as the suffix trie in [CharSequenceSearch](CharSequenceSearch-Spec.md) does), it stores, for each short substring (an *n-gram*), the compressed list of items that contain it. Memory is O(N·L), about 7 bytes per indexed character on lists of 100k items or more, and the index never grows after construction.

| Property | Value |
|---|---|
| Memory | ~7 bytes per indexed character at 100k+ items (47.4 MiB for 1M items / 7.5M chars, excluding the input strings) |
| Build time | ~1.2 s for 1M items, single thread |
| Query, 1–3 chars | O(1) to return, O(hits) to enumerate; no sort, no de-dup, no cache |
| Query, 4+ chars | Intersect the rarest gram lists, then verify the survivors; typically < 1 µs for narrow queries |
| Result order | Input order; one result per matching input position |
| Thread safety | Immutable after construction; any number of concurrent `Search` calls |

---

## Data Structures

| Field | Type | Purpose | Size at 1M items |
|---|---|---|---|
| `_items` | `T[]` | The indexed items, by id (input position) | 1,000,000 refs |
| `_grams` | `Dictionary<ulong, int>` | Packed gram → gram id | 104,555 grams |
| `_postingStart` | `int[G + 1]` | Byte offset of each gram's list in `_postings` | 0.4 MB |
| `_postingCount` | `int[G]` | Number of items in each list (also the exact result `Count`) | 0.4 MB |
| `_dense` | `bool[G]` | Whether a list is stored as a bitmap | 0.1 MB |
| `_postings` | `byte[]` | All posting lists, back to back | 26.2 MB |
| `_text8` / `_text16` | `byte[]` / `char[]` | Lowercased text of every item, for verification | 7.5 MB (Latin-1) |
| `_textStart` | `int[N + 1]` | Start of each item's text | 4.0 MB |

### Gram keys

A gram of 1–4 characters is packed into a `ulong`, 16 bits per character: `key = (key << 16) | c`. Grams never contain `'\0'` (it's the separator between an item's index strings), so grams of different lengths can never collide. The dictionary uses a multiplicative-mixing comparer because `ulong.GetHashCode()` (`hi ^ lo`) collides heavily on packed 16-bit characters. Without it, the 1M-item build took 33 s instead of about 1 s.

### Which grams are indexed

- **Every** distinct 1-, 2- and 3-character gram gets a list.
- A 4-character gram gets a list only when **16 or more** items contain it (`MinLongGramCount`). Of 724k distinct 4-grams in the 1M password list, 94% occur in fewer than 16 items. Their directory entries (about 36 bytes each) would cost more than the 3-gram intersection they save.

### Posting list encoding

Each list holds the ascending ids of the items containing the gram, in one of two forms, whichever is smaller:

1. **Delta + varint (LEB128):** each entry stores `id − previousId − 1` in 1–5 bytes. Dense lists have small gaps, so most entries take 1 byte.
2. **Bitmap:** `ceil(N / 64) × 8` bytes, bit *i* set when item *i* contains the gram. Chosen when the varint form would be larger. In the 1M password list, 28 lists (common letters and digits) are bitmaps.

### Text copy

Each item's index strings are lowercased with `ToLowerInvariant` and concatenated, separated by `'\0'`. If every character is ≤ U+00FF, the text is stored as `byte[]` (1 byte per character); otherwise as `char[]`.

---

## Indexing Algorithm

### Input
- `IEnumerable<T> items`: the collection.
- `Func<T, string?[]>? indexFunc`: the strings to index per item (default `ToString()`). Null and empty strings are skipped.

### Process

1. **Text:** lowercase and concatenate every item's strings; record `_textStart`.
2. **Pass 1, size the lists:** for every position in every string, form the 1-, 2-, 3- and 4-grams that don't cross a `'\0'`. Look up or assign a gram id. Skip the gram if `last[id] == item` (the item already counted it). Otherwise add 1 to its count and the varint size of `item − last[id] − 1` to its byte size.
3. **Drop rare 4-grams** and build the final dictionary with dense ids.
4. **Lay out `_postings`:** a prefix sum over the list sizes, using the bitmap size for dense lists.
5. **Pass 2, write the lists:** repeat the traversal. Items are visited in input order, so every list is written already sorted, with no sort step.
6. **Keep the text** as `byte[]` or `char[]`.

### Complexity

- **Time:** O(N·L) dictionary operations, two passes.
- **Memory:** O(N·L). At most `3L + (frequent 4-grams)` entries per item, compressed to about 1–2 bytes each.
- **Peak transient memory:** the first-pass dictionary of all distinct 4-grams, about 724k entries for 1M passwords.

---

## Search Algorithm

### Input
`string? search`. Null or empty returns an empty collection. The query is lowercased into a stack buffer for queries of up to 256 characters. A query containing `'\0'` returns empty.

### Short queries (1–3 characters, or a frequent 4-gram)

Look up the gram and return a `PostingView`: a read-only `ICollection<T>` over the list.

- `Count` is `_postingCount[gram]`, exact and O(1).
- Enumeration decodes 256 ids at a time into a small buffer, using varint decoding or `TrailingZeroCount` over bitmap words.
- `Current` is read on demand rather than stored on each `MoveNext`. Storing a reference into the boxed enumerator costs a GC write barrier per item.
- `CopyTo` decodes straight into the destination array. `Add`, `Remove` and `Clear` throw `NotSupportedException`.

### Long queries (4+ characters)

1. Look up every 3-gram of the query. If any is missing, return empty, because an item containing the query would contain every one of its 3-grams. Also look up every 4-gram that is indexed.
2. Sort the gram ids by list length, rarest first.
3. Decode the rarest list into a pooled `int[]` of candidates.
4. While more than 16 candidates remain (`VerifyThreshold`), intersect with the next list:
   - **Bitmap list:** an O(1) bit test per candidate, so it's always worth doing.
   - **Varint list:** a linear merge, done only when the list is at most 8× the candidate count (`IntersectRatio`).
   - Stop early if an intersection removes less than 12.5% of the candidates, because verifying the rest is cheaper.
5. **Verify** each candidate with a scalar substring test over its text range. Item texts are short, so a simple first-character scan beats the setup cost of vectorized `IndexOf`. This step removes false positives where every gram appears but not contiguously (for example `xbcabcx` for the query `abcab`).
6. Map the surviving ids to items in a new `T[]`, already in input order.

### Complexity

- **Short queries:** O(1) to return; O(hits) to enumerate.
- **Long queries:** O(|rarest list| + Σ|intersected lists| + candidates × L).

---

## Measured Performance (1M passwords, .NET 10)

| Metric | NGramSearch | SuffixArraySearch | CharSequenceSearch |
|---|---:|---:|---:|
| Build | 1.2 s | 0.64 s | 12.1 s |
| Index memory | 47.4 MiB | 83.2 MiB | 1.54 GiB |
| After 1,000 queries | 47.4 MiB | 120.9 MiB | 1.54 GiB |
| Random 1–8 char query (avg, Search call) | 5.28 µs | 103 µs | 1.51 µs |
| `love` (3,297 hits, enumerated) | 13.7 µs | 6.23 µs | 13.1 µs |
| `catherine` (5 hits, enumerated) | 0.59 µs | 0.15 µs | 0.08 µs |
| `a` (370k hits, enumerated) | 1.6 ms | 0.70 ms | 1.5 ms |

See the [README](../README.md#performance--memory-v11-net-10) for the method and the full tables.

---

## Result and source semantics

- The constructor snapshots the input sequence and indexed text; later collection or property changes require a new index. Returned objects remain the original references.
- Repeated matches and multiple matching fields for one input position produce one result. If the same object appears at multiple input positions, it can appear multiple times.
- Null or empty indexed strings are skipped; null or empty queries return empty. A null input collection throws `ArgumentNullException`. Supply non-null items and an index function that returns a non-null array.
- Short-query posting views are read-only. Long queries return fresh arrays; changing their elements does not change the index. Index state is immutable after construction, allowing concurrent searches.
- Implementation: [NGramSearch.cs](../FastSearch/Algorithms/NGramSearch.cs).

## Known Trade-offs

- **Very broad results** (hundreds of thousands of hits) enumerate more slowly than `SuffixArraySearch`, which hands back a cached array. NGramSearch decodes on every enumeration by design, so it never needs a cache.
- **Narrow long queries** take about 1 µs, versus about 0.1 µs for the trie, which precomputes every answer at O(N·L²) memory.
- **`'\0'` in source strings** acts as a separator, and queries containing `'\0'` return empty.

---

## API Surface

```csharp
public sealed class NGramSearch<T> : ISearch<T> where T : class
{
    public NGramSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null);
    public int Count { get; }                       // number of indexed items
    public ICollection<T> Search(string? search);   // case-insensitive substring match
}
```
