using System;
using System.Collections.Generic;
using System.Text;

namespace FastSearch
{
    internal static class HashSearchExtensions
    {
        public static HashSearch<T>.HashIndexEntry Find<T>(this IList<HashSearch<T>.HashIndexEntry> hashIndices, int hashCode) where T : class
        {
            int count = hashIndices.Count;

            int min = 0;
            int max = count - 1;

            while (min <= max)
            {
                int mid = (min + max) / 2;

                if (hashCode == hashIndices[mid].Hash)
                {
                    return hashIndices[mid];
                }
                else if (hashCode < hashIndices[mid].Hash)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            return null;
        }
    }
}
