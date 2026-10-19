# Array equality after the change

Measured at implementation commit `d19f360`.
Environment, temporary runner and default BenchmarkDotNet configuration match `baseline.md`.
No ShortRun job or reduced iteration settings were used.

| Method | Mean (ns) | StdDev (ns) | Allocated (bytes/op) | Operations/s |
| --- | ---: | ---: | ---: | ---: |
| EqualLongArrays | 3457.48 | 16.24 | 0 | 289227.91 |
| DecodeHistogram | 27770.42 | 793.14 | 189276 | 36009.54 |

BenchmarkDotNet reported a multimodal distribution for `DecodeHistogram` and removed one outlier.
The decoding timing difference is treated as noise, not as evidence of a speedup.
