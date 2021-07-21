using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Pineapple.Common.Preconditions;

namespace FastSearch
{
    public class CharSequenceSearch<T> : ISearch<T> where T : class
    {
        public class CharSequence
        {
            public CharSequence(char character)
            {
                Character = character;
                NextCharacters = new List<CharSequence>();
                Items = new List<T>();
            }

            public char Character { get; }

            public CharSequence FindNextCharacter(char character)
            {
                for (int i = 0; i < NextCharacters.Count; i++)
                {
                    var nextCharacter = NextCharacters[i];

                    if (nextCharacter.Character == character)
                    {
                        return nextCharacter;
                    }
                }

                return null;
            }

            public List<CharSequence> NextCharacters { get; }

            public List<T> Items { get; }

            public override bool Equals(object obj)
            {
                return obj is CharSequence other && Character == other.Character;
            }

            public override int GetHashCode()
            {
                return Character.GetHashCode();
            }
        }

        private static readonly List<T> Empty = new();
        private IDictionary<char, CharSequence> _rootmap;

        private static string IndexThis(T instance)
        {
            return instance.ToString();
        }

        public CharSequenceSearch(IEnumerable<T> items, Func<T, string> indexFunc = null)
        {
            CheckIsNotNull(nameof(items), items);

            var indexWithThis = indexFunc ?? IndexThis;

            BuildIndex(items, indexWithThis);
        }

        public ICollection<T> Search(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Empty;
            }

            var searchToUse = search.ToLowerInvariant();
            var nextChar = searchToUse[0];

            if (_rootmap.TryGetValue(nextChar, out var current))
            {
                for (var i = 1; i < searchToUse.Length; i++)
                {
                    current = current!.FindNextCharacter(searchToUse[i]);

                    if (current == null)
                    {
                        return Empty;
                    }
                }
            }
            else
            {
                return Empty;
            }

            return current.Items;
        }

        private void BuildIndex(IEnumerable<T> items, Func<T, string> indexWithThis)
        {
            var map = new ConcurrentDictionary<char, CharSequence>(ParallelismHelper.MaxDegreeOfParallelism,
                                                                   50);

            _ = Parallel.ForEach(items, ParallelismHelper.Options, (item) =>
              {
                  var value = indexWithThis(item)
                      .ToLowerInvariant()
                      .ToArray();

                  for (int i = 0; i < value.Length; i++)
                  {
                      int startIndex = i;
                      char currentChar = value[startIndex];

                      CharSequence currentSequence;

                      currentSequence = map.GetOrAdd(currentChar, (c) =>
                      {
                          return new CharSequence(c);
                      });

                      var lockObject = currentSequence;

                      lock (lockObject)
                      {
                          if (!currentSequence.Items.Contains(item))
                          {
                              currentSequence.Items.Add(item);
                          }
                      }

                      for (int j = i + 1; j < value.Length; j++)
                      {
                          var nextChar = value[j];

                          lock (lockObject.NextCharacters)
                          {
                              var nextSequence = currentSequence.FindNextCharacter(nextChar);

                              if (nextSequence == null)
                              {
                                  nextSequence = new CharSequence(nextChar);
                                  currentSequence.NextCharacters.Add(nextSequence);
                              }

                              nextSequence.Items.Add(item);
                              currentSequence = nextSequence;
                          }
                      }
                  };
              });

            _rootmap = map;
        }
    }
}
