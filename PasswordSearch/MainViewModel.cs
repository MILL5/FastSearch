using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FastSearch;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace PasswordSearch
{
    public class MainViewModel : ViewModelBase
    {
        private readonly StringBuilder _sb = new();
        private IEnumerable<string> _passwords;
        private volatile ISearch<string> _linqSearch;
        private volatile ISearch<string> _betterLinqSearch;
        private volatile ISearch<string> _mapReduceSearch;
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
            return _betterLinqSearch != null &&
                   _charSequenceSearch != null &&
                   _hashSearch != null &&
                   _linqSearch != null &&
                   _mapReduceSearch != null;
        }

        private async Task SearchAsync()
        {
            int searchLimit = NumberOfSearches;
            var searchForThis = SearchPhrase;

            if (UseLinq)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using LINQ"))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < searchLimit; i++)
                    {
                        result = _linqSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            if (UseBetterLinq)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using Better LINQ"))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < searchLimit; i++)
                    {
                        result = _betterLinqSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            if (UseMapReduce)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using MapReduce"))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < searchLimit; i++)
                    {
                        result = _mapReduceSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            if (UseHash)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using Hash"))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < searchLimit; i++)
                    {
                        result = _hashSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            if (UseCharSequence)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using Character Tree"))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < searchLimit; i++)
                    {
                        result = _charSequenceSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            await Task.CompletedTask;
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

            using (new TimerScope(_sb, $"LINQ indexed {limit}"))
            {
                _linqSearch = new LinqSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, $"Better LINQ indexed {limit}"))
            {
                _betterLinqSearch = new BetterLinqSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, $"Map Reduce indexed {limit}"))
            {
                _mapReduceSearch = new MapReduceSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, $"Hash indexed {limit}"))
            {
                _hashSearch = new HashSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, $"Character Tree indexed {limit}"))
            {
                _charSequenceSearch = new CharSequenceSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            _passwords = passwords;

            Application.Current.Dispatcher.Invoke(() =>
            {
                SearchCommand.RaiseCanExecuteChanged();
            });
        }

        public string SearchPhrase { get; set; } = "catherine";

        public bool UseLinq { get; set; }
        public bool UseBetterLinq { get; set; }
        public bool UseMapReduce { get; set; } = true;
        public bool UseHash { get; set; }
        public bool UseCharSequence { get; set; }

        public int NumberOfEntries { get; set; } = 10000;
        public int NumberOfSearches { get; set; } = 10000;
        public string Output => _sb.ToString();
    }
}
