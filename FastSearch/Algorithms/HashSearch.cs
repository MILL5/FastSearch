﻿using Pineapple.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Pineapple.Common.Preconditions;

namespace FastSearch
{
    public class HashSearch<T> : ISearch<T> where T : class
    {
        private const int DefaultCapacity = 1;
        
        internal class HashIndexEntry
        {
            public HashIndexEntry(int hash)
            {
                Hash = hash;
                Items = new List<ValueEntry>(DefaultCapacity);
            }

            public int Hash { get; }

            public List<ValueEntry> Items { get; }
        }

        internal class ValueEntry
        {
            public ValueEntry(string value)
            {
                Value = value;
                Items = new List<T>(DefaultCapacity);
            }

            public string Value { get; }
            public List<T> Items { get; }
        }

        internal IList<HashIndexEntry> _rootmap;

        private static readonly List<T> Empty = new();

        private static string[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public HashSearch(IEnumerable<T> items, Func<T, string[]> indexFunc = null, IParallelism parallelism = null)
        {
            CheckIsNotNull(nameof(items), items);

            var indexWithThis = indexFunc ?? IndexThis;
            parallelism ??= new Parallelism();

            BuildIndex(items, indexWithThis, parallelism);
        }

        public ICollection<T> Search(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }

            var searchToUse = search.ToLowerInvariant();
            var hashCodeToUse = searchToUse.GetHashCode();

            HashIndexEntry hashIndexEntry = _rootmap.Find(hashCodeToUse);

            if (hashIndexEntry != null)
            {
                // We can user FirstOrDefault here because we guarantee there are no collisions
                var valueEntry = hashIndexEntry.Items.FirstOrDefault(x => x.Value.Equals(searchToUse, StringComparison.OrdinalIgnoreCase));
                
                if (valueEntry != null)
                {
                    return valueEntry.Items;
                }
            }
             
            return Empty;
        }

        private void BuildIndex(IEnumerable<T> items, Func<T, string[]> indexWithThis, IParallelism parallelism)
        {
            var map = new ConcurrentDictionary<int, HashIndexEntry>(parallelism.MaxDegreeOfParallelism, 50);

            // For each item ...
            _ = Parallel.ForEach(items, parallelism.Options, (item) =>
              {
                  // We can have multiple strings that represent an item
                  foreach (var s in indexWithThis(item))
                  {
                      // We are case insensitive
                      var valueAsString = s.ToLowerInvariant();

                      for (int i = 0; i < valueAsString.Length; i++)
                      {
                          var valueToIndex = valueAsString[i..];

                          for (int j = 0; j < valueToIndex.Length; j++)
                          {
                              var valueToAdd = valueToIndex.Substring(0, j + 1);
                              var hashCodeToAdd = valueToAdd.GetHashCode();

                              var hashIndexEntry = map.GetOrAdd(hashCodeToAdd, (hc) =>
                              {
                                  return new HashIndexEntry(hc);
                              });

                              lock (hashIndexEntry)
                              {
                                  ValueEntry valueEntry = null;

                                  foreach (var entry in hashIndexEntry.Items)
                                  {
                                      if (entry.Value == valueToAdd)
                                      {
                                          valueEntry = entry;
                                          break;
                                      }
                                  }

                                  if (valueEntry == null)
                                  {
                                      valueEntry = new ValueEntry(valueToAdd);
                                      hashIndexEntry.Items.Add(valueEntry);
                                  }

                                  var found = valueEntry.Items
                                                .SingleOrDefault(x => ReferenceEquals(x, item));

                                  if (found == null)
                                      valueEntry.Items.Add(item);
                              }
                          }
                      };
                  }
              });

            _rootmap = map.Values.OrderBy(x => x.Hash).ToList();
        }
    }
}