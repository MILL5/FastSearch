# HashSearch — Specification

`HashSearch<T>` implements case-insensitive substring lookup by storing every distinct substring as a hash-indexed value with item postings. `T` must be a reference type.

## API

```csharp
public HashSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null,
    IParallelism? parallelism = null);
public ICollection<T> Search(string? search);
```

A null collection throws `ArgumentNullException`. The default selector uses `ToString()` and default parallelism comes from `Parallelism`. Supply non-null items and a selector returning a non-null array.

## Index construction

The constructor processes source occurrences in parallel. Each non-null field is lowercased with `ToLowerInvariant()`. Nested loops materialize every non-empty substring and compute its hash. A concurrent dictionary groups substrings by hash; a bucket stores separate value entries, so hash collisions do not combine different strings. Bucket locks protect value creation and postings updates.

A per-occurrence `HashSet<ValueEntry>` using reference equality prevents repeated additions for overlapping matches and multiple fields. After construction the hash buckets are sorted by signed hash value into a list.

## Queries and results

Queries are lowercased and hashed. Binary search finds the hash bucket, then a linear ordinal-ignore-case value comparison finds the matching substring. Successful queries return a read-only wrapper over that value's postings.

- Null or empty queries and misses return a shared mutable empty list; treat it as read-only.
- Null fields are skipped; empty fields generate no substrings.
- An occurrence appears once per value, even across multiple matching fields. Repeating the same reference as separate source occurrences can repeat it in results. Different objects with equal text remain separate.
- Parallel insertion determines postings order; input ordering is not guaranteed.
- Indexed text is a construction-time snapshot; collection or field changes require rebuilding. Results retain original object references.
- Completed index state is only read by queries, allowing concurrent searches. Do not mutate shared empty results.

## Costs and use

A field of length L generates L(L+1)/2 substring occurrences. Copying and hashing those strings can process O(L³) characters during construction. Stored distinct substring text can also require cubic character storage in the worst case, alongside postings and bucket overhead. Shared substrings reduce retained storage but do not remove generation work.

Query work includes O(k) normalization/hashing for query length k, O(log H) bucket search for H distinct hashes, and comparison within the collision bucket. A successful result wrapper avoids copying postings; enumeration costs O(result count). This approach spends substantial build time and memory to precompute substring answers.

Implementation: [HashSearch.cs](../FastSearch/Algorithms/HashSearch.cs) and [bucket binary search](../FastSearch/Algorithms/HashSearchExtensions.cs). See [benchmark method and measurements](../README.md#performance--memory-v11-net-10).
