﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Pineapple.Common.Preconditions;

namespace FastSearch
{
    internal class MapReduceSearch<T> : ISearch<T> where T : class
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

            public override string ToString()
            {
                return _s;
            }
        }

        private static readonly List<T> Empty = new();
        private IDictionary<char, List<ObjectWrapper>> _map = new Dictionary<char, List<ObjectWrapper>>();

        public MapReduceSearch(IEnumerable<T> items)
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

            var searchToUse = search.ToLowerInvariant().Distinct();

            var listsToReduce = new List<List<ObjectWrapper>>(_map.Count);

            var result = Parallel.ForEach(searchToUse, (c,s) =>
            {
                if (_map.TryGetValue(c, out var foundList))
                {
                    lock (listsToReduce)
                    {
                        listsToReduce.Add(foundList);
                    }
                }
                else
                {
                    s.Break();
                }
            });

            if ((!result.IsCompleted) || (listsToReduce.Count == 0))
            {
                return Empty;
            }

            listsToReduce = listsToReduce.AsParallel().OrderBy(x => x.Count).ToList();

            List<ObjectWrapper> combinedList = listsToReduce[0];

            for (int i = 1; i < listsToReduce.Count; i++)
            {
                combinedList = combinedList.Intersect(listsToReduce[i]).ToList();
            }

            return combinedList
                .AsParallel()
                .Where(x => x.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Instance)
                .ToList();
        }

        private void BuildIndex(IEnumerable<T> items)
        {
            var map = new Dictionary<char, List<ObjectWrapper>>();

            foreach (var item in items)
            {
                var o = new ObjectWrapper(item);

                var value = o.ToString()
                    .Distinct()
                    .ToList();

                foreach (var c in value)
                {
                    if (!map.ContainsKey(c))
                    {
                        map.Add(c, new List<ObjectWrapper>());
                    }

                    var list = map[c];
                    list.Add(o);
                }
            }

            _map = map;
        }
    }
}
