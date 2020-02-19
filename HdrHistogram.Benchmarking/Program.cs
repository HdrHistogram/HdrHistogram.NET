using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;

namespace HdrHistogram.Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.CreateEmpty() // A configuration for our benchmarks
                .With(MemoryDiagnoser.Default)
                .With(Job.Default // Adding first job
                    .With(CoreRuntime.Core21) // .netcoreapp2.1
                    .With(ClrRuntime.Net472) // .NET Framework 4.7.2
                    .With(Platform.X64) // Run as x64 application
                    .With(Platform.X86) // Run as x86 application
                    .With(Jit.LegacyJit) // Use LegacyJIT instead of the default RyuJIT
                    .With(Jit.RyuJit) // Use RyuJit
                  );

            var switcher = new BenchmarkSwitcher(new[] {
                typeof(LeadingZeroCount.LeadingZeroCount64BitBenchmark),
                typeof(LeadingZeroCount.LeadingZeroCount32BitBenchmark),
                typeof(Recording.Recording32BitBenchmark),
            });

            switcher.Run(args, config);
        }
    }
}
