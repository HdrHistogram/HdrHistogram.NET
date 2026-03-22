# Task Checklist: Issue #141 — ByteBuffer Reduce Allocations for Serialisation Path

## Implementation Note

Implementation changes are already committed on this branch (`feat(#141): implement tasks`,
`feat(#141): complete implementation`).
Benchmark scaffolding is partially complete (`bench(#141): add end-to-end serialisation benchmark`).
The checklist below reflects all tasks required by the brief; already-committed items are marked `[x]`.

---

## Phase 1 — Benchmark Scaffolding

### Create benchmark classes

- [x] **`HdrHistogram.Benchmarking/ByteBuffer/ByteBufferBenchmark.cs`** — Create micro-benchmark
  class decorated with `[MemoryDiagnoser]`.
  Setup allocates two `ByteBuffer` instances of `Iterations * sizeof(long)` bytes and pre-populates
  the read buffer with `i * 12345678L` values.
  Provides `PutLong_After` and `GetLong_After` `[Benchmark]` methods, each iterating 1 000 times.
  Verify: file exists; class has `[MemoryDiagnoser]`; both `[Benchmark]` methods are present.

- [x] **`HdrHistogram.Benchmarking/Serialisation/SerialisationBenchmark.cs`** — Create end-to-end
  benchmark class decorated with `[MemoryDiagnoser]`.
  Setup creates `LongHistogram(3600_000_000L, 3)` with 10 000 recorded values and pre-allocates
  buffers for all four operations.
  Provides `Encode`, `Decode`, `EncodeCompressed`, and `DecodeCompressed` `[Benchmark]` methods.
  Verify: file exists; class has `[MemoryDiagnoser]`; all four `[Benchmark]` methods are present.

### Register benchmarks

- [ ] **`HdrHistogram.Benchmarking/Program.cs`** — Add `typeof(Serialisation.SerialisationBenchmark)`
  to the `BenchmarkSwitcher` array alongside `ByteBufferBenchmark`.
  Why: `SerialisationBenchmark` was created but never registered; it cannot be selected at runtime.
  Verify: `Program.cs` `BenchmarkSwitcher` array contains
  `typeof(Serialisation.SerialisationBenchmark)`.

### Build verification

- [ ] **Build benchmarking project in Release configuration.**
  Command: `dotnet build HdrHistogram.Benchmarking/ -c Release`
  Why: confirms benchmark classes compile against all target frameworks before capturing results.
  Verify: command exits with code 0, no compilation errors.

### Baseline benchmarks

> **Note:** Implementation is already committed on this branch.
> To obtain a true baseline, check out the commit immediately before `5f164d3`
> (`feat(#141): implement tasks`) in an isolated worktree, run benchmarks there,
> then discard the worktree.
> If a pre-implementation baseline cannot be obtained, document this limitation in
> `plan/benchmarks/baseline.md` and treat the post-change results as the sole data point.

- [ ] **Create `plan/benchmarks/` directory.**
  Verify: directory exists at `plan/benchmarks/`.

- [ ] **Run baseline benchmarks on the pre-implementation state and save results.**
  Steps:

  1. Create an isolated worktree at the pre-implementation commit:
     `git worktree add /tmp/hdr-baseline 5f164d3^`
  2. Register `SerialisationBenchmark` in that worktree's `Program.cs`.
  3. Run micro-benchmarks:
     `dotnet run -c Release --project HdrHistogram.Benchmarking/ -- --filter '*ByteBufferBenchmark*' --exporters json`
  4. Run end-to-end benchmarks:
     `dotnet run -c Release --project HdrHistogram.Benchmarking/ -- --filter '*SerialisationBenchmark*' --exporters json`
  5. Remove the worktree: `git worktree remove /tmp/hdr-baseline`
  6. Save a formatted Markdown table to `plan/benchmarks/baseline.md` containing
     Mean, StdDev, Allocated, and Op/s for every benchmark method.

  Verify: `plan/benchmarks/baseline.md` exists and contains rows for
  `PutLong_After`, `GetLong_After`, `Encode`, `Decode`, `EncodeCompressed`, `DecodeCompressed`.

---

## Phase 2 — Implementation

### ByteBuffer.cs — replace BitConverter / IPAddress paths

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `GetShort()`** — Replace with
  `BinaryPrimitives.ReadInt16BigEndian(_internalBuffer.AsSpan(Position))`.
  Verify: no `IPAddress` or `BitConverter.GetBytes` reference in `GetShort`; `Position` advances by
  `sizeof(short)`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `GetInt()`** — Replace with
  `BinaryPrimitives.ReadInt32BigEndian(_internalBuffer.AsSpan(Position))`.
  Verify: no `IPAddress.NetworkToHostOrder` call; `Position` advances by `sizeof(int)`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `GetLong()`** — Replace with
  `BinaryPrimitives.ReadInt64BigEndian(_internalBuffer.AsSpan(Position))`.
  Verify: no `IPAddress.NetworkToHostOrder` call; `Position` advances by `sizeof(long)`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `GetDouble()`** — Replace with
  `BinaryPrimitives.ReadInt64BigEndian` to obtain raw bits, then `BitConverter.Int64BitsToDouble`.
  Verify: no `Array.Reverse` call; `Position` advances by `sizeof(double)`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `PutInt(int value)`** — Replace with
  `BinaryPrimitives.WriteInt32BigEndian(_internalBuffer.AsSpan(Position), value)`.
  Verify: no `BitConverter.GetBytes` call; `Position` advances by `sizeof(int)`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `PutInt(int index, int value)`** — Replace with
  `BinaryPrimitives.WriteInt32BigEndian(_internalBuffer.AsSpan(index), value)`.
  Verify: indexed overload uses `BinaryPrimitives`; `Position` is not modified.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `PutLong()`** — Replace with
  `BinaryPrimitives.WriteInt64BigEndian(_internalBuffer.AsSpan(Position), value)`.
  Verify: no `BitConverter.GetBytes` call; `Position` advances by `sizeof(long)`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs` — `PutDouble()`** — Replace with
  `BitConverter.DoubleToInt64Bits(value)` then
  `BinaryPrimitives.WriteInt64BigEndian(_internalBuffer.AsSpan(Position), bits)`.
  Verify: no `Array.Reverse` call; `Position` advances by `sizeof(double)`.

### ByteBuffer.cs — remove dead helpers and obsolete imports

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs`** — Remove dead private helper methods:
  `Int64BitsToDouble`, `ToInt64`, `CheckedFromBytes`, `FromBytes`, `CheckByteArgument`.
  Verify: none of those identifiers appear anywhere in `ByteBuffer.cs`.

- [x] **`HdrHistogram/Utilities/ByteBuffer.cs`** — Remove `using System.Net;` and any
  `IPAddress` references; add `using System.Buffers.Binary;`.
  Verify: file contains `using System.Buffers.Binary;`; no `System.Net` namespace appears.

### Dependency — netstandard2.0

- [x] **`HdrHistogram/HdrHistogram.csproj`** — Confirm `System.Memory` 4.5.* `PackageReference`
  is scoped to `netstandard2.0` so that `System.Buffers.Binary.BinaryPrimitives` is available on
  that target framework.
  Verify: `<PackageReference Include="System.Memory"` with `netstandard2.0` condition exists in the
  `.csproj`; project builds for `netstandard2.0`.

### Unit tests

- [x] **`HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` — `PutInt_and_GetInt_roundtrip`** —
  Add `[Theory]` with `[InlineData(42)]`, `[InlineData(-1)]`, `[InlineData(int.MaxValue)]`.
  Assert that `GetInt()` returns the written value and `Position` equals `sizeof(int)`.
  Verify: test exists; passes.

- [x] **`HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` — `PutInt_at_index_and_GetInt_roundtrip`** —
  Add `[Theory]` verifying that `PutInt(index, value)` does not advance `Position`.
  Assert `positionBefore == buffer.Position` after the indexed write.
  Verify: test exists; passes.

- [x] **`HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` — `PutLong_and_GetLong_roundtrip`** —
  Add `[Theory]` with `[InlineData(100L)]`, `[InlineData(-1L)]`, `[InlineData(long.MaxValue)]`.
  Verify: test exists; passes.

- [x] **`HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` — `PutDouble_and_GetDouble_roundtrip`** —
  Add `[Theory]` covering `0.0`, `double.PositiveInfinity`, and `3.14159265358979`.
  Use bit-identity comparison via `BitConverter.DoubleToInt64Bits` to avoid floating-point equality
  pitfalls.
  Verify: test exists; passes.

- [x] **`HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` — `PutDouble_and_GetDouble_roundtrip_NaN`** —
  Add `[Fact]` verifying NaN round-trip via bit-identity comparison.
  Why: `double.NaN != double.NaN` by IEEE 754; `double.IsNaN` or `DoubleToInt64Bits` must be used.
  Verify: test exists; passes.

- [x] **`HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` — `GetShort_returns_big_endian_value`** —
  Add `[Theory]` with raw `byte[]` setup via reflection helper `ByteBufferTestHelper`.
  Assert that the buffer reads the correct big-endian `short` value.
  Verify: test and `ByteBufferTestHelper` helper class exist; test passes.

### Run all unit tests

- [ ] **Run full test suite to confirm no regressions.**
  Command: `dotnet test HdrHistogram.UnitTests/ -c Release`
  Must pass:

  - `ByteBufferTests.ReadFrom_returns_all_bytes_when_stream_returns_partial_reads` (Issue #99)
  - All six new `ByteBufferReadWriteTests` methods
  - All `HistogramEncodingTestBase` encoding/decoding round-trip tests

  Verify: command exits with 0 failures across all target frameworks.

---

## Phase 3 — Benchmark Validation

- [ ] **Run post-change benchmarks** with identical configuration to baseline.
  Commands:

  ```
  dotnet run -c Release --project HdrHistogram.Benchmarking/ -- --filter '*ByteBufferBenchmark*' --exporters json
  dotnet run -c Release --project HdrHistogram.Benchmarking/ -- --filter '*SerialisationBenchmark*' --exporters json
  ```

  Save formatted Markdown table to `plan/benchmarks/post-change.md`.
  Include: Mean, StdDev, Allocated, Op/s for every benchmark method.
  Verify: `plan/benchmarks/post-change.md` exists; `PutLong_After` and `GetLong_After` rows show
  `Allocated = 0 B`.

- [ ] **Generate comparison in `plan/benchmarks/comparison.md`.**
  Required content:

  - Side-by-side table: `Benchmark | Baseline | Post-Change | Delta | Delta %`
  - Summary: which metrics improved, regressed, or are unchanged
  - Verdict: does the data support the change?

  Verify: `plan/benchmarks/comparison.md` exists with all three sections completed.

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion | Covered by |
|---|---|
| All `ByteBuffer` read/write methods use `BinaryPrimitives` with `.AsSpan()` — zero intermediate heap allocations | Phase 2 — implementation tasks 1–8 |
| No `System.Net.IPAddress` references remain in `ByteBuffer.cs` | Phase 2 — remove obsolete imports |
| No `BitConverter.GetBytes` or `Array.Reverse` calls remain in `ByteBuffer.cs` | Phase 2 — implementation tasks 1–8 |
| Dead private helper methods removed | Phase 2 — remove dead helpers |
| All pre-existing tests (including Issue #99 regression) continue to pass | Phase 2 — run all unit tests |
| New round-trip unit tests cover `PutInt`/`GetInt`, indexed `PutInt`, `PutLong`/`GetLong`, `PutDouble`/`GetDouble`, `GetShort` | Phase 2 — unit test tasks 1–6 |
| Benchmark classes exist with `[MemoryDiagnoser]` and report `0 B` allocations for `PutLong`/`GetLong` | Phase 1 — create benchmarks + Phase 3 — post-change results |
| Project builds and tests pass on all target frameworks: `net8.0`, `net9.0`, `net10.0`, `netstandard2.0` | Phase 2 — run all unit tests; Phase 1 — build verification |
