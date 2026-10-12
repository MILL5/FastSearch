using System;
using System.Collections.Generic;
using System.Linq;

namespace FastSearch
{
    public class StringSearch<T> : ISearch<T> where T : class
    {
        private readonly IEnumerable<T> _items;
        private readonly Func<T, string?[]> _indexWithThis;

        private static string?[] IndexThis(T instance)
        {
            return new[] { instance.ToString() };
        }

        public StringSearch(IEnumerable<T> items, Func<T, string?[]>? indexFunc = null)
        {
            ArgumentNullException.ThrowIfNull(items);

            _items = items;
            _indexWithThis = indexFunc ?? IndexThis;
        }

        public ICollection<T> Search(string? search)
        {
            if (search is null) return new List<T>();

            var items = _items;
            var found = new List<T>(items.Count());

            var searchForThis = search.ToLowerInvariant();

            foreach (var item in items)
            {
                foreach (var s in _indexWithThis(item))
                {
                    if (s is not null && s.Contains(searchForThis, StringComparison.OrdinalIgnoreCase))
                    {
                        found.Add(item);
                        break;
                    }
                }
            }

            return found;
        }
    }
}
