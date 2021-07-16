using System;
using System.Collections.Generic;
using System.Linq;
using static Pineapple.Common.Preconditions;

namespace FastSearch
{
    internal class LinqSearch<T> : ISearch<T> where T : class
    {
        private static readonly List<T> Empty = new();
        private readonly IEnumerable<T> _items;

        public LinqSearch(IEnumerable<T> items)
        {
            CheckIsNotNull(nameof(items), items);

            _items = items;
        }

        public ICollection<T> Search(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }
            
            var searchToUse = search.ToLowerInvariant();

            return _items.Where(x => x.ToLowerInvariant().Contains(searchToUse, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
