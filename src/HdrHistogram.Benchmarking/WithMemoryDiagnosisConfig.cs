using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;

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