using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace HdrHistogram.Benchmarking
{
    public class WithMemoryDiagnosisConfig : ManualConfig
    {
        public WithMemoryDiagnosisConfig()
        {
            Add(new MemoryDiagnoser());
            //Add(new InliningDiagnoser());
        }
    }
}