using System;

namespace FastSearch
{
    internal static class ObjectExtensions
    {
        public static string ToLowerInvariant(this object o)
        {
            return o.ToString()?.ToLowerInvariant() ?? string.Empty;
        }
    }
}
