using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordSearch.Benchmark
{
    public class BenchmarkShared
    {
        private const int NumberOfEntries = 10000;
        private static readonly List<string> _passwords;

        static BenchmarkShared()
        {
            var passwords = new List<string>(10000000);

            using (var s = new StreamReader(@"10-million-password-list-top-1000000.txt"))
            {
                while (s.Peek() > 0)
                {
                    var line = s.ReadLine();

                    if (!string.IsNullOrWhiteSpace(line))
                        passwords.Add(line);
                }
            }

            _passwords = passwords.Take(NumberOfEntries).ToList();
        }

        public static void Initialize()
        {
        }

        public static List<string> Passwords => _passwords;
    }
}
