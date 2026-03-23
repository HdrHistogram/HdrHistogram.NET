# Brief: Issue #161 — Drop netstandard2.0 target, support net8.0+ only

## Summary

Remove `netstandard2.0` from the main library's `<TargetFrameworks>`, retaining only `net10.0;net9.0;net8.0`.
This eliminates all conditional compilation guards that existed solely to support the older target, simplifies `Bitwise.cs` to use `BitOperations.LeadingZeroCount` unconditionally, and cleans up a branching code path in `HistogramLogReader.cs`.
The spec file `build-system.md` must be updated to reflect the new target list.

Previous NuGet releases remain available on NuGet.org for consumers on .NET Framework or older runtimes.

## Category

`performance`

Performance is a primary motivation: the netstandard2.0 code path uses a slow, lookup-table-based `LeadingZeroCount` rather than the `BitOperations` hardware intrinsic.
This change also delivers simplification and a reduction in build artefacts, but the performance impact on a hot instrumentation path is the key driver.

## Affected Files (confirmed by exploration)

| File | Change required |
|------|----------------|
| `HdrHistogram/HdrHistogram.csproj` | Remove `netstandard2.0` from `<TargetFrameworks>` (line 4); delete the netstandard2.0 `PropertyGroup` condition (lines 36-39) |
| `HdrHistogram/Utilities/Bitwise.cs` | Remove `#if NET5_0_OR_GREATER` guards (lines 25-29, 32-44); inline `IntrinsicNumberOfLeadingZeros` body directly into `NumberOfLeadingZeros`; delete the entire `Bitwise.Imperative` class (lines 55-109) |
| `HdrHistogram/HistogramLogReader.cs` | Remove `#if NETSTANDARD2_0` block in `IsComment()` (lines 241-248); keep the `char` overload unconditionally |
| `spec/tech-standards/build-system.md` | Remove `netstandard2.0` from the TargetFrameworks code block (line 25) and from the target table (line 33) |

No other files in the codebase contain `#if NETSTANDARD` or `#if NET5_0_OR_GREATER` blocks.
Unit-test and benchmarking projects already target `net10.0;net9.0;net8.0` only — no changes needed there.

## Acceptance Criteria

- [ ] Library targets `net10.0;net9.0;net8.0` only; `netstandard2.0` is absent from `HdrHistogram.csproj`
- [ ] No `#if NETSTANDARD` or `#if NET5_0_OR_GREATER` conditional compilation remains anywhere in the library
- [ ] `Bitwise.Imperative` class is fully removed
- [ ] `Bitwise.NumberOfLeadingZeros` calls `System.Numerics.BitOperations.LeadingZeroCount` unconditionally
- [ ] `HistogramLogReader.IsComment` uses `line.StartsWith('#')` (char overload) unconditionally
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

## Benchmark Strategy

### Why benchmarks are required

This is a `performance` category issue.
The stated motivation is that the netstandard2.0 fallback path is slower.
Benchmarks must confirm that the hardware-intrinsic path is indeed faster and that the improvement is observable at both micro and end-to-end level.

### Existing relevant benchmarks

`HdrHistogram.Benchmarking/LeadingZeroCount/` already contains benchmarks for this exact area:

- `LeadingZeroCountBenchmarkBase.cs` — benchmarks `Bitwise.NumberOfLeadingZeros()` (intrinsic path, line 119) and `Bitwise.Imperative.NumberOfLeadingZeros()` (fallback, line 130)

These benchmarks were written to compare the two implementations.
After `Bitwise.Imperative` is deleted, the `ImperativeImplementation` benchmark method must be removed or updated.

### Benchmark development plan

**Phase 1 — Baseline (before any code changes):**

1. Run the existing `LeadingZeroCount` benchmarks in Release mode against the current code to capture baseline metrics for both the intrinsic and imperative paths.
   Record: Mean, StdDev, Allocated, Op/s.
2. If an end-to-end benchmark for `RecordValue` throughput exists (check `Recording/`), run it too.

**Phase 2 — Implementation:**

3. Apply the changes described above.
4. Remove the `ImperativeImplementation` benchmark method from `LeadingZeroCountBenchmarkBase.cs` (it references the deleted class).
5. Update `Program.cs` `BenchmarkSwitcher` if the benchmark class is renamed.

**Phase 3 — Validation:**

6. Re-run the `LeadingZeroCount` micro-benchmarks and any `Recording` end-to-end benchmarks.
7. Produce a before/after table (Mean, Allocated, Op/s) and include it in the PR description.

**Metrics that matter:**

| Metric | Relevance |
|--------|-----------|
| Mean throughput (Op/s) | Primary — shows intrinsic is faster than the lookup table |
| Allocated (bytes) | Secondary — both paths should allocate nothing; confirms no regression |
| GC Gen0/Gen1 collections | Sanity check — should remain zero for both paths |

A micro-benchmark improvement in `LeadingZeroCount` that does not translate to observable improvement in `RecordValue` throughput is expected (the intrinsic is already used on net8.0+), but the benchmark result still validates correctness and code simplification.

## Risks and Open Questions

1. **Benchmark class references `Bitwise.Imperative`** — deleting the class will break the `ImperativeImplementation` benchmark at compile time.
   The benchmark must be updated in the same PR.

2. **NuGet package surface** — removing a target framework is a breaking change for consumers on netstandard2.0.
   This is explicitly accepted: "Previous NuGet releases will remain available."
   The PR description should note this prominently and suggest a major-version bump or release notes entry.

3. **No other conditional compilation found** — the grep over the codebase confirmed only two files contain the relevant `#if` blocks.
   Low risk of missing anything.

4. **Documentation file `PropertyGroup` condition** — the existing per-framework Release `PropertyGroup` conditions for net8.0, net9.0, net10.0 (lines 24-34 of the csproj) can be collapsed into a single unconditional `PropertyGroup Condition="'$(Configuration)' == 'Release'"` as shown in `build-system.md`.
   This is a tidy-up that is in scope but should be confirmed with the implementer.
