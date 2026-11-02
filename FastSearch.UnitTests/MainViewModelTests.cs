using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PasswordSearch;

namespace FastSearch.UnitTests;

[TestClass]
[DoNotParallelize]
public class MainViewModelTests
{
    private static readonly string[] IndexFields =
        ["_stringSearch", "_linqSearch", "_hashSearch", "_charSequenceSearch", "_suffixArraySearch", "_nGramSearch"];

    [TestCleanup]
    public void Cleanup() => PasswordSearch.FileSystem.Reset();

    [TestMethod]
    public void InitialStateDisablesSearch()
    {
        var vm = new MainViewModel();
        Assert.IsFalse(vm.IsBusy);
        Assert.IsTrue(vm.LoadFileCommand.CanExecute(null));
        Assert.IsFalse(vm.SearchCommand.CanExecute(null));
        Assert.IsTrue((bool)typeof(MainViewModel).GetProperty("IsIdle")!.GetValue(vm)!);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(1000001)]
    public async Task InvalidEntryCountsDoNotStartWork(int count)
    {
        var vm = new MainViewModel { NumberOfEntries = count };
        await AssertNoWork(vm, vm.LoadFileCommand);
        StringAssert.Contains(vm.Status, "1,000,000");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(1000000)]
    public async Task InclusiveEntryBoundsAreAcceptedAndReportActualAvailableCount(int count)
    {
        PasswordSearch.FileSystem.OpenFactory = _ => Task.FromResult(Data("alpha\nbeta\n"));
        var vm = new MainViewModel { NumberOfEntries = count };
        await Execute(vm, vm.LoadFileCommand);
        Assert.IsTrue(vm.SearchCommand.CanExecute(null));
        StringAssert.Contains(vm.Output, $"Loaded {Math.Min(count, 2)} passwords");
        StringAssert.Contains(vm.Output, $"N-Gram indexed {Math.Min(count, 2)}");
    }

    [TestMethod]
    [DataRow("invalid")]
    [DataRow("")]
    [DataRow("1.5")]
    [DataRow("999999999999999999999")]
    public async Task InvalidTypedDatasetTextDoesNotReusePreviousCount(string text)
    {
        var vm = new MainViewModel();
        var property = typeof(MainViewModel).GetProperty("NumberOfEntriesText");
        Assert.IsNotNull(property);
        property.SetValue(vm, text);
        await AssertNoWork(vm, vm.LoadFileCommand);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task BlankQueryDoesNotStartTiming(string query)
    {
        var vm = await Loaded();
        vm.SearchPhrase = query;
        await AssertNoWork(vm, vm.SearchCommand);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task NonpositiveRepetitionsDoNotStartTiming(int repetitions)
    {
        var vm = await Loaded();
        vm.NumberOfSearches = repetitions;
        await AssertNoWork(vm, vm.SearchCommand);
    }

    [TestMethod]
    public async Task LoadFiltersBlanksAndStopsAtSelectedNonblankCount()
    {
        var stream = new SingleByteStream("\n \nalpha\nbeta\nignored\n");
        PasswordSearch.FileSystem.OpenFactory = _ => Task.FromResult<Stream>(stream);
        var vm = new MainViewModel { NumberOfEntries = 2 };
        await Execute(vm, vm.LoadFileCommand);
        Assert.AreEqual(2, SearchField(vm, "_stringSearch").Search("a").Count);
        Assert.AreEqual(0, SearchField(vm, "_stringSearch").Search("ignored").Count);
        Assert.IsTrue(stream.BytesRead <= Encoding.UTF8.GetByteCount("\n \nalpha\nbeta\n"));
        StringAssert.Contains(vm.Output, "Loaded 2 passwords");
        Assert.IsFalse(vm.Output.Contains("10M", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LoadUsesCapturedCountAndNotifiesIdle()
    {
        var entered = NewCompletion();
        var release = NewCompletion();
        PasswordSearch.FileSystem.OpenFactory = async _ =>
        {
            entered.TrySetResult();
            await release.Task;
            return Data("alpha\nbeta\ngamma\n");
        };
        var vm = new MainViewModel { NumberOfEntries = 2 };
        var idleNotifications = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "IsIdle") idleNotifications++;
            if (e.PropertyName == nameof(vm.IsBusy) && vm.IsBusy) vm.NumberOfEntries = 1;
        };
        var completion = Execute(vm, vm.LoadFileCommand);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(vm.IsBusy);
            Assert.IsFalse(vm.LoadFileCommand.CanExecute(null));
            Assert.IsFalse(vm.SearchCommand.CanExecute(null));
        }
        finally { release.TrySetResult(); }
        await completion;
        StringAssert.Contains(vm.Output, "Loaded 2 passwords");
        Assert.AreEqual(2, idleNotifications);
    }

    [TestMethod]
    public async Task SearchUsesCapturedQueryRepetitionsAndAlgorithm()
    {
        var vm = await Loaded();
        vm.SearchPhrase = "alpha";
        vm.NumberOfSearches = 2;
        vm.UseString = true;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(vm.IsBusy) || !vm.IsBusy) return;
            vm.SearchPhrase = "beta";
            vm.NumberOfSearches = 9;
            vm.UseString = false;
        };
        await Execute(vm, vm.SearchCommand);
        StringAssert.Contains(vm.Output, "Search for alpha 2 times using String Contains");
        StringAssert.Contains(vm.Output, "Actual results: 1; oracle results: 1");
    }

    [TestMethod]
    public async Task FailedInitialLoadRecoversAndRetryEnablesSearch()
    {
        PasswordSearch.FileSystem.OpenFactory = _ => Task.FromResult<Stream>(new FailingReadStream());
        var vm = new MainViewModel { NumberOfEntries = 2 };
        await Execute(vm, vm.LoadFileCommand);
        Assert.IsFalse(vm.IsBusy);
        Assert.IsFalse(vm.SearchCommand.CanExecute(null));
        Assert.IsTrue(vm.LoadFileCommand.CanExecute(null));
        StringAssert.Contains(vm.Status, "failed");
        PasswordSearch.FileSystem.OpenFactory = _ => Task.FromResult(Data("alpha\nbeta\n"));
        await Execute(vm, vm.LoadFileCommand);
        Assert.IsTrue(vm.SearchCommand.CanExecute(null));
        Assert.AreEqual(1, SearchField(vm, "_stringSearch").Search("beta").Count);
        StringAssert.Contains(vm.Output, "Loaded 2 passwords");
    }

    [TestMethod]
    public async Task FailedLaterIndexConstructorRetainsAllSixPublishedReferences()
    {
        var vm = await Loaded();
        var index = typeof(MainViewModel).GetMethod("Index", BindingFlags.NonPublic | BindingFlags.Instance)!;
        // A successful new warmup pass supersedes the previous load before a later pass fails.
        index.Invoke(vm, [2, new List<string> { "new-alpha", "new-beta" }]);
        var original = IndexFields.Select(name => Field(name).GetValue(vm)).ToArray();
        var failure = Assert.ThrowsExactly<TargetInvocationException>(() =>
            index.Invoke(vm, [2, new ThrowOnThirdEnumeration()]));
        Assert.IsInstanceOfType<IOException>(failure.InnerException);
        for (var i = 0; i < IndexFields.Length; i++)
            Assert.AreSame(original[i], Field(IndexFields[i]).GetValue(vm), IndexFields[i]);
        Assert.IsTrue(vm.SearchCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task MismatchedResultsReportFailureWithBothCounts()
    {
        var vm = await Loaded();
        Field("_nGramSearch").SetValue(vm, new WrongSearch());
        vm.SearchPhrase = "alpha";
        vm.NumberOfSearches = 1;
        await Execute(vm, vm.SearchCommand);
        StringAssert.Contains(vm.Status, "failed");
        StringAssert.Contains(vm.Output, "Actual results: 0; oracle results: 1");
        Assert.IsFalse(vm.Status.Contains("Search complete", StringComparison.Ordinal));
        Assert.IsFalse(vm.IsBusy);
        Assert.IsTrue(vm.SearchCommand.CanExecute(null));
    }

    private static async Task AssertNoWork(MainViewModel vm, RelayCommand command)
    {
        var opened = false;
        var busy = false;
        PasswordSearch.FileSystem.OpenFactory = _ =>
        {
            opened = true;
            return Task.FromResult(Data("alpha\n"));
        };
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName == nameof(vm.IsBusy) && vm.IsBusy) busy = true;
        };
        var output = vm.Output;
        vm.PropertyChanged += handler;
        try { await Execute(vm, command); }
        finally { vm.PropertyChanged -= handler; }
        Assert.IsFalse(busy);
        Assert.IsFalse(opened);
        Assert.AreEqual(output, vm.Output);
    }

    private static async Task<MainViewModel> Loaded()
    {
        PasswordSearch.FileSystem.OpenFactory = _ => Task.FromResult(Data("alpha\nbeta\n"));
        var vm = new MainViewModel { NumberOfEntries = 2 };
        await Execute(vm, vm.LoadFileCommand);
        return vm;
    }

    private static async Task Execute(MainViewModel vm, RelayCommand command)
    {
        var completed = NewCompletion();
        var started = false;
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName != nameof(vm.IsBusy)) return;
            if (vm.IsBusy) started = true;
            else if (started) completed.TrySetResult();
        };
        vm.PropertyChanged += handler;
        try
        {
            command.Execute(null);
            if (started) await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { vm.PropertyChanged -= handler; }
    }

    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Stream Data(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));
    private static FieldInfo Field(string name) => typeof(MainViewModel).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static ISearch<string> SearchField(MainViewModel vm, string name) => (ISearch<string>)Field(name).GetValue(vm)!;

    private sealed class WrongSearch : ISearch<string>
    {
        public ICollection<string> Search(string search) => Array.Empty<string>();
    }

    private sealed class ThrowOnThirdEnumeration : IEnumerable<string>
    {
        private int _enumerations;
        public IEnumerator<string> GetEnumerator()
        {
            if (++_enumerations == 3) throw new IOException("later index failed");
            return ((IEnumerable<string>)new[] { "new-alpha", "new-beta" }).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SingleByteStream(string text) : MemoryStream(Encoding.UTF8.GetBytes(text))
    {
        public int BytesRead { get; private set; }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], cancellationToken);
            BytesRead += read;
            return read;
        }
    }

    private sealed class FailingReadStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("dataset read failed"));
    }
}
