# Issue #141: ByteBuffer — Reduce Allocations for Serialisation Path

## Summary

`ByteBuffer.PutInt`, `PutLong`, `GetInt`, and `GetLong` originally called `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))`, which allocates a new `byte[]` on every call and immediately copies it into the buffer.
The fix replaces all such paths with `BinaryPrimitives.WriteInt##BigEndian` / `ReadInt##BigEndian` operating directly on `_internalBuffer.AsSpan(Position)`, achieving zero intermediate heap allocations.
The `IPAddress.HostToNetworkOrder` / `NetworkToHostOrder` dance exists solely for big-endian byte ordering; `BinaryPrimitives` handles this directly.
The same allocation pattern applies to `PutDouble` / `GetDouble` (via `BitConverter.GetBytes` + `Array.Reverse`) and `GetShort`.

## Affected Files

| File | Change |
|------|--------|
| `HdrHistogram/Utilities/ByteBuffer.cs` | Replace all `BitConverter`/`IPAddress` paths with `BinaryPrimitives`; remove dead helper methods |
| `HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` | Add round-trip unit tests for all read/write methods |
| `HdrHistogram.Benchmarking/ByteBuffer/ByteBufferBenchmark.cs` | New micro-benchmark for `PutLong` / `GetLong` (1 000 iterations, `[MemoryDiagnoser]`) |
| `HdrHistogram.Benchmarking/Serialisation/SerialisationBenchmark.cs` | New end-to-end benchmark for `Encode` / `Decode` / `EncodeCompressed` / `DecodeCompressed` |

## Acceptance Criteria

- All `ByteBuffer` read/write methods use `BinaryPrimitives` with `.AsSpan()` — zero intermediate heap allocations per call.
- No `System.Net.IPAddress` references remain in `ByteBuffer.cs`.
- No `BitConverter.GetBytes` or `Array.Reverse` calls remain in `ByteBuffer.cs`.
- Dead private helper methods (`Int64BitsToDouble`, `ToInt64`, `CheckedFromBytes`, `FromBytes`, `CheckByteArgument`) are removed.
- All pre-existing tests (including the Issue #99 stream-read regression test) continue to pass.
- New round-trip unit tests cover `PutInt`/`GetInt`, indexed `PutInt`, `PutLong`/`GetLong`, `PutDouble`/`GetDouble`, and `GetShort`.
- Benchmark classes exist with `[MemoryDiagnoser]` and report zero managed allocations for `PutLong` / `GetLong`.
- Project builds and tests pass on all target frameworks: `net8.0`, `net9.0`, `net10.0`, `netstandard2.0`.

## Test Strategy

### Unit Tests

Add a new test class `ByteBufferReadWriteTests` in `HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs`:

- `PutInt_and_GetInt_roundtrip` — `[Theory]` with `[InlineData(42, -1, int.MaxValue)]`
- `PutInt_at_index_and_GetInt_roundtrip` — verifies indexed write does not advance `Position`
- `PutLong_and_GetLong_roundtrip` — `[Theory]` with `[InlineData(100L, -1L, long.MaxValue)]`
- `PutDouble_and_GetDouble_roundtrip` — `[Theory]` covering `0.0`, `PositiveInfinity`, and `3.14159…`
- `PutDouble_and_GetDouble_roundtrip_NaN` — separate `[Fact]` (NaN equality requires `double.IsNaN`)
- `GetShort_returns_big_endian_value` — verifies byte-order correctness via raw byte setup

Use `FluentAssertions` for assertions.
Access `_internalBuffer` via a `ByteBufferTestHelper` reflection helper where needed.

### Existing Tests

The existing `ByteBufferTests.ReadFrom_returns_all_bytes_when_stream_returns_partial_reads` test (Issue #99 regression) must continue to pass unchanged.
Full-stack encoding/decoding tests in `HdrHistogram.UnitTests/Persistence/HistogramEncodingTestBase.cs` exercise all callers and must continue to pass.

## Category

`performance`

## Benchmark Strategy

### Benchmark-Driven Development Process

Follow the benchmark-driven process from `spec/tech-standards/testing-standards.md`:

1. Add benchmarks **before** (or alongside) implementation changes so before/after results can be captured.
2. Run benchmarks in Release configuration: `dotnet run -c Release --project HdrHistogram.Benchmarking`.
3. Use `[MemoryDiagnoser]` to capture `Allocated` column alongside mean throughput.
4. Record results in the PR description.

### Existing Relevant Benchmarks

- `HdrHistogram.Benchmarking/Recording/Recording32BitBenchmark.cs` — recording throughput across histogram types; not directly affected.
- `HdrHistogram.Benchmarking/LeadingZeroCount/` — bit-manipulation benchmarks; not affected.

### New Micro-Benchmarks (`ByteBufferBenchmark.cs`)

Class: `ByteBufferBenchmark` in `HdrHistogram.Benchmarking/ByteBuffer/`

| Benchmark | What it measures |
|-----------|-----------------|
| `PutLong_After` | Writes 1 000 `long` values sequentially into a pre-allocated buffer |
| `GetLong_After` | Reads 1 000 `long` values sequentially from a pre-populated buffer |

Setup: allocate two `ByteBuffer` instances of `1000 * sizeof(long)` bytes; populate the read buffer with `i * 12345678L` values.
Expected result after optimisation: `Allocated = 0 B` for both benchmarks.

### New End-to-End Benchmarks (`SerialisationBenchmark.cs`)

Class: `SerialisationBenchmark` in `HdrHistogram.Benchmarking/Serialisation/`

| Benchmark | What it measures |
|-----------|-----------------|
| `Encode` | Encodes a 10 000-value `LongHistogram` to an uncompressed `ByteBuffer` |
| `Decode` | Decodes an uncompressed `ByteBuffer` back to a `HistogramBase` |
| `EncodeCompressed` | Encodes to a DEFLATE-compressed `ByteBuffer` |
| `DecodeCompressed` | Decodes from a DEFLATE-compressed `ByteBuffer` |

Setup: create `LongHistogram(3600_000_000L, 3)` with 10 000 recorded values spanning the range; pre-encode buffers for decode benchmarks.

### Metrics That Matter

| Metric | Why |
|--------|-----|
| `Mean` (ns) | Raw throughput improvement |
| `Allocated` (B) | Must be `0 B` for `PutLong`/`GetLong` after the fix |
| GC collections (`Gen0`) | Reduced allocation removes Gen 0 pressure on serialisation-heavy workloads |

## Risks and Open Questions

- **`netstandard2.0` compatibility**: `System.Buffers.Binary.BinaryPrimitives` is available via the `System.Memory` NuGet package on `netstandard2.0`.
  Verify that `HdrHistogram.csproj` references `System.Memory` (or that it is pulled in transitively) when targeting `netstandard2.0`.
- **`BitConverter.Int64BitsToDouble` on `netstandard2.0`**: Available since .NET Standard 1.1; no compatibility risk.
- **`double` NaN round-trip**: `BinaryPrimitives.ReadInt64BigEndian` + `BitConverter.Int64BitsToDouble` preserves all NaN bit patterns; test explicitly.
- **No `Span<T>`-based public API**: The issue mentions refactoring `ByteBuffer` to accept `Memory<byte>` or `Span<byte>` from callers.
  This is a larger structural change and is out of scope for this issue; note it as a follow-up.
