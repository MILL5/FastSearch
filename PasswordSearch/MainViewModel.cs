using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastSearch;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PasswordSearch
{
    public class MainViewModel : ObservableObject
    {
        private readonly StringBuilder _sb = new();
        private volatile ISearch<string> _stringSearch;
        private volatile ISearch<string> _linqSearch;
        private volatile ISearch<string> _hashSearch;
        private volatile ISearch<string> _charSequenceSearch;
        private volatile ISearch<string> _suffixArraySearch;
        private volatile ISearch<string> _nGramSearch;

        public MainViewModel()
        {
            LoadFileCommand = new RelayCommand(LoadFileCommandMethod, () => !IsBusy);
            SearchCommand = new RelayCommand(SearchCommandMethod, CanExecuteSearch);
        }

        public RelayCommand LoadFileCommand { get; }
        public RelayCommand SearchCommand { get; }

        private void LoadFileCommandMethod()
        {
            if (IsBusy) return;
            if (!int.TryParse(NumberOfEntriesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit) ||
                limit < 1 || limit > 1000000)
            {
                Status = "Enter a dataset size from 1 to 1,000,000.";
                return;
            }
            _ = RunOperationAsync(() => LoadFileAsync(limit), "Loading and indexing dataset…", "Dataset ready. Run a search to measure performance.");
        }

        private void SearchCommandMethod()
        {
            if (!CanExecuteSearch()) return;
            var phrase = SearchPhrase;
            var repetitions = NumberOfSearches;
            if (string.IsNullOrWhiteSpace(phrase) || repetitions <= 0)
            {
                Status = "Enter a search phrase and a positive number of repetitions.";
                return;
            }
            var (search, algorithm) = UseString ? (_stringSearch, "String Contains") :
                UseLinq ? (_linqSearch, "LINQ") :
                UseHash ? (_hashSearch, "Hash") :
                UseCharSequence ? (_charSequenceSearch, "Character Sequence") :
                UseSuffixArray ? (_suffixArraySearch, "Suffix Array") :
                (_nGramSearch, "N-Gram");
            var oracle = _stringSearch;
            _ = RunOperationAsync(() => SearchAsync(repetitions, phrase, search, algorithm, oracle),
                "Running search benchmark…", "Search complete. Results appear below.");
        }

        private bool _isBusy;
        private string _status = "Load a dataset to get started.";

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(IsIdle));
            }
        }

        public bool IsIdle => !IsBusy;

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        private async Task RunOperationAsync(Func<Task> operation, string runningStatus, string completedStatus)
        {
            IsBusy = true;
            Status = runningStatus;
            LoadFileCommand.NotifyCanExecuteChanged();
            SearchCommand.NotifyCanExecuteChanged();
            try
            {
                await Task.Run(operation);
                Status = completedStatus;
            }
            catch (Exception ex)
            {
                Status = "Operation failed. Check the results and try again.";
                _sb.Insert(0, $"{ex.Message}{Environment.NewLine}");
                OnPropertyChanged(nameof(Output));
            }
            finally
            {
                IsBusy = false;
                LoadFileCommand.NotifyCanExecuteChanged();
                SearchCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecuteSearch()
        {
            return !IsBusy && _stringSearch != null &&
                   _charSequenceSearch != null &&
                   _hashSearch != null &&
                   _linqSearch != null &&
                   _suffixArraySearch != null &&
                   _nGramSearch != null;
        }

        private async Task SearchAsync(int searchLimit, string searchForThis, ISearch<string> search,
            string algorithm, ISearch<string> oracle)
        {
            _sb.Insert(0, Search(searchLimit, searchForThis, search, algorithm, oracle));
            await MainThread.InvokeOnMainThreadAsync(() => OnPropertyChanged(nameof(Output)));
        }

        private StringBuilder Search(int searchLimit, string searchForThis, ISearch<string> search,
            string algorithm, ISearch<string> oracle)
        {
            int result = 0;

            var sb = new StringBuilder();

            using (new TimerScope(sb, $"Search for {searchForThis} {searchLimit} times using {algorithm}"))
            {
                for (int i = 0; i < searchLimit; i++)
                {
                    result = search.Search(searchForThis).Count;
                }
            }
            // Validate and report counts outside the selected implementation's timed loop.
            var expected = oracle.Search(searchForThis).Count;
            sb.AppendLine($"Actual results: {result}; oracle results: {expected}");
            if (result != expected)
            {
                sb.AppendLine("Validation for results failed.");
                throw new InvalidOperationException(sb.ToString());
            }

            return sb;
        }

        private async Task LoadFileAsync(int limit)
        {
            var passwords = new List<string>(limit);
            var loadLog = new StringBuilder();
            using (new TimerScope(loadLog, "Load packaged passwords"))
            {
                // The list ships inside the app package (MauiAsset).
                using var reader = new StreamReader(await FileSystem.OpenAppPackageFileAsync("10-million-password-list-top-1000000.txt"));
                string line;
                while (passwords.Count < limit && (line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line)) passwords.Add(line);
                }
            }
            loadLog.AppendLine($"Loaded {passwords.Count} passwords");
            _sb.Insert(0, loadLog);

            StringBuilder lastlog = null;
            
            for (int i = 0; i < 6; i++)
            {
                lastlog = Index(passwords.Count, passwords);
            }

            _sb.Insert(0, lastlog);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(Output));
                SearchCommand.NotifyCanExecuteChanged();
            });
        }

        private StringBuilder Index(int limit, IEnumerable<string> passwords)
        {
            var sb = new StringBuilder();
            ISearch<string> stringSearch, linqSearch, hashSearch, charSequenceSearch, suffixArraySearch, nGramSearch;

            using (new TimerScope(sb, $"String indexed {limit}"))
            {
                stringSearch = new StringSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"LINQ indexed {limit}"))
            {
                linqSearch = new LinqSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"Hash indexed {limit}"))
            {
                hashSearch = new HashSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"Character Sequence indexed {limit}"))
            {
                charSequenceSearch = new CharSequenceSearch<string>(passwords);
            }

            using (new TimerScope(sb, $"Suffix Array indexed {limit}"))
            {
                suffixArraySearch = new SuffixArraySearch<string>(passwords);
            }

            using (new TimerScope(sb, $"N-Gram indexed {limit}"))
            {
                nGramSearch = new NGramSearch<string>(passwords);
            }

            // Publish only a complete pass; a later failed pass retains this complete set.
            _stringSearch = stringSearch;
            _linqSearch = linqSearch;
            _hashSearch = hashSearch;
            _charSequenceSearch = charSequenceSearch;
            _suffixArraySearch = suffixArraySearch;
            _nGramSearch = nGramSearch;

            return sb;
        }

        public string SearchPhrase { get; set; } = "catherine";

        public bool UseString { get; set; }
        public bool UseLinq { get; set; }
        public bool UseHash { get; set; }
        public bool UseCharSequence { get; set; }
        public bool UseSuffixArray { get; set; }
        public bool UseNGram { get; set; } = true;

        private int _numberOfEntries = 10000;
        private string _numberOfEntriesText = "10000";

        public int NumberOfEntries
        {
            get => _numberOfEntries;
            set
            {
                SetProperty(ref _numberOfEntries, value);
                NumberOfEntriesText = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        // Preserve invalid typed text so it cannot silently reuse a previous integer binding.
        public string NumberOfEntriesText
        {
            get => _numberOfEntriesText;
            set
            {
                if (!SetProperty(ref _numberOfEntriesText, value)) return;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                    SetProperty(ref _numberOfEntries, count, nameof(NumberOfEntries));
            }
        }
        public int NumberOfSearches { get; set; } = 10000;
        public string Output => _sb.ToString();
    }
}
