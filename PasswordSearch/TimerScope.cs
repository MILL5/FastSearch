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

        public TimerScope(StringBuilder stringBuilder, string message)
        {
            _sb = stringBuilder;
            _m = message;

            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Dispose()
        {
            _sw.Stop();
            _sb.Insert(0, $"{_m} took {_sw.ElapsedMilliseconds}ms\r\n");
        }
    }
}
