using BenchmarkDotNet.Running;
using HdrHistogram.Benchmarking.LeadingZeroCount;

namespace HdrHistogram.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                typeof(LeadingZeroCount64BitBenchmark),
                typeof(LeadingZeroCount32BitBenchmark)
            });
            switcher.Run(args);
        }
    }
}
