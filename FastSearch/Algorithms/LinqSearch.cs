using Pineapple.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pineapple.Common.Preconditions;

namespace FastSearch
{
    public class LinqSearch<T> : ISearch<T> where T : class
    {
        private static readonly List<T> Empty = new();

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

        private readonly IEnumerable<ObjectWrapper> _items;
        private readonly IParallelism _parallelism;

        private static string[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public LinqSearch(IEnumerable<T> items, Func<T, string[]> indexFunc = null, IParallelism parallelism = null)
        {
            CheckIsNotNull(nameof(items), items);

            var indexWithThis = indexFunc ?? IndexThis;
            _parallelism = parallelism ?? new Parallelism();

            var temp = new List<ObjectWrapper>(items.Count());

            foreach (var item in items)
            {
                foreach (var s in indexWithThis(item))
                {
                    temp.Add(new ObjectWrapper(item, s));
                }
            }

            _items = temp;
        }

        public ICollection<T> Search(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }

            var searchToUse = search.ToLowerInvariant();

            return _items
                .AsParallel()
                .WithDegreeOfParallelism(_parallelism.MaxDegreeOfParallelism)
                .Where(x => x.ToString().Contains(searchToUse, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Instance)
                .ToList();
        }
    }
}
