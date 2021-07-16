using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FastSearch.UnitTests
{
    [TestClass]
    public class ContainsTests
    {
        private const int _numberOfTimes = 100000000;

        private CharSequenceSearch<string>.CharSequence GetCharacterSequence()
        {
            var cs = new CharSequenceSearch<string>.CharSequence('a');
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('a'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('b'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('c'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('d'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('e'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('f'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('g'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('h'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('i'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('j'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('k'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('l'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('m'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('n'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('o'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('p'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('q'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('r'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('s'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('t'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('u'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('v'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('w'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('x'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('y'));
            cs.NextCharacters.Add(new CharSequenceSearch<string>.CharSequence('z'));

            return cs;
        }

        [TestMethod]
        public void TimeForContains()
        {
            var cs = GetCharacterSequence();
            var z = cs.NextCharacters.Single(x => x.Character == 'z');

            for (int i = 0; i < _numberOfTimes; i++)
            {
                if (!cs.NextCharacters.Contains(z))
                {
                    throw new Exception("WAT!");
                }
            }
        }

        [TestMethod]
        public void TimeForReferenceEquality()
        {
            var cs = GetCharacterSequence();
            var z = cs.NextCharacters.Single(x => x.Character == 'z');

            for (int i = 0; i < _numberOfTimes; i++)
            {
                bool found = false;

                for (int j = 0; j < cs.NextCharacters.Count; j++)
                {
                    if (ReferenceEquals(cs.NextCharacters[j], z))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception("WAT!");
                }
            }
        }

        [TestMethod]
        public void TimeForSingleOrDefault()
        {
            var cs = GetCharacterSequence();
            var z = cs.NextCharacters.Single(x => x.Character == 'z');

            for (int i = 0; i < _numberOfTimes; i++)
            {
                if (cs.NextCharacters.SingleOrDefault(x => x.Character == z.Character) == null)
                {
                    throw new Exception("WAT!");
                }
            }
        }

        [TestMethod]
        public void TimeForFind()
        {
            var cs = GetCharacterSequence();
            var z = 'z';

            for (int i = 0; i < _numberOfTimes; i++)
            {
                CharSequenceSearch<string>.CharSequence found = null;

                for (int j = 0; j < cs.NextCharacters.Count; j++)
                {
                    if (cs.NextCharacters[j].Character == z)
                    {
                        found = cs.NextCharacters[j];
                        break;
                    }
                }

                if (found == null)
                {
                    throw new Exception("WAT!");
                }
            }
        }

        public class MyObject1
        {
            private readonly string _phrase;

            public MyObject1(string phrase)
            {
                _phrase = phrase;
            }
        }

        public class MyObject2
        {
            private readonly string _phrase;

            public MyObject2(string phrase)
            {
                _phrase = phrase;
            }

            public override bool Equals(object obj)
            {
                if (obj is MyObject2 other)
                {
                    return _phrase.Equals(other._phrase, StringComparison.Ordinal);
                }

                return false;
            }
            public override int GetHashCode()
            {
                return _phrase.GetHashCode();
            }
        }

        [TestMethod]
        public void Intersect()
        {
            List<MyObject2> list1 = new List<MyObject2>();
            List<MyObject2> list2 = new List<MyObject2>();

            var mo1 = new MyObject2("hello");
            var mo2 = new MyObject2("world");
            var mo3 = new MyObject2("hello");

            list1.AddRange(new[] { mo1, mo2, mo3 });
            list2.AddRange(new[] { mo1, mo2, mo3 });

            var result = list1.Intersect(list2).ToList();

            Assert.AreEqual(result.Count, list1.Count);
        }
    }
}
