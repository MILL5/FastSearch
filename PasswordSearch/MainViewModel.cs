using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FastSearch;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace PasswordSearch
{
    public class MainViewModel : ViewModelBase
    {
        private readonly StringBuilder _sb = new();
        private volatile ISearch<string> _stringSearch;
        private volatile ISearch<string> _linqSearch;
        private volatile ISearch<string> _hashSearch;
        private volatile ISearch<string> _charSequenceSearch;

        public MainViewModel()
        {
            LoadFileCommand = new RelayCommand(LoadFileCommandMethod);
            SearchCommand = new RelayCommand(SearchCommandMethod, CanExecuteSearch);
        }

        public RelayCommand LoadFileCommand { get; }
        public RelayCommand SearchCommand { get; }

        private void LoadFileCommandMethod()
        {
            Task.Run(LoadFileAsync);
        }

        private void SearchCommandMethod()
        {
            Task.Run(SearchAsync);
        }

        private bool CanExecuteSearch()
        {
            return _stringSearch != null &&
                   _charSequenceSearch != null &&
                   _hashSearch != null &&
                   _linqSearch != null;
        }

        private async Task SearchAsync()
        {
            int searchLimit = NumberOfSearches;
            var searchForThis = SearchPhrase;

            StringBuilder lastlog = null;

            for (int i = 0; i < 1; i++)
            {
                lastlog = Search(searchLimit, searchForThis);
            }

            _sb.Insert(0, lastlog);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RaisePropertyChanged(() => Output);
                SearchCommand.RaiseCanExecuteChanged();
            });
        }

        private StringBuilder Search(int searchLimit, string searchForThis)
        {
            int linqResult = 0,
                stringResult = 0,
                hashResult = 0,
                charSequenceResult = 0;

            var sb = new StringBuilder();

            using (new TimerScope(sb, $"Search for {searchForThis} {searchLimit} times using String Contains"))
            {
                for (int i = 0; i < searchLimit; i++)
                {
                    stringResult = _stringSearch.Search(searchForThis).Count;
                }
            }

            using (new TimerScope(sb, $"Search for {searchForThis} {searchLimit} times using LINQ"))
            {
                for (int i = 0; i < searchLimit; i++)
                {
                    linqResult = _linqSearch.Search(searchForThis).Count;
                }
            }

            using (new TimerScope(sb, $"Search for {searchForThis} {searchLimit} times using Hash"))
            {
                for (int i = 0; i < searchLimit; i++)
                {
                    hashResult = _hashSearch.Search(searchForThis).Count;
                }
            }

            using (new TimerScope(sb, $"Search for {searchForThis} {searchLimit} times using Character Tree"))
            {
                for (int i = 0; i < searchLimit; i++)
                {
                    charSequenceResult = _charSequenceSearch.Search(searchForThis).Count;
                }
            }

            if (linqResult != stringResult &&
                linqResult != hashResult &&
                linqResult != charSequenceResult)
            {
                sb.Clear();
                sb.Insert(0, $"Validation for results failed.");
            }

            return sb;
        }

        private async Task LoadFileAsync()
        {
            var limit = NumberOfEntries;
            var passwords = new List<string>(10000000);

            using (new TimerScope(_sb, "Load 10M passwords"))
            {
                using (var s = new StreamReader(@"10-million-password-list-top-1000000.txt"))
                {
                    while (s.Peek() > 0)
                    {
                        var line = await s.ReadLineAsync();

                        if (!string.IsNullOrWhiteSpace(line))
                            passwords.Add(line);
                    }
                }
            }

            passwords = passwords.Take(limit).ToList();

            StringBuilder lastlog = null;
            
            for (int i = 0; i < 6; i++)
            {
                lastlog = Index(limit, passwords);
            }

            _sb.Insert(0, lastlog);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RaisePropertyChanged(() => Output);
                SearchCommand.RaiseCanExecuteChanged();
            });
        }

        private StringBuilder Index(int limit, List<string> passwords)
        {
            var sb = new StringBuilder();

            using (new TimerScope(sb, $"String indexed {limit}"))
            {
                _stringSearch = new StringSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"LINQ indexed {limit}"))
            {
                _linqSearch = new LinqSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"Hash indexed {limit}"))
            {
                _hashSearch = new HashSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"Character Tree indexed {limit}"))
            {
                _charSequenceSearch = new CharSequenceSearch<string>(passwords);
            }

            return sb;
        }

        public string SearchPhrase { get; set; } = "catherine";

        public bool UseString { get; set; }
        public bool UseLinq { get; set; }
        public bool UseHash { get; set; }
        public bool UseCharSequence { get; set; }

        public int NumberOfEntries { get; set; } = 10000;
        public int NumberOfSearches { get; set; } = 10000;
        public string Output => _sb.ToString();
    }
}
