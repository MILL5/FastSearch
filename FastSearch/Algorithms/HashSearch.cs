using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Pineapple.Common.Preconditions;
using static FastSearch.ParallelismHelper;

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

        internal class ObjectWrapperList : List<ObjectWrapper>
        {
            private List<T> _instances;

            public ObjectWrapperList()
            {
            }

            public void Materialize()
            {
                Instances = this.Select(x => x.Instance).ToList();
            }

            public List<T> Instances
            { 
                get
                {
                    if (_instances == null)
                    {
                        Materialize();
                    }

                    return _instances;
                }
                private set
                {
                    _instances = value;
                }
            }
        }

        private IDictionary<int, ObjectWrapperList> _rootmap;
        private static readonly List<T> Empty = new();

        private static string[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public HashSearch(IEnumerable<T> items, Func<T, string[]> indexFunc = null, int? maxDegreeOfParallelism = null)
        {
            CheckIsNotNull(nameof(items), items);

            var indexWithThis = indexFunc ?? IndexThis;

            BuildIndex(items, indexWithThis, maxDegreeOfParallelism);
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
                return found.Instances;
            }

            return Empty;
        }

        private void BuildIndex(IEnumerable<T> items, Func<T, string[]> indexWithThis, int? maxDegreeOfParallelism)
        {
            int degreeOfParallelism = GetMaxDegreeOfParallelism(maxDegreeOfParallelism);
            var options = GetOptions(maxDegreeOfParallelism);

            var map = new ConcurrentDictionary<int, ObjectWrapperList>(degreeOfParallelism, 50);

            _ = Parallel.ForEach(items, options, (item) =>
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
                                  return new ObjectWrapperList();
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

            foreach (var item in map)
            {
                item.Value.Materialize();
            }

            _rootmap = map;
        }
    }
}
