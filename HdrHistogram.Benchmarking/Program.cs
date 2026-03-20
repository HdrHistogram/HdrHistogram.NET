using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace HdrHistogram.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddColumn(
                    StatisticColumn.OperationsPerSecond,
                    StatisticColumn.Mean, StatisticColumn.StdErr, StatisticColumn.StdDev,
                    StatisticColumn.P0, StatisticColumn.Q1, StatisticColumn.P50, StatisticColumn.P67, StatisticColumn.Q3, StatisticColumn.P80, StatisticColumn.P90, StatisticColumn.P95, StatisticColumn.P100)
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core90))
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0))
                ;

            var switcher = new BenchmarkSwitcher(new[] {
                typeof(LeadingZeroCount.LeadingZeroCount64BitBenchmark),
                typeof(LeadingZeroCount.LeadingZeroCount32BitBenchmark),
                typeof(Recording.Recording32BitBenchmark),
                typeof(ByteBuffer.ByteBufferBenchmark),
            });
            switcher.Run(args, config);
        }
    }
}
