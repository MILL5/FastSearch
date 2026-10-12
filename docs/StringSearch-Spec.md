# StringSearch — Specification

`StringSearch<T>` is the sequential case-insensitive substring scanner and the consistency-test oracle. It implements `ISearch<T>` for reference types without building an index.

## API

```csharp
public StringSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null);
public ICollection<T> Search(string? search);
```

A null collection throws `ArgumentNullException`. The default selector uses `ToString()`. Supply non-null items and a selector returning a non-null array.

## Query execution and results

The constructor retains the supplied sequence and selector. Every non-empty query calls `Count()` for result capacity, lowercases the query with `ToLowerInvariant()`, then enumerates the source. Each selected non-null field is tested with `Contains(..., StringComparison.OrdinalIgnoreCase)`. The first matching field adds the item and stops field scanning for that occurrence.

- Results are new mutable lists in source enumeration order.
- Multiple matching fields add an occurrence once; repeated source occurrences can produce repeated references. Equal text on different objects does not collapse results.
- Null and empty queries return an empty list, matching the other five algorithms. Null fields are ignored.
- The source and selector are live: later sequence or property changes affect subsequent queries. `Count()` may enumerate a non-collection source before the search enumeration; use a repeatable source.
- Concurrent calls require safe source enumeration and a thread-safe selector. Concurrent source mutation is unsupported unless the source itself supports it.

## Costs and use

Construction stores references with constant overhead. Search scans the source and field text; cost depends on the runtime substring search and total text examined. The returned list reserves capacity for the entire source, so each non-empty query can allocate O(N) result storage even for few matches. A non-collection source also incurs the preliminary O(N) count pass. This suits small collections and data that changes frequently.

Implementation: [StringSearch.cs](../FastSearch/Algorithms/StringSearch.cs). See [benchmark method and measurements](../README.md#performance--memory-v11-net-10).
