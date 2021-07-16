﻿using System;
using System.Collections.Generic;
using System.Linq;
using static Pineapple.Common.Preconditions;

namespace FastSearch
{
    internal class BetterLinqSearch<T> : ISearch<T> where T : class
    {
        private static readonly List<T> Empty = new();

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

        private readonly IEnumerable<T> _items;

        public BetterLinqSearch(IEnumerable<T> items)
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

            return _items.Where(x => x.ToString().Contains(searchToUse, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
