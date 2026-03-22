You are an orchestrator. Your job is to coordinate subagents, not write code yourself.

Study ./spec/README.md.
Read ./plan/ready/brief.md to understand the goal.
Read ./plan/ready/task.md to find incomplete tasks (marked with `[ ]`).

For each incomplete task:
1. Use the Task tool to delegate implementation to a subagent. Provide the subagent with:
   - The specific task description
   - The relevant file paths and current content
   - The acceptance criteria from the brief
   - The project conventions from EXECUTE.prompt.md and .claude/
2. Use a second Task tool subagent to verify the changes:
   - Run `dotnet build` and confirm it compiles
   - Run `dotnet test` and confirm all tests pass
   - Review the diff against the task requirements
3. If verification fails, delegate a fix to another subagent with the error details.
4. Once the task passes verification, update task.md marking it `[x]`.

After all tasks are marked `[x]`:
1. Use a Task tool subagent to perform a code review:
   - Run `git diff upstream/main -- ':!plan'`
   - Check for: missed edge cases, test coverage gaps, style violations, leftover TODOs
2. If the review identifies issues, append new `[ ]` tasks to task.md describing each fix.
3. If the review is clean, move brief.md and task.md to ./plan/done/

Process as many tasks as you can in this iteration.

Special handling for benchmark tasks (applies when brief category is `performance`):

Baseline capture (Phase 1 benchmark tasks):
- After creating benchmark classes, commit ONLY the benchmark files:
  `git add HdrHistogram.Benchmarking/ && git commit -m "bench: add benchmarks for baseline capture"`
- Run benchmarks in Release configuration. Use `--filter` to target only the relevant benchmarks.
- BenchmarkDotNet outputs markdown tables to stdout and detailed results to `BenchmarkDotNet.Artifacts/`.
  Copy the results table into `plan/benchmarks/baseline.md`.
- Do NOT proceed to Phase 2 implementation tasks until baseline results are captured and saved.

Post-change validation (Phase 3 benchmark tasks):
- Run the exact same benchmark command used for the baseline.
- Save results to `plan/benchmarks/post-change.md`.
- Generate `plan/benchmarks/comparison.md` by reading both files and computing deltas.
- If any benchmark shows a regression, flag it in comparison.md and add a new task to investigate.

Benchmark execution notes:
- Both Phase 1 (baseline) and Phase 3 (final comparison) MUST use default BenchmarkDotNet settings — do NOT use `--job short` for these runs.
- Use `--job short` ONLY for ad-hoc iteration during Phase 2 development.
- Benchmarks that fail to compile or run must be fixed before proceeding.
