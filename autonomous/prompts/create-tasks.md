Study ./spec/README.md
Read ./plan/ready/brief.md.

Use the Task tool to delegate codebase exploration to a subagent:
- Examine the files identified in the brief
- Map the current implementation, dependencies, and test coverage
- Return a summary of what exists and what needs to change

Using the exploration results, create ./plan/ready/task.md with a checklist.
Each task should be:
- Specific: which file, what change, why
- Atomic: one logical change per task
- Verifiable: how to confirm it's done
- Ordered: dependencies respected

Include tasks for:
- Implementation changes
- Updating or adding unit tests
- Updating XML doc comments if public API changes

Use `[ ]` for each task. 
Validate the task list is complete by cross-referencing every acceptance criterion in the brief — each criterion must be covered by at least one task.

Any task that attempts to alter the ./.github folder will likely fail due to permissions restrictions. These changes should accompany the PR as an attached file with clear direction on the manual intervention required to complete the work.

If the brief category is `performance`, follow the benchmark-driven development process
in spec/tech-standards/testing-standards.md. The task ordering MUST be:

Phase 1 — Benchmark scaffolding (before any implementation changes):
  - [ ] Create benchmark class(es) in HdrHistogram.Benchmarking/
    - Micro-benchmarks for the specific operations being optimised
    - End-to-end benchmarks that exercise the realistic user workflow
    - Add [MemoryDiagnoser] to all benchmark classes
    - Register new benchmarks in Program.cs BenchmarkSwitcher
  - [ ] Build and verify benchmarks compile:
    `dotnet build HdrHistogram.Benchmarking/ -c Release`
  - [ ] Run baseline benchmarks on the UNMODIFIED code
    - Run: `dotnet run -c Release --project HdrHistogram.Benchmarking/ -- --filter '*BenchmarkClass*' --exporters json`
    - Save formatted results table to `plan/benchmarks/baseline.md`
    - Include: Mean, StdDev, Allocated, Op/s for each benchmark method

Phase 2 — Implementation (the actual code changes):
  - [ ] (implementation tasks as normal)
  - [ ] (unit tests as normal)

Phase 3 — Benchmark validation (after implementation is complete):
  - [ ] Run post-change benchmarks with identical configuration
    - Save results to `plan/benchmarks/post-change.md`
  - [ ] Generate comparison in `plan/benchmarks/comparison.md` containing:
    - Side-by-side table: Benchmark | Baseline | Post-Change | Delta | Delta %
    - Summary: which metrics improved, which regressed, which unchanged
    - Verdict: does the data support the change?

Create the directory `plan/benchmarks/` for storing results.

If the brief category is `functional`, use the current ordering (implementation, tests, docs).