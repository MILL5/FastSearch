# SuffixArraySearch — Specification

`SuffixArraySearch<T>` implements case-insensitive substring search with a generalized suffix array, short-prefix tables and a broad-query result cache. `T` must be a reference type.

## API

```csharp
public SuffixArraySearch(IEnumerable<T> items,
    Func<T, string?[]>? indexFunc = null, int cacheThreshold = 64);
public int Count { get; }
public ICollection<T> Search(string? search);
```

A null collection throws `ArgumentNullException`. The default selector uses `ToString()`. Supply non-null items and a selector returning a non-null array. `Count` includes every input position, including positions without searchable fields. `cacheThreshold` is the suffix-hit threshold for caching, rather than the number of distinct returned items.

## Index construction

The constructor snapshots the input sequence. Non-null, non-empty fields are invariant-lowercased and concatenated with a `\0` terminator per field. Every character position becomes a suffix entry, associated with its owning input position.

Suffixes are sorted first by a packed four-character key; equal-key runs without a terminator are refined by comparing the remaining characters. Three dictionaries then map each one-, two- and three-character prefix to its exact contiguous suffix-array range.

## Queries and results

Queries are lowercased into a stack buffer up to 256 characters, or a heap buffer for longer queries. A prefix-table lookup gives the candidate range. Queries longer than three characters perform lower/upper binary bounds within their three-character bucket, skipping the already matched prefix.

Matching suffix owners are collected by sorting and deduplicating small owner lists, or by marking a bitmap for larger ranges. Both paths return items in input position order.

- Null or empty queries return an empty array. Null and empty fields are skipped.
- Repeated suffix hits and multiple matching fields collapse to one result per input position. The same reference supplied at multiple positions can appear multiple times; equal text on distinct objects remains separate.
- Indexed text and source positions are snapshots. Later collection or property changes require rebuilding. Results retain original object references.
- `\0` is reserved as a field terminator; source text containing it is outside the normal searchable-text contract. Queries containing it have no supported matching guarantee.
- Uncached results are fresh arrays. Cached results use read-only wrappers, keeping callers from changing cached array elements.
- Concurrent searches are supported through read-only index state, a concurrent cache and synchronized eviction, insertion and reference accounting. Do not mutate returned item properties concurrently without application-level coordination.

## Cache and costs

At or above `cacheThreshold` suffix hits, queries check a normalized-key cache. Cached result collections are reused. The cache uses a reference-count budget equal to the suffix count and clears when accounting exceeds it. This is a reset policy rather than LRU eviction or a total-memory limit; keys and wrappers add overhead. Eviction, insertion and reference accounting are synchronized so retained cached item references stay within the budget under concurrent queries.

Retained text, suffix positions and owners grow linearly with indexed characters, plus input references and prefix dictionaries. Construction sorts S suffixes in O(S log S) comparisons; tied-prefix comparisons can inspect the remaining field length. Temporary keys and owner arrays increase peak build memory.

Short-query range lookup is expected O(1) after O(k) normalization. Longer queries need O(log B) comparisons within bucket size B, each potentially comparing the remaining query characters. Collection cost depends on suffix hits: small lists sort/deduplicate; broad lists use an input-size bitmap. Cached hits reuse the collection, while enumeration still costs O(result count).

Implementation: [SuffixArraySearch.cs](../FastSearch/Algorithms/SuffixArraySearch.cs). See [benchmark method and measurements](../README.md#performance--memory-v11-net-10) and [NGramSearch](NGramSearch-Spec.md).
