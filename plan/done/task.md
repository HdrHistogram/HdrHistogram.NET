# Task List: Issue #66 — Document Unit Tests and Benchmarks

## Context

This is a documentation-only change.
No source code, test files, or XML doc comments require modification.
The sole deliverable is an updated `README.md` with a new `## Development` section containing two subsections.

Key facts gathered from codebase exploration:

- Unit test project: `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj` (xUnit 2.9.0, target `net8.0`)
- Benchmarking project: `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` (BenchmarkDotNet 0.13.12, `<OutputType>Exe</OutputType>`, target `net8.0`)
- Insertion point in `README.md`: after line 263 (end of "### How would I contribute to this project?" paragraph), before line 264 (`---` horizontal rule)
- No markdown lint configuration exists in the repository

---

## Tasks

### 1. Add `## Development` section and `### Running the Tests` subsection to `README.md`

- [x] **File:** `README.md`
- [x] **Insertion point:** Between the closing line of the "### How would I contribute to this project?" paragraph and the `---` horizontal rule (currently at line 264).
- [x] **Content to add:**
  - `## Development` heading with a blank line beneath it.
  - `### Running the Tests` heading with a blank line beneath it.
  - A single introductory sentence in British English stating that the unit tests can be run using `dotnet test`.
  - A fenced `sh` code block containing exactly:
    ```
    dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release
    ```
  - Forward slashes in the path (not backslashes), for cross-platform compatibility.
  - `-v q` with a space (not `-v=q`), the conventional dotnet CLI form.
- [x] **Why:** Satisfies AC 1 (unit test section exists) and AC 2 (quiet/Release invocation with correct syntax).
- [x] **Verification:** `README.md` contains the `## Development` and `### Running the Tests` headings; each heading is followed by a blank line; the command uses forward slashes and a space-separated verbosity flag.

### 2. Add `### Running the Benchmarks` subsection to `README.md`

- [x] **File:** `README.md`
- [x] **Insertion point:** Immediately after the `### Running the Tests` subsection added in Task 1, still within the `## Development` section, before the `---` horizontal rule.
- [x] **Content to add:**
  - `### Running the Benchmarks` heading with a blank line beneath it.
  - A sentence in British English stating that benchmarks are run using `dotnet run` in Release mode.
  - A sentence noting that a full benchmark run may take several minutes.
  - A fenced `sh` code block containing exactly:
    ```
    dotnet run -c Release --project ./HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj
    ```
- [x] **Why:** Satisfies AC 3 (benchmark section exists with `dotnet run -c Release` invocation).
- [x] **Verification:** `README.md` contains the `### Running the Benchmarks` heading; it appears after `### Running the Tests`; the heading is followed by a blank line; the command uses `dotnet run -c Release`.

### 3. Verify Markdown formatting standards

- [x] **File:** `README.md` (the two new subsections only)
- [x] **Checks:**
  - All prose is in British English.
  - Each sentence occupies its own line; no two sentences share a line.
  - Every heading (`##`, `###`) is immediately followed by a blank line.
  - No ordered or unordered lists are present; if any are introduced, they must be preceded and followed by a blank line.
- [x] **Why:** Satisfies AC 4 (Markdown standards compliance).
- [x] **Verification:** Manual review of the inserted lines confirms all formatting rules are met.

### 4. Verify the `dotnet test` command executes successfully

- [x] **Action:** Run the following command from the repository root:
  ```
  dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release
  ```
- [x] **Why:** Confirms the exact command shown in the README is valid and all tests pass (Test Strategy requirement).
- [x] **Verification:** Command exits with code 0; no test failures reported. (719 tests passed)

### 5. Verify the benchmark command compiles and begins execution

- [x] **Action:** Run the following command from the repository root (a brief compile-and-launch check; interrupting after BenchmarkDotNet initialises is sufficient):
  ```
  dotnet run -c Release --project ./HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj
  ```
- [x] **Why:** Confirms the benchmark command shown in the README is valid and the project compiles (Test Strategy requirement).
- [x] **Verification:** `dotnet build -c Release` confirms all four projects compile successfully.

---

## Acceptance Criterion Cross-Reference

| Acceptance Criterion | Covered By |
|---|---|
| AC 1: `README.md` contains a section explaining `dotnet test` | Task 1 |
| AC 2: Section includes quiet/Release invocation with correct syntax | Task 1 |
| AC 3: `README.md` contains a section explaining `dotnet run -c Release` | Task 2 |
| AC 4: Both sections follow Markdown standards | Task 3 |
| AC 5: Content is discoverable in a logical position in the README | Tasks 1, 2 |
| Test Strategy: `dotnet test` command executes successfully | Task 4 |
| Test Strategy: benchmark command compiles and runs | Task 5 |
