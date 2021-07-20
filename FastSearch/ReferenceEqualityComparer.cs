using System;
using System.Collections.Generic;

namespace FastSearch
{
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
