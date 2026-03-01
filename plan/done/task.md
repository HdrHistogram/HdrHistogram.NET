# Task List: Issue #105 — Remove EOL Target Frameworks from Benchmarking Project

## Implementation Changes

### Task 1 — Update TargetFrameworks in HdrHistogram.Benchmarking.csproj

**File:** `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`

**Change:** Replace the multi-value `<TargetFrameworks>` element on line 5 with a single `net8.0` target.

**From:**
```xml
<TargetFrameworks>net8.0;net7.0;net6.0;net5.0;net47;netcoreapp3.1;netcoreapp2.1.29</TargetFrameworks>
```

**To:**
```xml
<TargetFrameworks>net8.0</TargetFrameworks>
```

**Why:** Eliminates all five EOL frameworks from the benchmarking project, resolving `NETSDK1138` warnings.

**Verification:** Open the file and confirm the `<TargetFrameworks>` element contains exactly `net8.0` and no semicolons.

- [x] Replace the `<TargetFrameworks>` value in `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` with `net8.0`

---

### Task 2 — Update the Benchmarking Project TFM block in build-system.md

**File:** `spec/tech-standards/build-system.md`

**Change:** Lines 39–45 — replace the "Multi-targeted for performance comparison" description and the multi-framework XML snippet with the single-target equivalent.

**From (lines 39–45):**
```markdown
### Benchmarking Project

Multi-targeted for performance comparison:

```xml
<TargetFrameworks>net8.0;net7.0;net6.0;net5.0;net47;netcoreapp3.1;netcoreapp2.1.29</TargetFrameworks>
```
```

**To:**
```markdown
### Benchmarking Project

Targets the current LTS runtime only (developer tool, not a shipped library):

```xml
<TargetFrameworks>net8.0</TargetFrameworks>
```
```

**Why:** The spec must match the actual project file so future readers are not misled.

**Verification:** Lines 39–45 of `build-system.md` show `net8.0` only, with no EOL framework identifiers and no "Multi-targeted for performance comparison" text.

- [x] Update the Benchmarking Project TFM block (lines 39–45) in `spec/tech-standards/build-system.md`

---

### Task 3 — Update the Benchmark Configuration bullet in build-system.md

**File:** `spec/tech-standards/build-system.md`

**Change:** Line 227 — replace the "Multiple .NET versions for comparison" bullet with a description that reflects the single `net8.0` target.

**From (lines 226–229):**
```markdown
BenchmarkDotNet is used with these targets:
- Multiple .NET versions for comparison
- Windows diagnostics support
- Memory allocation tracking
```

**To:**
```markdown
BenchmarkDotNet is used with these targets:
- `net8.0` (current LTS runtime)
- Windows diagnostics support
- Memory allocation tracking
```

**Why:** The "Multiple .NET versions for comparison" bullet no longer holds after reducing to a single target; leaving it in place would contradict the updated TFM block above it.

**Verification:** Line 227 (or equivalent) contains `` `net8.0` (current LTS runtime) `` and no reference to "Multiple .NET versions".

- [x] Update the Benchmark Configuration bullet (line 227) in `spec/tech-standards/build-system.md`

---

## Tests

No unit tests exist in the benchmarking project and the brief explicitly states that no new tests need to be added or modified.
Verification is build-only (see Build Verification tasks below).

---

## Build Verification

### Task 4 — Verify the benchmarking project builds without NETSDK1138 warnings

**Command:** `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release`

**Why:** Confirms that the EOL framework removal eliminates the `NETSDK1138` warnings and that the project still compiles cleanly under `net8.0`.

**Verification:** Command exits with code 0 and the output contains zero occurrences of `NETSDK1138`.

- [x] Run `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release` and confirm zero `NETSDK1138` warnings and exit code 0

---

### Task 5 — Verify the full solution builds successfully

**Command:** `dotnet build -c Release` (from repo root)

**Why:** Ensures that removing the EOL target frameworks from the benchmarking project does not introduce regressions in any other project in the solution.

**Verification:** Command exits with code 0 and output shows all projects (main library, unit tests, examples, benchmarking) built successfully.

- [x] Run `dotnet build -c Release` from the repo root and confirm the full solution builds with exit code 0 and no regressions

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion (from brief) | Covered by |
|-----------------------------------|------------|
| `HdrHistogram.Benchmarking.csproj` contains no EOL target frameworks | Task 1 |
| `dotnet build -c Release` completes with zero `NETSDK1138` warnings | Task 4 |
| `dotnet build -c Release` completes successfully for the whole solution | Task 5 |
| `build-system.md` Benchmarking Project TFM block reflects `net8.0` only | Task 2 |
| `build-system.md` Benchmark Configuration section accurately describes the final `TargetFrameworks` value | Task 3 |
