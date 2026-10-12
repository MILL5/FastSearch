# CharSequenceSearch — Specification

`CharSequenceSearch<T>` implements case-insensitive substring search with a suffix trie. It precomputes the item list for each substring path, trading substantial index memory for fast queries. `T` must be a reference type.

## API

```csharp
public CharSequenceSearch(IEnumerable<T> items,
    Func<T, string?[]>? indexFunc = null,
    IParallelism? parallelism = null, bool enableExactMatch = false);
public ICollection<T> Search(string? search);
```

A null collection throws `ArgumentNullException`. The default index function uses `ToString()`. Supply non-null items and a function returning a non-null array. `IParallelism` controls the parallel constructor build.

## Index construction

Each non-null field is lowercased with `ToLowerInvariant()`. In substring mode, every starting position creates a trie path through the remaining characters. For `cat`, the paths are `cat`, `at` and `t`; the item is associated with every node on each path.

Roots are stored in a concurrent character dictionary. Each node contains a character, a `List<CharSequence>` of children and a `List<T>` of matching items. Child lookup scans the child list linearly. A per-input-item `HashSet<CharSequence>` with reference equality prevents repeated additions to the same node across fields and suffixes. Locks protect root postings and child creation/postings during construction.

Exact mode inserts only the path starting at position zero. It stores prefix candidates, then filters query candidates against whole field values with `StringComparison.OrdinalIgnoreCase`. This is whole-string equality.

## Queries and results

Queries are lowercased, then walk from the root through successive child lists. A missing character returns empty. Substring queries return a read-only wrapper over the terminal node's postings without copying. Exact queries invoke the index function again and return a new filtered list.

- Null or empty queries and misses return a shared empty array that rejects collection mutation.
- Null fields are skipped. Empty fields contribute no nodes in substring mode. **In exact mode an empty indexed field currently throws `IndexOutOfRangeException` during construction**; omit empty fields when using that mode.
- Each input occurrence contributes at most once to a matching node. Repeating the same object in the source can repeat it in results. Distinct objects with identical text remain separate results.
- Parallel build insertion order determines result order; input ordering is not guaranteed.
- The index snapshots field text at construction and retains original object references. Source collection changes require rebuilding. Exact filtering rereads fields, but its candidate trie remains the original snapshot, so property changes can make results inconsistent until rebuilt.
- Normal queries read a completed index and may run concurrently. Exact-mode callers must also make their index function and objects safe for concurrent reads.

## Costs and trade-offs

For a field of length L, substring indexing visits L(L+1)/2 nodes; exact indexing visits L nodes. With maximum child count C, linear child lookup can multiply traversal cost by C. Trie nodes and item postings can grow quadratically with total field lengths. Per-item visited sets make posting deduplication average O(1), avoiding scans of the growing postings lists.

A query of length k costs O(k × C), plus O(k) normalization; substring results wrap the existing list and enumeration costs O(result count). Exact mode additionally reads and compares the candidate fields and allocates a result list. Memory includes mutable trie nodes, list capacity and postings even though the index is read-only after construction. Parallel builds can contend on popular root paths.

Implementation: [CharSequenceSearch.cs](../FastSearch/Algorithms/CharSequenceSearch.cs). See the [benchmark method and measurements](../README.md#performance--memory-v11-net-10), [NGramSearch](NGramSearch-Spec.md) and [SuffixArraySearch](SuffixArraySearch-Spec.md).
