# Array equality comparison

Both runs use .NET 8.0.31 on the same machine with default BenchmarkDotNet settings.
Baseline and implementation revisions are recorded in the companion results files.

| Method | Baseline mean (ns) | After mean (ns) | Delta (ns) | Delta (%) | Baseline bytes/op | After bytes/op |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| EqualLongArrays | 101983.71 | 3457.48 | -98526.23 | -96.61% | 1130496 | 0 |
| DecodeHistogram | 28356.44 | 27770.42 | -586.02 | -2.07% | 189276 | 189276 |

## Interpretation

The primitive long-array comparison eliminates per-element boxing and its measured allocations.
Custom value/reference types retain the original `object.Equals` comparison.
This preserves typed versus object equality differences, nullable values and covariant reference arrays.
Floating-point comparisons retain equality semantics for NaN and signed zero.

The existing histogram decoding caller compares `Type[]`, which does not box elements and uses the unchanged fallback.
Its allocations are unchanged and its timing delta is within the spread of these measurements.
No end-to-end histogram decoding improvement is claimed.
No benchmark showed a mean regression in this run.
Benchmarks were measured only on .NET 8; .NET 9 and .NET 10 are covered by correctness tests, not performance claims.

## Regression evidence

Before the implementation change, 25 compatibility cases passed and the new allocation case failed.
It measured 786,432 bytes for sixteen comparisons of two 1,024-element long arrays, against an expected zero.
After the change, all 26 targeted cases pass.
The complete suite passed 925 tests on each of .NET 8, .NET 9 and .NET 10.
All results described here are automated checks.
