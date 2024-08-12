using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;

namespace HdrHistogram.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                //.AddDiagnoser(MemoryDiagnoser.Default)
                .AddColumn(
                    StatisticColumn.OperationsPerSecond,
                    StatisticColumn.Mean, StatisticColumn.StdErr, StatisticColumn.StdDev,
                    StatisticColumn.P0, StatisticColumn.Q1, StatisticColumn.P50, StatisticColumn.P67, StatisticColumn.Q3, StatisticColumn.P80, StatisticColumn.P90, StatisticColumn.P95, StatisticColumn.P100)
                .AddJob(Job.Default.WithRuntime(ClrRuntime.Net481).WithJit(Jit.LegacyJit))
                .AddJob(Job.Default.WithRuntime(ClrRuntime.Net481).WithJit(Jit.RyuJit))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core21))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core31))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core50))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60))
                ;

            var switcher = new BenchmarkSwitcher(new[] {
                typeof(LeadingZeroCount.LeadingZeroCount64BitBenchmark),
                typeof(LeadingZeroCount.LeadingZeroCount32BitBenchmark),
                typeof(Recording.Recording32BitBenchmark),
            });
            switcher.Run(args, config);
        }
    }
}
