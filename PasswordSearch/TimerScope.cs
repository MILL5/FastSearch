using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordSearch
{
    public class TimerScope : IDisposable
    {
        private readonly Stopwatch _sw;
        private readonly StringBuilder _sb;
        private readonly string _m;
        private readonly int _reductionFactor;

        public TimerScope(StringBuilder stringBuilder, string message, int reductionFactor = 1)
        {
            _sb = stringBuilder;
            _m = message;
            _reductionFactor = reductionFactor;

            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Dispose()
        {
            _sw.Stop();

            if (_reductionFactor == 1)
                _sb.Insert(0, $"{_m} took {_sw.ElapsedMilliseconds}ms\r\n");
            else
                _sb.Insert(0, $"{_m} took {_sw.ElapsedMilliseconds * _reductionFactor}ms\r\n");
        }
    }
}
