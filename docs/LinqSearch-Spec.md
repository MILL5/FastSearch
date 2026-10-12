# LinqSearch — Specification

`LinqSearch<T>` implements case-insensitive substring scanning with PLINQ. It snapshots searchable field text at construction and uses parallel query execution. `T` must be a reference type.

## API

```csharp
public LinqSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null,
    IParallelism? parallelism = null);
public ICollection<T> Search(string? search);
```

A null collection throws `ArgumentNullException`. The default selector uses `ToString()`; default parallelism comes from `Parallelism`. Supply non-null items and a selector returning a non-null array.

## Construction and queries

Construction calls `Count()` for capacity, enumerates the input and creates one wrapper for every non-null field. Each wrapper stores the original item reference and an invariant-lowercased string. Empty fields are retained. A non-collection source may be enumerated twice and must be repeatable.

A non-empty query is lowercased, then runs `AsParallel().WithDegreeOfParallelism(...)`. Each wrapper is tested with ordinal-ignore-case `Contains`, and matching original references are collected into a new list.

- PLINQ is unordered here; source result order is not guaranteed.
- There is no deduplication: two matching fields on one item produce two results, and repeated source occurrences also remain separate.
- Null or empty queries return a shared empty array that rejects collection mutation. Misses return a new mutable list. Null fields are skipped; empty fields cannot match a non-empty query.
- Collection and text changes after construction do not change matching. Returned objects are the original references, so their current properties may differ from indexed text. Rebuild to refresh.
- The wrapper list is read-only after construction. Concurrent searches require safe access to the parallelism configuration.

## Costs and use

Construction copies and normalizes total field text and allocates one wrapper per non-null field. Memory is proportional to that text plus wrappers and references. Every query scans all wrappers; parallel scheduling and result materialization add overhead. Multiple fields increase both work and possible result size. This is useful as a parallel-scan comparison when building a substring index would be too expensive.

Implementation: [LinqSearch.cs](../FastSearch/Algorithms/LinqSearch.cs). See [benchmark method and measurements](../README.md#performance--memory-v11-net-10).
