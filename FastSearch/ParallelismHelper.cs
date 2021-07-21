using System;
using System.Threading.Tasks;

namespace FastSearch
{
    internal class ParallelismHelper
    {
        public static ParallelOptions Options
        {
            get
            {
                int maxDegreeOfParallelism = MaxDegreeOfParallelism;

                return new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            }
        }

        public static int MaxDegreeOfParallelism
        {
            get
            {
                int maxCpusToUse = Convert.ToInt32(Math.Floor(Environment.ProcessorCount * 2.0 / 3.0));
                int minParallelism = maxCpusToUse > 1 ? 2 : 1;

                return Math.Max(maxCpusToUse, minParallelism);
            }
        }
    }
}
