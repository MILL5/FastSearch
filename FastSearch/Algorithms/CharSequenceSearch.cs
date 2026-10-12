using Pineapple.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FastSearch
{
    public class CharSequenceSearch<T> : ISearch<T> where T : class
    {
        public class CharSequence
        {
            public CharSequence(char character)
            {
                Character = character;
                NextCharacters = new List<CharSequence>();
                Items = new List<T>();
            }

            public char Character { get; }

            public CharSequence? FindNextCharacter(char character)
            {
                for (int i = 0; i < NextCharacters.Count; i++)
                {
                    var nextCharacter = NextCharacters[i];

                    if (nextCharacter.Character == character)
                    {
                        return nextCharacter;
                    }
                }

                return null;
            }

            public List<CharSequence> NextCharacters { get; }

            public List<T> Items { get; }

            public override bool Equals(object? obj)
            {
                return obj is CharSequence other && Character == other.Character;
            }

            public override int GetHashCode()
            {
                return Character.GetHashCode();
            }
        }

        private static readonly T[] Empty = Array.Empty<T>();
        private IDictionary<char, CharSequence> _rootmap = null!; // assigned by BuildIndex
        private readonly bool _enableExactMatch;
        private readonly Func<T, string?[]> _indexWithThis;

        private static string?[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public CharSequenceSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null, IParallelism? parallelism = null, bool enableExactMatch = false)
        {
            ArgumentNullException.ThrowIfNull(items);
            
            _enableExactMatch = enableExactMatch;

            _indexWithThis = indexFunc ?? IndexThis;

            parallelism ??= new Parallelism();

            BuildIndex(items, _indexWithThis, parallelism, _enableExactMatch);
        }

        public ICollection<T> Search(string? search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }

            var searchToUse = search.ToLowerInvariant();
            var nextChar = searchToUse[0];

            if (_rootmap.TryGetValue(nextChar, out var current))
            {
                for (var i = 1; i < searchToUse.Length; i++)
                {
                    current = current!.FindNextCharacter(searchToUse[i]);

                    if (current == null)
                    {
                        return Empty;
                    }
                }
            }
            else
            {
                return Empty;
            }

            // Exact match compares case-insensitively (the index is lowercased; the source strings are not).
            return _enableExactMatch
                ? current.Items.Where(x => _indexWithThis(x).Any(s => string.Equals(s, search, StringComparison.OrdinalIgnoreCase))).ToList()
                : current.Items.AsReadOnly(); // never hand out the index's own mutable list
        }

        private void BuildIndex(IEnumerable<T> items, Func<T, string?[]> indexWithThis, IParallelism parallelism, bool enableExactMatch)
        {
            var map = new ConcurrentDictionary<char, CharSequence>(parallelism.MaxDegreeOfParallelism,
                                                                   50);

            _ = Parallel.ForEach(items, parallelism.Options, (item) =>
              {
                  var values = indexWithThis(item);

                  // Nodes this item has already been added to. Item-local, so O(1) dedupe without
                  // scanning node.Items (which made the build quadratic in the number of items).
                  var visited = new HashSet<CharSequence>(new ReferenceEqualityComparer<CharSequence>());

                  foreach (var s in values)
                  {
                      if (s is null) continue;

                      var value = s.ToLowerInvariant()
                                   .ToArray();

                      var endIndex = enableExactMatch ? 1 : value.Length;

                      for (int i = 0; i < endIndex; i++)
                      {
                          int startIndex = i;
                          char currentChar = value[startIndex];

                          CharSequence currentSequence;

                          currentSequence = map.GetOrAdd(currentChar, (c) =>
                          {
                              return new CharSequence(c);
                          });

                          var lockObject = currentSequence;

                          lock (lockObject)
                          {
                              if (visited.Add(currentSequence))
                              {
                                  currentSequence.Items.Add(item);
                              }
                          }

                          for (int j = i + 1; j < value.Length; j++)
                          {
                              var nextChar = value[j];

                              lock (lockObject.NextCharacters)
                              {
                                  var nextSequence = currentSequence.FindNextCharacter(nextChar);

                                  if (nextSequence == null)
                                  {
                                      nextSequence = new CharSequence(nextChar);
                                      currentSequence.NextCharacters.Add(nextSequence);
                                  }

                                  if (visited.Add(nextSequence))
                                  {
                                      nextSequence.Items.Add(item);
                                  }

                                  currentSequence = nextSequence;
                              }
                          }
                      }
                  }
              });

            _rootmap = map;
        }
    }
}
