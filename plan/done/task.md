# Task List: Issue #141 — ByteBuffer Allocation Elimination

Cross-referenced against all acceptance criteria in `brief.md`.

---

## 1. Project Configuration

- [x] **`HdrHistogram/HdrHistogram.csproj`** — Add a conditional `<PackageReference>` for `System.Memory` (version `4.5.*`) scoped to `netstandard2.0` only.
  The `BinaryPrimitives` type lives in `System.Buffers.Binary`, which ships in `System.Memory` for `netstandard2.0`; `net8.0`/`net9.0`/`net10.0` include it in-box.
  **Verify:** `dotnet restore` succeeds; `dotnet build` succeeds on all four target frameworks.

---

## 2. Implementation Changes — `HdrHistogram/Utilities/ByteBuffer.cs`

- [x] **Add `using System.Buffers.Binary;`** at the top of the file.
  Required before any `BinaryPrimitives` call compiles.
  **Verify:** File compiles without an unresolved-type error.

- [x] **`GetShort` (line 109–114)** — Replace `IPAddress.HostToNetworkOrder(BitConverter.ToInt16(_internalBuffer, Position))` with `BinaryPrimitives.ReadInt16BigEndian(_internalBuffer.AsSpan(Position))`.
  Reads the 16-bit big-endian value directly; no intermediate allocation.
  **Verify:** No reference to `BitConverter` or `IPAddress` remains in this method.

- [x] **`GetInt` (line 120–125)** — Replace `IPAddress.HostToNetworkOrder(BitConverter.ToInt32(_internalBuffer, Position))` with `BinaryPrimitives.ReadInt32BigEndian(_internalBuffer.AsSpan(Position))`.
  **Verify:** No reference to `BitConverter` or `IPAddress` remains in this method.

- [x] **`GetLong` (line 131–136)** — Replace `IPAddress.HostToNetworkOrder(BitConverter.ToInt64(_internalBuffer, Position))` with `BinaryPrimitives.ReadInt64BigEndian(_internalBuffer.AsSpan(Position))`.
  **Verify:** No reference to `BitConverter` or `IPAddress` remains in this method.

- [x] **`GetDouble` (line 142–147)** — Replace the `ToInt64` → `CheckedFromBytes` → `FromBytes` call chain with:
  ```csharp
  var longBits = BinaryPrimitives.ReadInt64BigEndian(_internalBuffer.AsSpan(Position));
  Position += sizeof(double);
  return BitConverter.Int64BitsToDouble(longBits);
  ```
  **Verify:** Method body references neither `ToInt64` nor any private helper; result is semantically equivalent.

- [x] **`PutInt(int value)` (line 241–246)** — Replace `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))` + `Array.Copy` with `BinaryPrimitives.WriteInt32BigEndian(_internalBuffer.AsSpan(Position), value); Position += sizeof(int);`.
  **Verify:** No `BitConverter.GetBytes` or `Array.Copy` call remains in this method.

- [x] **`PutInt(int index, int value)` (line 256–261)** — Replace `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))` + `Array.Copy` with `BinaryPrimitives.WriteInt32BigEndian(_internalBuffer.AsSpan(index), value);` (position must NOT advance).
  **Verify:** Position is not modified; no `BitConverter.GetBytes` call remains.

- [x] **`PutLong` (line 267–272)** — Replace `BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value))` + `Array.Copy` with `BinaryPrimitives.WriteInt64BigEndian(_internalBuffer.AsSpan(Position), value); Position += sizeof(long);`.
  **Verify:** No `BitConverter.GetBytes` or `Array.Copy` call remains in this method.

- [x] **`PutDouble` (line 278–285)** — Replace `BitConverter.GetBytes` + `Array.Reverse` with:
  ```csharp
  BinaryPrimitives.WriteInt64BigEndian(_internalBuffer.AsSpan(Position), BitConverter.DoubleToInt64Bits(value));
  Position += sizeof(double);
  ```
  **Verify:** No `Array.Reverse` or `BitConverter.GetBytes` call remains in this method.

---

## 3. Dead Code Removal — `HdrHistogram/Utilities/ByteBuffer.cs`

These five private helpers are unreachable once `GetDouble` is rewritten (acceptance criterion 4).
Remove them in a single edit to keep the diff reviewable.

- [x] **Delete `Int64BitsToDouble` (line 156–159)** — Thin wrapper; callers replaced.
- [x] **Delete `ToInt64` (line 167–170)** — Only called by `CheckedFromBytes`; now unused.
- [x] **Delete `CheckedFromBytes` (line 180–184)** — Only called by `ToInt64`; now unused.
- [x] **Delete `CheckByteArgument` (line 196–208)** — Only called by `CheckedFromBytes`; now unused.
- [x] **Delete `FromBytes` (line 218–226)** — Only called by `CheckedFromBytes`; now unused.
  **Verify:** `dotnet build` reports zero compiler warnings about unreachable/unused code; no `CS0219` or `CS8321` warnings.

---

## 4. Import Cleanup — `HdrHistogram/Utilities/ByteBuffer.cs`

- [x] **Remove `using System.Net;`** — `IPAddress` is no longer referenced anywhere in the file after the implementation changes above.
  **Verify:** No `CS0246` (type not found) or `IDE0005` (unnecessary using) warnings after removal; `dotnet build` succeeds.

---

## 5. Unit Tests — `HdrHistogram.UnitTests/Utilities/ByteBufferTests.cs`

Add a new `ByteBufferReadWriteTests` class (or extend the existing `ByteBufferTests` class) using xUnit `[Theory]` / `[InlineData]`.

- [x] **`PutInt_and_GetInt_roundtrip`** — Write a known `int` via `PutInt`, reset `Position` to 0, read via `GetInt`, assert equality.
  Use `[InlineData]` with at least: a positive value, a negative value, and `int.MaxValue`.
  **Verify:** All three inline cases pass; position advances by `sizeof(int)` (4).

- [x] **`PutInt_at_index_and_GetInt_roundtrip`** — Call `PutInt(index, value)` at a non-zero index; confirm `Position` did not change; read from the same index; assert equality.
  Use `[InlineData]` with at least two different (index, value) pairs.
  **Verify:** Position is unchanged after the indexed write; read-back equals the written value.

- [x] **`PutLong_and_GetLong_roundtrip`** — Same pattern for `long`.
  Use `[InlineData]` with at least: a positive value, a negative value, and `long.MaxValue`.
  **Verify:** All three inline cases pass; position advances by `sizeof(long)` (8).

- [x] **`PutDouble_and_GetDouble_roundtrip`** — Same pattern for `double`.
  Use `[InlineData]` with at least: `0.0`, `double.NaN`, `double.PositiveInfinity`, and a normal finite value.
  Note: `double.NaN` equality requires `BitConverter.DoubleToInt64Bits` comparison, not `==`.
  **Verify:** All inline cases pass; position advances by `sizeof(double)` (8).

- [x] **`GetShort_returns_big_endian_value`** — Allocate a `ByteBuffer`, write two bytes in known big-endian order directly into the internal buffer (or via `BlockCopy`), call `GetShort`, assert the expected `short` value.
  Use `[InlineData]` with at least two known byte sequences.
  **Verify:** Result matches the expected big-endian interpretation.

- [x] **Confirm existing test is unmodified and still passes** — `ReadFrom_returns_all_bytes_when_stream_returns_partial_reads` must pass without any change to its body or the `PartialReadStream` helper.
  **Verify:** Test run output shows this test green.

---

## 6. Integration / Regression Confirmation

- [x] **Run the full unit test suite** (`dotnet test HdrHistogram.UnitTests/`) and confirm all histogram encoding/decoding tests pass unchanged.
  These tests exercise `HistogramEncoderV2`, which calls every rewritten `ByteBuffer` method, serving as integration regression coverage.
  **Verify:** Zero test failures; zero skipped tests introduced by this change.

---

## 7. Benchmarks — `HdrHistogram.Benchmarking/ByteBuffer/ByteBufferBenchmark.cs`

- [x] **Create directory `HdrHistogram.Benchmarking/ByteBuffer/`** and add `ByteBufferBenchmark.cs` with:
  - `[MemoryDiagnoser]` attribute on the benchmark class.
  - `PutLong_After` benchmark — calls `PutLong` in a loop using the new `BinaryPrimitives` implementation.
  - `GetLong_After` benchmark — calls `GetLong` in a loop using the new `BinaryPrimitives` implementation.
  - Buffer setup in `[GlobalSetup]` so allocation inside setup is excluded from measurements.
  **Verify:** `dotnet build HdrHistogram.Benchmarking/` succeeds; the class is discovered by BenchmarkDotNet when run with `--list flat`.

- [x] **Record baseline benchmark numbers** from the original code before any changes and include them in the PR description as a before/after table.
  (Because the "before" code will be deleted, run BenchmarkDotNet against the original branch first.)
  **Verify:** PR description contains an `Allocated` column comparison showing zero allocation in the "After" rows.
  **Note:** Baseline numbers must be captured from the original branch before merging and included in the PR description.

---

## 8. Format and Build Verification

- [x] **`dotnet format HdrHistogram/`** — Run after all implementation and dead-code-removal changes; fix any reported issues.
  **Verify:** Command exits with code 0 and reports no files changed (or all changes were intentional).

- [x] **`dotnet format HdrHistogram.UnitTests/`** — Run after adding new tests.
  **Verify:** Command exits with code 0.

- [x] **`dotnet format HdrHistogram.Benchmarking/`** — Run after adding the benchmark class.
  **Verify:** Command exits with code 0.

- [x] **Multi-framework build check** — `dotnet build HdrHistogram/ -f netstandard2.0`, then repeat for `net8.0`, `net9.0`, `net10.0`.
  **Verify:** Zero errors and zero warnings on all four target frameworks.

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion | Covered By |
|---|---|
| 1. All public read/write methods use `BinaryPrimitives` with `AsSpan`, zero intermediate allocations | Tasks in §2 |
| 2. No references to `IPAddress`, `HostToNetworkOrder`, or `NetworkToHostOrder` in `ByteBuffer.cs` | Tasks in §2 + §4 |
| 3. No references to `BitConverter.GetBytes` or `Array.Reverse` in `ByteBuffer.cs` | Tasks in §2 |
| 4. Dead helpers (`ToInt64`, `CheckedFromBytes`, `FromBytes`, `CheckByteArgument`, `Int64BitsToDouble`) removed | Tasks in §3 |
| 5. All existing tests pass unchanged | Tasks in §5 (last item) + §6 |
| 6. New round-trip tests: `PutInt`/`GetInt`, `PutLong`/`GetLong`, `PutDouble`/`GetDouble`, `PutInt(index,value)` | Tasks in §5 |
| 7. New `ByteBufferBenchmark` class with `[MemoryDiagnoser]` in `HdrHistogram.Benchmarking/` | Tasks in §7 |
| 8. Builds and tests pass on `net8.0`, `net9.0`, `net10.0`, `netstandard2.0` | §1 + §8 |
| 9. `dotnet format` passes with no warnings | Tasks in §8 |
