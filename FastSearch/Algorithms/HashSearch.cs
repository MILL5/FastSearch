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

            public ObjectWrapper(T instance)
            {
                Instance = instance;
                _s = instance.ToLowerInvariant();
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

        private IDictionary<int, List<ObjectWrapper>> _rootmap = new Dictionary<int, List<ObjectWrapper>>();
        private static readonly List<T> Empty = new();

        public HashSearch(IEnumerable<T> items)
        {
            CheckIsNotNull(nameof(items), items);

            BuildIndex(items);
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
                return found
                        .Where(x => string.CompareOrdinal(x.ToString(),searchToUse) == 0)
                        .Select(x => x.Instance)
                        .ToList();
            }

            return Empty;
        }

        private void BuildIndex(IEnumerable<T> items)
        {
            var map = new ConcurrentDictionary<int, List<ObjectWrapper>>(Environment.ProcessorCount, 50);

            Parallel.ForEach(items, (item) =>
            {
                var value = new ObjectWrapper(item);
                var valueAsString = value.ToString();

                for (int i = 0; i < valueAsString.Length; i++)
                {
                    var hashCode = valueAsString[i..].GetHashCode();

                    var list = map.GetOrAdd(hashCode, (hashCode) =>
                    {
                        return new List<ObjectWrapper>();
                    });

                    lock (list)
                    {
                        list.Add(value);
                    }
                };
            });

            _rootmap = map;
        }
    }
}
