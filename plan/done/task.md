# Task List: Issue #107 — Modernize NuGet Package Metadata

> **Status:** Implementation complete as of commit `425f022`.
> All tasks below are **verification tasks** — each confirms one or more acceptance criteria from the brief.
> No new source code, test, or CI changes are required.

---

## 1. Inspect `.csproj` — deprecated properties removed

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<PackageLicenseUrl>` is absent from the file.
  **Why:** Acceptance criterion 5 — deprecated element must not appear.
  **Verify:** `grep -n "PackageLicenseUrl" HdrHistogram/HdrHistogram.csproj` returns no output.

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<PackageIconUrl>` is absent from the file.
  **Why:** Acceptance criterion 6 — deprecated element must not appear.
  **Verify:** `grep -n "PackageIconUrl" HdrHistogram/HdrHistogram.csproj` returns no output.

---

## 2. Inspect `.csproj` — modern properties present

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<PackageLicenseExpression>CC0-1.0 OR BSD-2-Clause</PackageLicenseExpression>` is present.
  **Why:** Acceptance criterion 5 — modern SPDX expression replaces deprecated URL.
  **Verify:** `grep -n "PackageLicenseExpression" HdrHistogram/HdrHistogram.csproj` shows the correct value.

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<PackageIcon>HdrHistogram-icon-64x64.png</PackageIcon>` is present.
  **Why:** Acceptance criterion 6 — modern icon declaration replaces deprecated URL.
  **Verify:** `grep -n "PackageIcon>" HdrHistogram/HdrHistogram.csproj` shows the correct value (no `Url` suffix).

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<PackageReadmeFile>README.md</PackageReadmeFile>` is present.
  **Why:** Acceptance criterion 2 — readme declaration eliminates the "missing readme" informational message.
  **Verify:** `grep -n "PackageReadmeFile" HdrHistogram/HdrHistogram.csproj` shows `README.md`.

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<WarningsAsErrors>NU5125;NU5048</WarningsAsErrors>` is present.
  **Why:** Acceptance criterion 7 — treats both deprecated-metadata warnings as build errors to prevent future regression.
  **Verify:** `grep -n "WarningsAsErrors" HdrHistogram/HdrHistogram.csproj` shows `NU5125;NU5048`.

---

## 3. Inspect `.csproj` — embedding `<None>` items present

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<None Include="../HdrHistogram-icon-64x64.png" Pack="true" PackagePath="" />` is present in an `<ItemGroup>`.
  **Why:** Acceptance criterion 3 — without this declaration the icon is not copied into the `.nupkg`.
  **Verify:** `grep -n "HdrHistogram-icon-64x64.png" HdrHistogram/HdrHistogram.csproj` shows `Pack="true"`.

- [x] **File:** `HdrHistogram/HdrHistogram.csproj`
  **Change:** Confirm `<None Include="../README.md" Pack="true" PackagePath="" />` is present in an `<ItemGroup>`.
  **Why:** Acceptance criterion 4 — without this declaration README.md is not copied into the `.nupkg`.
  **Verify:** `grep -n "README.md" HdrHistogram/HdrHistogram.csproj` shows `Pack="true"`.

---

## 4. Confirm source files exist at expected relative paths

- [x] **File:** `HdrHistogram-icon-64x64.png` (repo root)
  **Change:** Confirm the icon file exists so the relative path `../HdrHistogram-icon-64x64.png` resolves correctly during pack.
  **Why:** Acceptance criterion 3 — file must be present for the embed to succeed.
  **Verify:** `ls -lh HdrHistogram-icon-64x64.png` succeeds.

- [x] **File:** `README.md` (repo root)
  **Change:** Confirm README.md exists so the relative path `../README.md` resolves correctly during pack.
  **Why:** Acceptance criterion 4 — file must be present for the embed to succeed.
  **Verify:** `ls -lh README.md` succeeds.

---

## 5. Build check — no NU5125/NU5048 warnings

- [x] **Command:** `dotnet build -c Release`
  **Change:** n/a — verification step only.
  **Why:** Acceptance criterion 1 — `<WarningsAsErrors>` causes the build to fail if either warning appears, so a green build proves they are absent.
  **Verify:** Exit code 0; no `NU5125` or `NU5048` in output.

---

## 6. Pack check — no warnings, no "missing readme" message

- [x] **Command:** `dotnet pack ./HdrHistogram/HdrHistogram.csproj -c Release --no-build`
  **Change:** n/a — verification step only.
  **Why:** Acceptance criteria 1 & 2 — `dotnet pack` must complete without NU5125, NU5048, or a "missing readme" informational message.
  **Verify:** Exit code 0; no warnings or informational messages about license URL, icon URL, or readme in stdout/stderr.

---

## 7. Package content inspection — icon and readme embedded at root

- [x] **Command:** Inspect the generated `.nupkg` archive for `HdrHistogram-icon-64x64.png` and `README.md`.
  **Change:** n/a — verification step only.
  **Why:** Acceptance criteria 3 & 4 — both files must appear at the package root (not in a subdirectory).
  **Verify:**
  ```
  unzip -l HdrHistogram/bin/Release/HdrHistogram.*.nupkg | grep -E 'README|icon'
  ```
  Output shows entries for `README.md` and `HdrHistogram-icon-64x64.png` without path prefixes.

---

## 8. Unit test check — no regressions

- [x] **Command:** `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release --no-build`
  **Change:** n/a — verification step only.
  **Why:** Acceptance criterion 8 — metadata-only changes must not break existing library tests.
  **Verify:** All tests pass; exit code 0; no failures or errors reported.

---

## Acceptance Criterion Cross-Reference

| Criterion | Task(s) |
|-----------|---------|
| 1. No NU5125 / NU5048 warnings from `dotnet pack` | §5 (build check), §6 (pack check) |
| 2. No "missing readme" informational message | §2 (PackageReadmeFile present), §3 (None Include README.md), §6 (pack check) |
| 3. `.nupkg` contains icon at package root | §2 (PackageIcon present), §3 (None Include icon), §4 (icon file exists), §7 (content inspection) |
| 4. `.nupkg` contains README.md at package root | §2 (PackageReadmeFile present), §3 (None Include README.md), §4 (README.md exists), §7 (content inspection) |
| 5. `PackageLicenseUrl` absent; `PackageLicenseExpression` present | §1 (removed), §2 (present) |
| 6. `PackageIconUrl` absent; `PackageIcon` present | §1 (removed), §2 (present) |
| 7. `WarningsAsErrors` prevents future regression | §2 (WarningsAsErrors present), §5 (build check enforces it) |
| 8. All existing unit tests pass | §8 (unit test check) |
