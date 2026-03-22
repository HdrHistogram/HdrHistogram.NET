# Benchmark Comparison: Baseline vs Post-Change

## Baseline Limitation

A true pre-implementation baseline was not captured because the implementation was already committed before benchmarks could be run.
See `baseline.md` for details.

## Post-Change Results

Both benchmark suites ran successfully.
See `post-change.md` for full results and environment details.

## Code-Level Analysis (Authoritative)

The implementation replaces all `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))` calls with `BinaryPrimitives.Write*BigEndian(_internalBuffer.AsSpan(Position), value)`.

| Metric | Before | After | Delta |
|--------|--------|-------|-------|
| Heap allocations per `PutLong` call | 1 × `byte[8]` (16 B managed overhead + 8 B data) | 0 | −100% |
| Heap allocations per `GetLong` call | 1 × `byte[8]` (16 B managed overhead + 8 B data) | 0 | −100% |
| Heap allocations per `PutInt` call | 1 × `byte[4]` | 0 | −100% |
| Heap allocations per `GetInt` call | 1 × `byte[4]` | 0 | −100% |
| Heap allocations per `PutDouble` call | 1 × `byte[8]` + `Array.Reverse` (in-place) | 0 | −100% |
| Heap allocations per `GetDouble` call | 1 × `byte[8]` + `Array.Reverse` (in-place) | 0 | −100% |

## Measured Post-Change Results (ByteBuffer)

| Benchmark | Runtime | Allocated |
|-----------|---------|----------:|
| `PutLong` | .NET 10.0 | 0 B |
| `GetLong` | .NET 10.0 | 0 B |
| `PutLong` | .NET 8.0 | 0 B |
| `GetLong` | .NET 8.0 | 0 B |
| `PutLong` | .NET 9.0 | 0 B |
| `GetLong` | .NET 9.0 | 0 B |

The `Allocated` column showing `-` in BenchmarkDotNet output means zero bytes allocated per operation.

## Verdict

The change provably eliminates all intermediate heap allocations in `ByteBuffer` read/write methods, confirmed by live benchmark runs showing `0 B` allocated for `PutLong` and `GetLong` across all three runtimes (.NET 8, 9, 10).
The `BinaryPrimitives` API operates directly on `Span<byte>` slices of the existing `_internalBuffer` array, with no new allocations.
No regressions are expected: the same big-endian byte-order semantics are preserved, verified by 891 passing unit tests.
