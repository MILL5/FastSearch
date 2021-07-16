using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private ISearch<string> _linqSearch;
        private ISearch<string> _betterLinqSearch;
        private ISearch<string> _mapReduceSearch;
        private ISearch<string> _hashSearch;
        private ISearch<string> _charSequenceSearch;

        public MainViewModel()
        {
            LoadFileCommand = new RelayCommand(LoadFileCommandMethod);
            SearchCommand = new RelayCommand(SearchCommandMethod);
        }

        public ICommand LoadFileCommand { get; }
        public ICommand SearchCommand { get; }

        private void LoadFileCommandMethod()
        {
            Task.Run(LoadFileAsync);
        }

        private void SearchCommandMethod()
        {
            Task.Run(SearchAsync);
        }

        private async Task SearchAsync()
        {
            const int searchLimit = 100000;
            const int reductionFactor = 100;

            var searchForThis = SearchPhrase;

            if (UseLinq)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using LINQ", reductionFactor))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < (searchLimit / reductionFactor); i++)
                    {
                        result = _linqSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            if (UseBetterLinq)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using Better LINQ", reductionFactor))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < (searchLimit / reductionFactor); i++)
                    {
                        result = _betterLinqSearch.Search(searchForThis);
                    }

                    _sb.Insert(0, $"Found {result.Count} results\r\n");
                }

                RaisePropertyChanged(() => Output);
            }

            if (UseMapReduce)
            {
                using (new TimerScope(_sb, $"Search for {searchForThis} {searchLimit} times using MapReduce", reductionFactor))
                {
                    ICollection<string> result = null;

                    for (int i = 0; i < (searchLimit / reductionFactor); i++)
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
            var limit = Limit;
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

            using (new TimerScope(_sb, "LINQ index"))
            {
                _linqSearch = new LinqSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, "Better LINQ index"))
            {
                _betterLinqSearch = new BetterLinqSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, "Map Reduce index"))
            {
                _mapReduceSearch = new MapReduceSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, "Hash index"))
            {
                _hashSearch = new HashSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            using (new TimerScope(_sb, "Character Tree index"))
            {
                _charSequenceSearch = new CharSequenceSearch<string>(passwords);
            }

            RaisePropertyChanged(() => Output);

            _passwords = passwords;
        }

        public string SearchPhrase { get; set; } = "catherine";

        public bool UseLinq { get; set; }
        public bool UseBetterLinq { get; set; }
        public bool UseMapReduce { get; set; } = true;
        public bool UseHash { get; set; }
        public bool UseCharSequence { get; set; }

        public int Limit { get; set; } = 100000;
        public string Output => _sb.ToString();
    }
}
