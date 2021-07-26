using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using System;
using System.Collections.Generic;
using System.Text;

namespace PasswordSearch.Benchmark
{
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithRuntime(CoreRuntime.Core50)
                .WithPlatform(Platform.X64)
                .WithJit(Jit.Default)
                .WithGcServer(true));

            AddExporter(DefaultExporters.AsciiDoc);

            AddColumnProvider(DefaultColumnProviders.Instance);

            WithOption(ConfigOptions.Default, true);
            WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            WithOption(ConfigOptions.DisableLogFile, true);
            WithOption(ConfigOptions.JoinSummary, true);
            WithOption(ConfigOptions.StopOnFirstError, true);

            AddLogger(new ConsoleLogger());
        }
    }
}
