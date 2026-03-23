# Task List: Issue #161 — Drop netstandard2.0 target, support net8.0+ only

## Implementation Changes

- [x] **`HdrHistogram/HdrHistogram.csproj` line 4** — Remove `netstandard2.0` from `<TargetFrameworks>`, leaving `net10.0;net9.0;net8.0`.
  - **Why:** Drops the legacy target entirely.
  - **Verify:** `<TargetFrameworks>` value contains no `netstandard2.0` token.

- [x] **`HdrHistogram/HdrHistogram.csproj` lines 24–34** — Collapse the three per-framework Release `PropertyGroup` blocks (one each for `net8.0`, `net9.0`, `net10.0`) into a single `PropertyGroup Condition="'$(Configuration)' == 'Release'"` block with a single `<DocumentationFile>` element that resolves via `$(TargetFramework)`.
  - **Why:** Eliminates duplicated XML and the framework-specific condition strings.
  - **Verify:** Only one Release `PropertyGroup` exists; it contains no `$(TargetFramework)` literals in the condition string.

- [x] **`HdrHistogram/HdrHistogram.csproj` lines 36–39** — Delete the `netstandard2.0` `PropertyGroup` block (the one setting `DefineConstants` to `RELEASE;NETSTANDARD2_0`).
  - **Why:** The constant `NETSTANDARD2_0` is no longer needed once the target is removed.
  - **Verify:** No `PropertyGroup` referencing `netstandard2.0` or `NETSTANDARD2_0` remains in the file.

- [x] **`HdrHistogram/Utilities/Bitwise.cs` lines 23–44** — Simplify `NumberOfLeadingZeros(long)` to call `System.Numerics.BitOperations.LeadingZeroCount((ulong)value)` directly; remove the `#if NET5_0_OR_GREATER` / `#else` / `#endif` guards and delete the private `IntrinsicNumberOfLeadingZeros` helper.
  - **Why:** All supported targets (net8.0+) provide `BitOperations.LeadingZeroCount`; the conditional dispatch is dead code.
  - **Verify:** `NumberOfLeadingZeros` body is a single `return System.Numerics.BitOperations.LeadingZeroCount((ulong)value);` statement; no `#if` directives remain in the method or immediately around it.

- [x] **`HdrHistogram/Utilities/Bitwise.cs` lines 55–109** — Delete the entire `Bitwise.Imperative` nested public static class (including the `Lookup` table, `NumberOfLeadingZeros`, `NumberOfLeadingZerosLong`, and `Log2` methods).
  - **Why:** The imperative fallback path is unreachable on net8.0+; removing it eliminates dead code and the public surface that the benchmark references.
  - **Verify:** No `class Imperative` or `Bitwise.Imperative` identifier exists anywhere in the solution.

- [x] **`HdrHistogram/HistogramLogReader.cs` lines 241–248** — Remove the `#if NETSTANDARD2_0` / `#else` / `#endif` block inside `IsComment(string line)`, keeping only `return line.StartsWith('#');`.
  - **Why:** The `char` overload of `StartsWith` is available on all net8.0+ targets; the string-overload fallback is dead code.
  - **Verify:** `IsComment` contains no `#if` directives; the method body is `return line.StartsWith('#');`.

- [x] **`HdrHistogram.Benchmarking/LeadingZeroCount/LeadingZeroCountBenchmarkBase.cs`** — Remove the `"Imperative"` entry from the validation dictionary (line 56) and delete the `ImperativeImplementation()` benchmark method (lines 124–133).
  - **Why:** Both reference `Bitwise.Imperative` which will no longer exist; leaving them causes a compile error.
  - **Verify:** `dotnet build HdrHistogram.Benchmarking/ -c Release` exits with code 0; no reference to `Bitwise.Imperative` remains in the file.

## Unit Tests

- [x] **`HdrHistogram.UnitTests/`** — Add a focused unit test class `BitwiseTests` (e.g. `HdrHistogram.UnitTests/Utilities/BitwiseTests.cs`) that asserts `Bitwise.NumberOfLeadingZeros` returns correct results for representative inputs: `0`, `1`, `2`, powers of two up to 2⁶², and `long.MaxValue`.
  - **Why:** No existing test directly covers `Bitwise`; this provides a regression anchor if the method is ever touched again.
  - **Verify:** Test class exists; `dotnet test -c Release` reports the new tests as passing on all three target frameworks.

- [x] **Run the full unit-test suite** — Execute `dotnet test -c Release` across all three target frameworks (net8.0, net9.0, net10.0).
  - **Why:** Confirms that removing the netstandard2.0 conditional paths has not broken any indirect consumer of `Bitwise` or `HistogramLogReader`.
  - **Verify:** Zero test failures; zero skipped tests that were previously passing.

## Documentation

- [x] **`spec/tech-standards/build-system.md` line 25** — Remove `netstandard2.0` from the `<TargetFrameworks>` code block example.
  - **Why:** The spec must reflect the actual supported targets.
  - **Verify:** The code block contains only `net10.0;net9.0;net8.0`.

- [x] **`spec/tech-standards/build-system.md` line 33** — Delete the `| \`netstandard2.0\` | Broad compatibility (.NET Framework 4.6.1+, .NET Core 2.0+) |` row from the target table.
  - **Why:** The target no longer exists; the row is misleading.
  - **Verify:** No mention of `netstandard2.0` remains anywhere in `build-system.md`.

---

## Acceptance Criteria Cross-Reference

| Acceptance criterion (from brief) | Covered by task |
|---|---|
| Library targets `net10.0;net9.0;net8.0` only; `netstandard2.0` absent from `.csproj` | Task 1 (TargetFrameworks) |
| No `#if NETSTANDARD` or `#if NET5_0_OR_GREATER` conditional compilation in library | Tasks 4, 6 (Bitwise.cs, HistogramLogReader.cs) |
| `Bitwise.Imperative` class fully removed | Task 5 |
| `Bitwise.NumberOfLeadingZeros` calls `BitOperations.LeadingZeroCount` unconditionally | Task 4 |
| `HistogramLogReader.IsComment` uses `line.StartsWith('#')` unconditionally | Task 6 |
| Three per-framework Release `PropertyGroup` conditions collapsed into one | Task 2 |
| `LeadingZeroCountBenchmarkBase.cs` no longer references `Bitwise.Imperative`; benchmarking project compiles | Task 7 |
| All unit tests pass on net8.0, net9.0, net10.0 | Tasks 8 (new tests), 9 (full suite) |
| `spec/tech-standards/build-system.md` no longer references `netstandard2.0` | Tasks 10, 11 |
