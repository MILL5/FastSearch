using System.Collections.Generic;

namespace FastSearch
{
    public interface ISearch<T> where T : class
    {
        ICollection<T> Search(string search);
    }
}