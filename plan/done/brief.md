# Brief: Issue #66 — Document Unit Tests and Benchmarks

## Issue

**Number:** #66
**Title:** Document Unit Tests and Benchmarks
**Branch:** `agent/66-document-unit-tests-and-benchmarks`

## Summary

The project `README.md` contains no information about how to run the unit test suite or the benchmarking project.
Developers contributing to the project have no discoverable entry point for running tests or benchmarks from the command line.
The fix is to add a dedicated section to `README.md` that shows the exact `dotnet` CLI commands for both activities.

## Affected Files

Confirmed by exploration:

| File | Change |
|------|--------|
| `README.md` | Add "Running the Tests" and "Running the Benchmarks" sections |

No code changes are required.
No spec files need updating (the testing standards already document the framework stack; this is purely user-facing README content).

## Project Structure (for reference)

- **Unit test project:** `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj`
  - Framework: xUnit 2.9.0, FluentAssertions 6.12.0, Xunit.Combinatorial 1.6.24
  - Target framework: `net8.0`
- **Benchmarking project:** `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`
  - Framework: BenchmarkDotNet 0.13.12
  - Target framework: `net8.0`

## Acceptance Criteria

1. `README.md` contains a section explaining how to run the unit tests using `dotnet test`.
2. The section includes at minimum the quiet/Release invocation from the issue:
   `dotnet test .\HdrHistogram.UnitTests\HdrHistogram.UnitTests.csproj -v=q -c=Release`
3. `README.md` contains a section explaining how to run the benchmarks using `dotnet run -c Release`.
4. Both sections follow the project's Markdown standards (British English, one sentence per line, blank line under headings, blank lines around lists).
5. The content is discoverable — placed logically within the existing README structure (e.g., under a "Development" or "Contributing" heading, or as its own top-level section).

## Test Strategy

This issue is documentation-only; there are no source code or test file changes.
Verification is manual:

- Confirm the `dotnet test` command in the README executes successfully against the repo.
- Confirm the benchmark `dotnet run -c Release` command compiles and runs.
- Confirm Markdown formatting passes any lint checks present in CI.

No new automated tests are needed.

## Risks and Open Questions

- **Placement in README:** The current README has no "Development" or "Contributing" section.
  A new top-level section must be added.
  Decide whether to name it "Development" (broader) or "Running Tests" (narrower).
  Using "Development" is preferred as it can also house the benchmarks subsection.

- **Path separators:** The issue uses Windows-style backslashes (`.\`).
  The README should use forward slashes (`./`) for cross-platform compatibility, or show both.

- **Verbosity flag syntax:** The issue uses `-v=q`; the canonical dotnet CLI form is `-v q` (space, not equals).
  Both are accepted by the SDK; use the space form as it is more conventional.

- **Benchmark run time:** BenchmarkDotNet full runs are slow.
  The section should note that benchmarks must be run in Release mode and may take several minutes.
