# Issue \#141: ByteBuffer — Massive Allocation Waste on Hot Serialisation Path

## Summary

`ByteBuffer.cs` is the core serialisation primitive used throughout histogram encoding and decoding.
Every call to `PutInt`, `PutLong`, `GetInt`, `GetLong`, `GetShort`, `PutDouble`, and `GetDouble` currently incurs one or more heap allocations:

- `PutInt` and `PutLong` call `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))`, which allocates a `byte[]`, then immediately discards it after `Array.Copy`.
- `GetInt`, `GetLong`, and `GetShort` call `IPAddress.HostToNetworkOrder(BitConverter.ToInt32/64(...))`, which also performs unnecessary work.
- `PutDouble` calls `BitConverter.GetBytes` and `Array.Reverse`, allocating and mutating a temporary array.
- `GetDouble` delegates to a private `ToInt64 → CheckedFromBytes → FromBytes` chain that manually loops over bytes when a direct API call is available.

The `IPAddress` host/network order functions exist solely for byte-order conversion; they are a networking API being misused as an endianness utility.
`System.Buffers.Binary.BinaryPrimitives` (available since `netstandard2.0` via the `System.Memory` package) provides `WriteInt64BigEndian`, `ReadInt64BigEndian`, and equivalents for all required widths, writing directly into a `Span<byte>` with zero allocation.

On serialisation-heavy workloads (encoding thousands of histogram snapshots) this reduces GC pressure materially.

## Affected Files

| File | Change |
|---|---|
| `HdrHistogram/Utilities/ByteBuffer.cs` | Replace allocation-heavy implementations with `BinaryPrimitives` equivalents |
| `HdrHistogram/HdrHistogram.csproj` | Add `System.Memory` package reference for `netstandard2.0` target (if not already present) |
| `HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs` | Add round-trip tests for `PutInt`/`GetInt`, `PutLong`/`GetLong`, `PutDouble`/`GetDouble`, and the positioned `PutInt(index, value)` overload |
| `HdrHistogram.Benchmarking/` | Add a new `ByteBufferBenchmark` class to provide before/after evidence |

## Required Code Changes

### `PutLong` (line 267–272)

```csharp
// Before
var longAsBytes = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value));
Array.Copy(longAsBytes, 0, _internalBuffer, Position, longAsBytes.Length);
Position += longAsBytes.Length;

// After
BinaryPrimitives.WriteInt64BigEndian(_internalBuffer.AsSpan(Position), value);
Position += sizeof(long);
```

### `GetLong` (line 131–136)

```csharp
// Before
var longValue = IPAddress.HostToNetworkOrder(BitConverter.ToInt64(_internalBuffer, Position));
Position += sizeof(long);
return longValue;

// After
var longValue = BinaryPrimitives.ReadInt64BigEndian(_internalBuffer.AsSpan(Position));
Position += sizeof(long);
return longValue;
```

### `PutInt(int value)` (line 241–246) and `PutInt(int index, int value)` (line 256–261)

Replace `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))` + `Array.Copy` with `BinaryPrimitives.WriteInt32BigEndian`.

### `GetInt` (line 120–125) and `GetShort` (line 109–114)

Replace `IPAddress.HostToNetworkOrder(BitConverter.ToInt32/16(...))` with `BinaryPrimitives.ReadInt32BigEndian` / `ReadInt16BigEndian`.

### `PutDouble` (line 278–285)

```csharp
// After — no allocation, no Array.Reverse
BinaryPrimitives.WriteInt64BigEndian(_internalBuffer.AsSpan(Position), BitConverter.DoubleToInt64Bits(value));
Position += sizeof(double);
```

### `GetDouble` (line 142–147)

```csharp
// After — replaces ToInt64/CheckedFromBytes/FromBytes/CheckByteArgument chain
var longBits = BinaryPrimitives.ReadInt64BigEndian(_internalBuffer.AsSpan(Position));
Position += sizeof(double);
return BitConverter.Int64BitsToDouble(longBits);
```

Using `BitConverter.DoubleToInt64Bits` / `Int64BitsToDouble` with `BinaryPrimitives.WriteInt64BigEndian` / `ReadInt64BigEndian` is compatible with `netstandard2.0`, avoiding the need for `BinaryPrimitives.WriteDoubleBigEndian` which requires .NET 5+.

Once `GetDouble` is rewritten the following private helpers become dead code and should be deleted:

- `Int64BitsToDouble` (line 156–159)
- `ToInt64` (line 167–170)
- `CheckedFromBytes` (line 180–184)
- `CheckByteArgument` (line 196–208)
- `FromBytes` (line 218–226)

The `using System.Net;` import should also be removed once `IPAddress` is no longer referenced.

## Acceptance Criteria

1. All public read/write methods (`GetShort`, `GetInt`, `PutInt`, `PutInt(index,value)`, `GetLong`, `PutLong`, `GetDouble`, `PutDouble`) use `BinaryPrimitives` with `AsSpan`, performing zero intermediate heap allocations.
2. No references to `IPAddress`, `IPAddress.HostToNetworkOrder`, or `IPAddress.NetworkToHostOrder` remain in `ByteBuffer.cs`.
3. No references to `BitConverter.GetBytes` or `Array.Reverse` remain in `ByteBuffer.cs`.
4. The dead private helpers (`ToInt64`, `CheckedFromBytes`, `FromBytes`, `CheckByteArgument`, `Int64BitsToDouble`) are removed.
5. All existing tests pass unchanged.
6. New round-trip unit tests cover: `PutInt`/`GetInt`, `PutLong`/`GetLong`, `PutDouble`/`GetDouble`, and `PutInt(index, value)`.
7. A new benchmark class exists in `HdrHistogram.Benchmarking/` demonstrating the allocation difference.
8. The project builds and tests pass on all target frameworks: `net8.0`, `net9.0`, `net10.0`, `netstandard2.0`.
9. `dotnet format` passes with no warnings.

## Test Strategy

### Unit tests to add (`ByteBufferTests.cs`)

Add a new test class `ByteBufferReadWriteTests` (or extend the existing class) with:

- `PutInt_and_GetInt_roundtrip` — write a known `int`, reset position, read it back, assert equality. Cover positive, negative, and `int.MaxValue`.
- `PutInt_at_index_and_GetInt_roundtrip` — write to a specific index without advancing position; read from that index; assert equality.
- `PutLong_and_GetLong_roundtrip` — same pattern for `long`.
- `PutDouble_and_GetDouble_roundtrip` — same pattern for `double`. Include `double.NaN`, `double.PositiveInfinity`, and `0.0`.
- `GetShort_returns_big_endian_value` — write known bytes in big-endian order into the raw buffer, call `GetShort`, assert result.

All tests should use xUnit `[Theory]` with `[InlineData]` where multiple values are exercised.

### Existing tests

The single existing test (`ReadFrom_returns_all_bytes_when_stream_returns_partial_reads`) must continue to pass unmodified; it exercises a different code path and is unaffected by this change.

### Integration / regression

The existing histogram encoding and decoding tests (round-trip encode/decode of `LongHistogram` via `HistogramEncoderV2`) exercise the full stack and serve as integration regression coverage. These should be confirmed passing.

## Benchmark

Add `HdrHistogram.Benchmarking/ByteBuffer/ByteBufferBenchmark.cs` with:

- `PutLong_Before` / `PutLong_After` benchmarks (or a single parameterised benchmark switching on implementation)
- `GetLong_Before` / `GetLong_After`
- Configured with `[MemoryDiagnoser]` to surface allocation counts

The issue requires before/after benchmark results to accompany the PR. Because the "before" code will be replaced, record baseline numbers from the original code prior to the change, and include them in the PR description.

## Risks and Open Questions

1. **`netstandard2.0` compatibility** — `BinaryPrimitives` is in `System.Buffers.Binary` and `AsSpan()` on arrays requires `System.Memory`. These are available in `netstandard2.0` via the `System.Memory` NuGet package (version 4.5.x). Verify whether `HdrHistogram.csproj` already references this package; add it if not.

2. **`BinaryPrimitives.WriteDoubleBigEndian` not available on `netstandard2.0`** — Mitigated by using `BinaryPrimitives.WriteInt64BigEndian(span, BitConverter.DoubleToInt64Bits(value))` instead, which is available across all target frameworks.

3. **Byte-order correctness** — `IPAddress.HostToNetworkOrder` converts from host byte order (typically little-endian on x86/x64) to big-endian, and `BinaryPrimitives.WriteInt64BigEndian` writes in big-endian unconditionally. The replacement is semantically equivalent. This must be confirmed by the round-trip unit tests on a little-endian host.

4. **`GetShort` semantics** — The current implementation calls `IPAddress.HostToNetworkOrder` on a value read with `BitConverter.ToInt16`, which means it reads the buffer as little-endian and converts. The replacement `BinaryPrimitives.ReadInt16BigEndian` reads directly as big-endian, which is correct. Verify by tracing the callers of `GetShort` (currently only `HistogramDecoder` variants).

5. **`Memory<byte>` / `Span<byte>` refactor** — The issue mentions refactoring `ByteBuffer` to work over `Memory<byte>` or `Span<byte>` to allow caller-supplied pooled memory. This is noted as a secondary suggestion. It is a larger architectural change and should be treated as a separate issue rather than included here, to keep this PR focused and reviewable.
