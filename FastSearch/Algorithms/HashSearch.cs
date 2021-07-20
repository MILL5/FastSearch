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
        internal class ObjectWrapper
        {
            private readonly string _s;

            public ObjectWrapper(T instance, string value)
            {
                Instance = instance;
                _s = value.ToLowerInvariant();
            }

            public T Instance { get; }

            public override bool Equals(object obj)
            {
                return obj is ObjectWrapper other && _s == other._s;
            }

            public override int GetHashCode()
            {
                return _s.GetHashCode();
            }

            public override string ToString()
            {
                return _s;
            }
        }

        private IDictionary<int, List<ObjectWrapper>> _rootmap;
        private static readonly List<T> Empty = new();

        private static string[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public HashSearch(IEnumerable<T> items, Func<T, string[]> indexFunc = null)
        {
            CheckIsNotNull(nameof(items), items);

            var indexWithThis = indexFunc ?? IndexThis;

            BuildIndex(items, indexWithThis);
        }

        public ICollection<T> Search(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }

            var searchToUse = search.ToLowerInvariant();
            var hashCodeToUse = searchToUse.GetHashCode();

            if (_rootmap.TryGetValue(hashCodeToUse, out var found))
            {
                var equalityComparer = new ReferenceEqualityComparer<T>();

                return found
                    .Where(x => x.ToString().Contains(searchToUse, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Instance)
                    .Distinct(equalityComparer)
                    .ToList();
            }

            return Empty;
        }

        private void BuildIndex(IEnumerable<T> items, Func<T, string[]> indexWithThis)
        {
            var map = new ConcurrentDictionary<int, List<ObjectWrapper>>(Environment.ProcessorCount, 50);

            _ = Parallel.ForEach(items, (item) =>
              {
                  foreach (var s in indexWithThis(item))
                  {
                      var value = new ObjectWrapper(item, s);
                      var valueAsString = value.ToString();

                      for (int i = 0; i < valueAsString.Length; i++)
                      {
                          var valueToIndex = valueAsString[i..];

                          for (int j = 0; j < valueToIndex.Length; j++)
                          {
                              var valueToAdd = valueToIndex.Substring(0, j + 1);
                              var hashCodeToAdd = valueToAdd.GetHashCode();

                              var list = map.GetOrAdd(hashCodeToAdd, (hc) =>
                              {
                                  return new List<ObjectWrapper>();
                              });

                              lock (list)
                              {
                                  var found = list.Where(x => ReferenceEquals(x.Instance, value.Instance))
                                                  .SingleOrDefault();

                                  if (found == null)
                                      list.Add(value);
                              }
                          }
                      };
                  }
              });

            _rootmap = map;
        }
    }
}
