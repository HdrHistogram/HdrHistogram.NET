using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace HdrHistogram.Benchmarking
{
    public class ExhuastiveJobWithMemoryDiagnosisConfig : ManualConfig
    {
        public ExhuastiveJobWithMemoryDiagnosisConfig()
        {
            Add(new MemoryDiagnoser());

            Add(
                Job.Dry
                .With(Platform.X64)
                .With(Platform.X86)
                .With(Jit.RyuJit)
                .With(Jit.LegacyJit)
                .With(Runtime.Clr)
                .WithId("ExhuastiveJobWithMemoryDiagnosis"));
        }
    }
}