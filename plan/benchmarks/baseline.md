# Array equality baseline

Base library: `023f13c77134b6e15626f6eb8325a8b365f4d62e`.
Benchmark-only commit: `b3b30f6`.
The implementation is unchanged.

BenchmarkDotNet 0.15.8, default job settings, .NET 8.0.31 x64, SDK 10.0.401, Windows 11, Intel Core i9-13900K.
The same two benchmark classes and command are used before and after the change.
A temporary runner calls `BenchmarkSwitcher.FromTypes(new[] { typeof(ArrayEqualityBenchmark), typeof(ArrayEqualityDecodingBenchmark) }).Run(args)` with the default configuration.
This selects .NET 8 alone without the repository entry point adding its three explicit runtime jobs.

```text
dotnet run -c Release --project <temporary-runner>/Runner.csproj -- --filter "*ArrayEquality*" --runtimes net8.0 --exporters json --artifacts <results-directory>
```

| Method | Mean (ns) | StdDev (ns) | Allocated (bytes/op) | Operations/s |
| --- | ---: | ---: | ---: | ---: |
| EqualLongArrays | 101983.71 | 5050.88 | 1130496 | 9805.49 |
| DecodeHistogram | 28356.44 | 764.77 | 189276 | 35265.36 |

The microbenchmark compares two separate 23,552-element long arrays (184 KiB each).
The end-to-end benchmark decodes an encoded histogram through the public API and its constructor lookup.
That lookup compares `Type[]`, so it does not box primitive values.
Do not infer a histogram decoding speedup from the long-array microbenchmark.
