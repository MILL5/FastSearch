using System;
using System.IO;
using System.Threading.Tasks;

namespace PasswordSearch;

// Logic tests only: these do not emulate native UI thread dispatch.
internal static class MainThread
{
    public static Task InvokeOnMainThreadAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}

internal static class FileSystem
{
    public static Func<string, Task<Stream>> OpenFactory { get; set; } = Unconfigured;

    public static Task<Stream> OpenAppPackageFileAsync(string filename) => OpenFactory(filename);

    public static void Reset() => OpenFactory = Unconfigured;

    private static Task<Stream> Unconfigured(string _) =>
        Task.FromException<Stream>(new InvalidOperationException("No test dataset configured."));
}
