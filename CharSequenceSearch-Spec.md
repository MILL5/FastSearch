# CharSequenceSearch — Detailed Specification

## Overview

`CharSequenceSearch<T>` is a generic, case-insensitive substring search algorithm that builds a suffix trie at index time to enable fast lookups at query time. It implements `ISearch<T>` and is designed for the type-ahead/autocomplete use case: index once, search many times.

---

## Data Structures

### CharSequence (Trie Node)

Each node in the trie represents a single character and holds:

| Field | Type | Purpose |
|---|---|---|
| `Character` | `char` | The character this node represents |
| `NextCharacters` | `List<CharSequence>` | Child nodes (next characters in the sequence) |
| `Items` | `List<T>` | All items whose indexed string contains the substring represented by the path from root to this node |

### Root Map

- Type: `IDictionary<char, CharSequence>` (built as `ConcurrentDictionary` during indexing)
- Maps each starting character to its root trie node
- Serves as the entry point for all searches

---

## Indexing Algorithm

### Input
- `IEnumerable<T> items` — the collection to index
- `Func<T, string[]> indexFunc` — optional function that returns the strings to index for each item (defaults to `ToString()`)
- `IParallelism parallelism` — controls degree of parallelism
- `bool enableExactMatch` — when true, only indexes from position 0 (prefix-only matching)

### Process

For each item, in parallel:

1. Retrieve the string representations via `indexFunc(item)`.
2. Lowercase each string and convert to a `char[]`.
3. Determine the suffix iteration range:
   - **Substring mode** (`enableExactMatch = false`): iterate `i` from `0` to `value.Length - 1` (all suffixes).
   - **Exact match mode** (`enableExactMatch = true`): iterate `i` only at `0` (prefix only).
4. For each starting position `i`:
   - Look up or create the root node for `value[i]` in the `ConcurrentDictionary`.
   - Add the item to that root node's `Items` list (if not already present).
   - Walk through subsequent characters `value[i+1]` through `value[length-1]`:
     - Find or create a child `CharSequence` node for the next character.
     - Add the item to that child node's `Items` list (if not already present via `ReferenceEquals`).

### Concurrency

- `ConcurrentDictionary.GetOrAdd` provides thread-safe root node creation.
- `lock(lockObject)` protects the root node's `Items` list.
- `lock(lockObject.NextCharacters)` protects child node creation and child `Items` updates.

### Indexing Example

Given the string `"cat"`, the following trie paths are created (substring mode):

```
Suffix "cat":  root['c'] → 'a' → 't'     (item added at each node)
Suffix "at":   root['a'] → 't'            (item added at each node)
Suffix "t":    root['t']                   (item added at node)
```

### Complexity

- **Time**: O(N * L²) where N = number of items, L = average string length
  - For each item: L suffixes, each suffix walks up to L characters
- **Space**: O(N * L²) nodes in the worst case (no shared prefixes), but significant sharing reduces this in practice

---

## Search Algorithm

### Input
- `string search` — the query string

### Process

1. If `search` is null or empty, return an empty list.
2. Lowercase the search string: `search.ToLowerInvariant()`.
3. Look up the first character in `_rootmap`.
   - If not found, return empty.
4. Walk the trie: for each subsequent character in the search string, call `FindNextCharacter(char)` on the current node.
   - If any character is not found, return empty.
5. Return the `Items` list from the final node reached.
   - In exact match mode, additionally filter items by checking `indexFunc(item).Contains(searchToUse)`.

### Complexity

- **Time**: O(k * C) where k = search string length, C = average number of children per node
  - The `FindNextCharacter` method performs a linear scan of `NextCharacters`
  - With the current `List<CharSequence>` implementation, C could be up to 36+ (a-z, 0-9, symbols)
- **Space**: O(1) additional (returns a reference to the existing `Items` list, no copying)

---

## Current Performance Bottlenecks

### 1. Linear Child Lookup (Highest Impact)

**Location**: `CharSequence.FindNextCharacter()` (line 24-37)

**Problem**: `NextCharacters` is a `List<CharSequence>` scanned linearly. This is the hot path — called once per character of every search query. For a node with 36 children (a-z, 0-9), this is 36 comparisons per character in the worst case.

**Impact**: Degrades search from O(k) to O(k * C) where C = children per node.

### 2. List-Based Item Deduplication During Build

**Location**: `BuildIndex()` lines 139 and 159-161

**Problem**: Checking `currentSequence.Items.Contains(item)` is O(n) on a `List<T>`. The `SingleOrDefault(x => ReferenceEquals(x, item))` scan on child items is also O(n). Both are inside the inner indexing loop.

**Impact**: Indexing time grows quadratically with the number of items that share common substrings.

### 3. Per-Search String Allocation

**Location**: `Search()` line 84

**Problem**: `search.ToLowerInvariant()` allocates a new string on every call. In high-throughput type-ahead scenarios, this creates GC pressure.

### 4. Over-Allocated Collections

**Problem**: After indexing completes, `List<CharSequence>` and `List<T>` retain excess capacity (List doubles capacity on growth). The data structure is read-only after build, but pays for mutable overhead.

### 5. Lock Contention During Parallel Build

**Location**: `BuildIndex()` lines 137, 149

**Problem**: Coarse-grained locking on `lockObject` and `lockObject.NextCharacters` serializes threads that hit the same trie node. Popular characters (e.g., 'e', 'a') become contention hotspots.

---

## Proposed Improvements

### Improvement 1: O(1) Child Lookup via Dictionary

**Change**: Replace `List<CharSequence> NextCharacters` with `Dictionary<char, CharSequence>`.

**Rationale**: Eliminates the linear scan in `FindNextCharacter()`. Every character step in a search becomes O(1) amortized. This is the single highest-impact change because it directly affects every search query on every character.

**Alternative**: For purely lowercase alphanumeric input, a fixed `CharSequence[36]` array with direct index mapping (`'a'=0, ..., 'z'=25, '0'=26, ..., '9'=35`) would give true O(1) with no hashing overhead and better cache locality. However, `Dictionary` is more general and handles any Unicode input.

**Search changes**:
```csharp
// Before
public CharSequence FindNextCharacter(char character)
{
    for (int i = 0; i < NextCharacters.Count; i++)
    {
        if (NextCharacters[i].Character == character)
            return NextCharacters[i];
    }
    return null;
}

// After
public CharSequence FindNextCharacter(char character)
{
    NextCharacters.TryGetValue(character, out var result);
    return result;
}
```

**Expected impact**: Search performance improves from O(k * C) to O(k). Most significant when nodes have many children (root-level and second-level nodes).

---

### Improvement 2: HashSet for Item Deduplication During Build

**Change**: Use a `HashSet<T>` with `ReferenceEqualityComparer<T>` during indexing to track which items have been added to each node, instead of `List.Contains()` and `SingleOrDefault()` with `ReferenceEquals`.

**Rationale**: Deduplication checks drop from O(n) to O(1). After indexing completes, convert the `HashSet` to an array for the read-only search phase.

**Build changes**:
```csharp
// Before: O(n) per check
if (!currentSequence.Items.Contains(item)) ...
var exists = nextSequence.Items.SingleOrDefault(x => ReferenceEquals(x, item));

// After: O(1) per check
if (currentSequence.ItemSet.Add(item)) ...
```

**Expected impact**: Significantly faster indexing for datasets with many shared substrings.

---

### Improvement 3: Post-Build Compaction

**Change**: After `BuildIndex` completes, walk the trie and:
- Convert all `List<T>` (or `HashSet<T>`) item collections to `T[]` arrays.
- Convert all `Dictionary<char, CharSequence>` to `FrozenDictionary<char, CharSequence>` (.NET 8+) or trimmed arrays.

**Rationale**: Arrays use less memory (no slack capacity, no internal bookkeeping), have better cache locality, and signal immutability. `FrozenDictionary` is optimized for read-heavy workloads.

**Expected impact**: 20-40% memory reduction depending on dataset characteristics. Slight search speed improvement from cache effects.

---

### Improvement 4: Stack-Allocated Search Lowering

**Change**: For search strings under 128 characters, use `Span<char>` with `stackalloc` to avoid the `ToLowerInvariant()` heap allocation.

```csharp
public ICollection<T> Search(string search)
{
    if (string.IsNullOrEmpty(search))
        return Empty;

    Span<char> buffer = search.Length <= 128
        ? stackalloc char[search.Length]
        : new char[search.Length];

    search.AsSpan().ToLowerInvariant(buffer);

    // walk trie using buffer...
}
```

**Rationale**: Eliminates a string allocation per search call. In type-ahead scenarios with thousands of searches per second, this reduces GC pressure measurably.

**Expected impact**: Eliminates one allocation per search. Measurable under high-throughput workloads.

---

### Improvement 5: Reduced Lock Contention with ConcurrentDictionary Children

**Change**: Replace `List<CharSequence>` children with `ConcurrentDictionary<char, CharSequence>` during the build phase. This eliminates the need for `lock(lockObject.NextCharacters)` since `GetOrAdd` handles thread-safe insertion.

Item lists still need synchronization, but can use a lighter-weight mechanism like `lock` on the individual `Items` list rather than the parent's `NextCharacters`.

**Rationale**: The current design locks on `NextCharacters` for the entire inner loop iteration (finding/creating child + adding item). With `ConcurrentDictionary`, child lookup/creation is lock-free, and only item insertion needs synchronization.

**Expected impact**: Better parallel indexing throughput, especially for datasets where many items share common prefixes (e.g., dictionary words).

---

## Implementation Priority

| Priority | Improvement | Effort | Impact |
|---|---|---|---|
| 1 | O(1) child lookup (Dictionary) | Low | High — directly speeds up every search |
| 2 | HashSet deduplication during build | Low | High — directly speeds up indexing |
| 3 | Post-build compaction | Medium | Medium — reduces memory footprint |
| 4 | Stack-allocated search lowering | Low | Low-Medium — reduces GC pressure |
| 5 | ConcurrentDictionary children | Medium | Medium — improves parallel indexing |

---

## API Surface (Unchanged)

The public API remains identical. All improvements are internal to the implementation:

```csharp
public class CharSequenceSearch<T> : ISearch<T> where T : class
{
    // Constructor: builds the index
    public CharSequenceSearch(
        IEnumerable<T> items,
        Func<T, string[]> indexFunc = null,
        IParallelism parallelism = null,
        bool enableExactMatch = false);

    // Search: returns matching items
    public ICollection<T> Search(string search);
}
```

No breaking changes. All improvements are drop-in replacements for existing behavior.
