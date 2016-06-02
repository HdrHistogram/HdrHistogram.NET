using BenchmarkDotNet.Running;

namespace HdrHistogram.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                typeof(LeadingZeroCount.LeadingZeroCount64BitBenchmark),
                typeof(LeadingZeroCount.LeadingZeroCount32BitBenchmark),
                typeof(Recording.Recording32BitBenchmark),
            });
            switcher.Run(args);
        }
    }
}
