# Brief: Issue #161 — Drop netstandard2.0 target, support net8.0+ only

## Summary

Remove `netstandard2.0` from the main library's `<TargetFrameworks>`, retaining only `net10.0;net9.0;net8.0`.
This eliminates all conditional compilation guards that existed solely to support the older target, simplifies `Bitwise.cs` to use `BitOperations.LeadingZeroCount` unconditionally, and cleans up a branching code path in `HistogramLogReader.cs`.
The three per-framework Release `PropertyGroup` conditions in the csproj are collapsed into a single unconditional `PropertyGroup` as part of this change.
The spec file `build-system.md` must be updated to reflect the new target list.

Previous NuGet releases remain available on NuGet.org for consumers on .NET Framework or older runtimes.

## Category

`chore`

This is a maintenance and simplification task.
The hardware-intrinsic `LeadingZeroCount` path (`#if NET5_0_OR_GREATER`) is already compiled in for all net8.0+ builds; removing the fallback does not change the compiled output for any net8.0, net9.0, or net10.0 consumer.
No performance improvement is delivered to existing net8.0+ consumers — the change reduces build artefacts, removes dead code, and simplifies the codebase.

## Affected Files (confirmed by exploration)

| File | Change required |
|------|----------------|
| `HdrHistogram/HdrHistogram.csproj` | Remove `netstandard2.0` from `<TargetFrameworks>` (line 4); delete the netstandard2.0 `PropertyGroup` condition (lines 36-39); collapse the three per-framework Release `PropertyGroup` conditions (lines 24-34) into a single `Condition="'$(Configuration)' == 'Release'"` block |
| `HdrHistogram/Utilities/Bitwise.cs` | Remove `#if NET5_0_OR_GREATER` guards (lines 25-29, 32-44); inline `IntrinsicNumberOfLeadingZeros` body directly into `NumberOfLeadingZeros`; delete the entire `Bitwise.Imperative` class (lines 55-109) |
| `HdrHistogram/HistogramLogReader.cs` | Remove `#if NETSTANDARD2_0` block in `IsComment()` (lines 241-248); keep the `char` overload unconditionally |
| `spec/tech-standards/build-system.md` | Remove `netstandard2.0` from the TargetFrameworks code block (line 25) and from the target table (line 33) |
| `HdrHistogram.Benchmarking/LeadingZeroCount/LeadingZeroCountBenchmarkBase.cs` | Remove the `ImperativeImplementation` benchmark method (references deleted `Bitwise.Imperative` class) |

No other files in the codebase contain `#if NETSTANDARD` or `#if NET5_0_OR_GREATER` blocks.
Unit-test and benchmarking projects already target `net10.0;net9.0;net8.0` only — no changes needed there.

## Acceptance Criteria

- [ ] Library targets `net10.0;net9.0;net8.0` only; `netstandard2.0` is absent from `HdrHistogram.csproj`
- [ ] No `#if NETSTANDARD` or `#if NET5_0_OR_GREATER` conditional compilation remains anywhere in the library
- [ ] `Bitwise.Imperative` class is fully removed
- [ ] `Bitwise.NumberOfLeadingZeros` calls `System.Numerics.BitOperations.LeadingZeroCount` unconditionally
- [ ] `HistogramLogReader.IsComment` uses `line.StartsWith('#')` (char overload) unconditionally
- [ ] The three per-framework Release `PropertyGroup` conditions are collapsed into a single `Condition="'$(Configuration)' == 'Release'"` block
- [ ] `LeadingZeroCountBenchmarkBase.cs` no longer references `Bitwise.Imperative`; the benchmarking project compiles cleanly
- [ ] All unit tests pass on net8.0, net9.0, and net10.0
- [ ] `spec/tech-standards/build-system.md` no longer references `netstandard2.0`

## Test Strategy

### Existing tests

No unit tests directly target `Bitwise` or `HistogramLogReader.IsComment` in isolation.
The existing suite exercises both paths indirectly via histogram recording and log-reading tests.
Run the full unit-test suite across all three target frameworks after the changes:

```
dotnet test -c Release
```

All tests must pass; no new test failures are acceptable.

### New tests (optional but recommended)

Consider adding a focused unit test in `HdrHistogram.UnitTests/` that asserts `Bitwise.NumberOfLeadingZeros` returns correct values for a range of inputs (including 0, 1, powers-of-two, and `long.MaxValue`).
This is low-risk but provides a regression anchor if `Bitwise.cs` is touched again.

There is no need to add tests for `IsComment` — it is a private static helper with trivial logic.

## Risks and Open Questions

1. **Benchmark class references `Bitwise.Imperative`** — deleting the class will break the `ImperativeImplementation` benchmark at compile time.
   The benchmark must be updated in the same PR (listed in Affected Files above).

2. **NuGet package surface** — removing a target framework is a breaking change for consumers on netstandard2.0.
   This is explicitly accepted: "Previous NuGet releases will remain available."
   The PR description should note this prominently and suggest a major-version bump or release notes entry.

3. **No other conditional compilation found** — the grep over the codebase confirmed only two files contain the relevant `#if` blocks.
   Low risk of missing anything.
