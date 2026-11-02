using Pineapple.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastSearch
{
    public class LinqSearch<T> : ISearch<T> where T : class
    {
        private static readonly T[] Empty = Array.Empty<T>();

        internal class ObjectWrapper
        {
            private readonly string _s;

            public ObjectWrapper(T instance, string value, int occurrenceId)
            {
                Instance = instance;
                OccurrenceId = occurrenceId;
                _s = value.ToLowerInvariant();
            }

            public T Instance { get; }
            public int OccurrenceId { get; }

            public override bool Equals(object? obj)
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
        private readonly bool _hasMultipleFields;

        private static string?[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public LinqSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null, IParallelism? parallelism = null)
        {
            ArgumentNullException.ThrowIfNull(items);

            var indexWithThis = indexFunc ?? IndexThis;
            _parallelism = parallelism ?? new Parallelism();

            var temp = new List<ObjectWrapper>(items.Count());
            var occurrenceId = 0;

            foreach (var item in items)
            {
                var fieldCount = 0;
                foreach (var s in indexWithThis(item))
                {
                    if (s is null) continue;
                    temp.Add(new ObjectWrapper(item, s, occurrenceId));
                    fieldCount++;
                }

                _hasMultipleFields |= fieldCount > 1;
                occurrenceId++;
            }

            _items = temp;
        }

        public ICollection<T> Search(string? search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }

            var searchToUse = search.ToLowerInvariant();

            var matches = _items
                .AsParallel()
                .WithDegreeOfParallelism(_parallelism.MaxDegreeOfParallelism)
                .Where(x => x.ToString().Contains(searchToUse, StringComparison.OrdinalIgnoreCase));

            if (_hasMultipleFields)
            {
                // Deduplicate fields, preserving repeated occurrences and value-equal instances.
                matches = matches.GroupBy(x => x.OccurrenceId).Select(group => group.First());
            }

            return matches
                .Select(x => x.Instance)
                .ToList();
        }
    }
}
