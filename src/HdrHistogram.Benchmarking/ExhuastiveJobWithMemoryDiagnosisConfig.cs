using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;

namespace HdrHistogram.Benchmarking
{
    public class ExhuastiveJobWithMemoryDiagnosisConfig : ManualConfig
    {
        public ExhuastiveJobWithMemoryDiagnosisConfig()
        {
            Add(new MemoryDiagnoser());

            var crossProduct = from jitter in new[] {Jit.LegacyJit, Jit.RyuJit}
                               from platform in new[] {Platform.AnyCpu, Platform.X64, Platform.X86}
                               //Lets not test old/unsupported frameworks.
                               from framework in new[] { /*Framework.V40, Framework.V45, Framework.V451,*/ Framework.V452, /*Framework.V46, Framework.V461,*/ Framework.V462 } 
                               //from toolchain in new[] {Toolchain.}
                               //Currently the project doesn't support Core or Mono
                               from runtime in new[] {Runtime.Clr, /*Runtime.Core , Runtime.Mono*/}
                               //where !(runtime == Runtime.Core && (jitter == Jit.LegacyJit || platform!=Platform.X64))
                               select new Job()
                               {
                                   Jit = jitter,
                                   Platform = platform,
                                   Framework = framework,
                                   Runtime = runtime
                               };

            Add(crossProduct.ToArray());
        }
    }
}