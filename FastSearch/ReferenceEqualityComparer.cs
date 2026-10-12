using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FastSearch
{
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            // Identity hash: must not depend on overridden Equals/GetHashCode.
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
