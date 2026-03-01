# Task List: Fix Unresolved XML cref in Bitwise.cs (#106)

## Context

`HdrHistogram/Utilities/Bitwise.cs` line 53 has an XML doc `<see cref="System.Numerics.BitOperations.LeadingZeroCount(ulong)"/>` on the `Imperative` nested class summary.
`System.Numerics.BitOperations` does not exist in `netstandard2.0`, so the compiler emits a `CS1574` warning for that target framework.
The fix is a one-line doc-comment change; no logic or tests need to change.

---

## Tasks

### Implementation

- [x] **Fix XML cref in `HdrHistogram/Utilities/Bitwise.cs:53`**
  - **File:** `HdrHistogram/Utilities/Bitwise.cs`
  - **Line:** 53 (the `<summary>` of the `Imperative` nested class)
  - **Change:** Replace `<see cref="System.Numerics.BitOperations.LeadingZeroCount(ulong)"/>` with `<c>System.Numerics.BitOperations.LeadingZeroCount(ulong)</c>`
  - **Why:** `System.Numerics.BitOperations` is not part of the `netstandard2.0` API surface; using `<c>` renders the method name in monospace without requiring compiler resolution.
  - **Verify:** The word `cref` no longer appears in that `<summary>` block; the method name is still present as formatted code.

---

### Build Verification

- [x] **Build for `netstandard2.0` with CS1574 treated as error**
  - **Command:** `dotnet build HdrHistogram/HdrHistogram.csproj -f netstandard2.0 /warnaserror:CS1574`
  - **Why:** Directly validates acceptance criterion 1 — no `CS1574` warning on the `netstandard2.0` target.
  - **Verify:** Command exits with code 0 and no `CS1574` diagnostic appears in output.

- [x] **Build for `net8.0`**
  - **Command:** `dotnet build HdrHistogram/HdrHistogram.csproj -f net8.0`
  - **Why:** Validates acceptance criterion 2 — the fix must not introduce new warnings or errors on the modern target framework.
  - **Verify:** Command exits with code 0; output contains no new warnings or errors compared to pre-fix baseline.

---

### Regression Testing

- [x] **Run the full test suite**
  - **Command:** `dotnet test`
  - **Why:** Confirms no regressions from the doc-comment change (the fix is IL-invisible, but CI must stay green).
  - **Verify:** All tests pass; no test failures or errors reported.

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion (from brief) | Covered by Task |
|-----------------------------------|-----------------|
| `dotnet build -f netstandard2.0` produces no `CS1574` warning | Build for `netstandard2.0` with CS1574 treated as error |
| `dotnet build -f net8.0` continues to succeed without new warnings | Build for `net8.0` |
| Generated XML documentation for `Imperative` still describes its purpose clearly | Fix XML cref (method name retained as `<c>` text) |
| No runtime behaviour changes | Fix XML cref (doc-comment only; no IL change) + full test suite |
