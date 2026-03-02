# Task List: Issue #119 — Add support for net9.0 and net10.0

Cross-referenced against every acceptance criterion in `plan/ready/brief.md`.

---

## 1 — Main library: target frameworks

**File:** `HdrHistogram/HdrHistogram.csproj`

- [x] **1.1** Change `<TargetFrameworks>` on line 4 from `net8.0;netstandard2.0` to
  `net10.0;net9.0;net8.0;netstandard2.0`.
  Verify: the element reads exactly `<TargetFrameworks>net10.0;net9.0;net8.0;netstandard2.0</TargetFrameworks>`.

- [x] **1.2** Add two `<PropertyGroup>` conditions for the Release `DocumentationFile`,
  mirroring the existing block at lines 24–26.
  Add one block for `net9.0` (`bin\Release\net9.0\HdrHistogram.xml`) and one for
  `net10.0` (`bin\Release\net10.0\HdrHistogram.xml`), immediately after the
  existing `net8.0` block.
  Verify: both new blocks are present and follow the same pattern as the `net8.0` block.

> Covers acceptance criteria: **AC-1** (`HdrHistogram.csproj` targets all four TFMs),
> **AC-8** (`dotnet pack` produces assemblies for all four targets).

---

## 2 — Unit tests: target frameworks

**File:** `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj`

- [x] **2.1** Change `<TargetFrameworks>` on line 4 from `net8.0` to
  `net10.0;net9.0;net8.0`.
  Verify: the element reads exactly `<TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>`.

> Covers acceptance criteria: **AC-2**, **AC-7** (`dotnet test` on all three runtimes),
> **AC-9** (no regressions).

---

## 3 — Examples project: target framework

**File:** `HdrHistogram.Examples/HdrHistogram.Examples.csproj`

- [x] **3.1** Change `<TargetFrameworks>` on line 5 from `net8.0` to `net10.0`.
  Verify: the element reads exactly `<TargetFrameworks>net10.0</TargetFrameworks>`.

> Covers acceptance criterion: **AC-3**.

---

## 4 — Benchmarking project: target frameworks and BenchmarkDotNet compatibility

**File:** `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`

- [x] **4.1** Change `<TargetFrameworks>` on line 5 from `net8.0` to
  `net10.0;net9.0;net8.0`.
  Verify: the element reads exactly `<TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>`.

- [x] **4.2** Attempt a Release build of the benchmarking project:
  ```bash
  dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release
  ```
  If the build succeeds, this task is done — BenchmarkDotNet 0.13.12 is compatible.
  If the build fails due to BenchmarkDotNet incompatibility with net10.0, proceed to task 4.3.

- [x] **4.3** *(Conditional — only if task 4.2 fails)* Upgrade both
  `BenchmarkDotNet` and `BenchmarkDotNet.Diagnostics.Windows` to the latest
  stable version.
  Verify: `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release`
  succeeds after the upgrade.
  Note: if this task is executed, also complete task 10.6 in the spec section below.

> Covers acceptance criteria: **AC-4**, **AC-6** (`dotnet build` succeeds for all TFMs).

---

## 5 — CI pipeline: multi-version SDK install

**File:** `.github/workflows/ci.yml`

- [x] **5.1** Replace the `setup-dotnet` step (lines 15–19) with a single step using
  a multi-line `dotnet-version` scalar:
  ```yaml
  - uses: actions/setup-dotnet@v4
    with:
      dotnet-version: |
        8.0.x
        9.0.x
        10.0.x
      cache: true
      cache-dependency-path: '**/*.csproj'
  ```
  Do **not** use separate `setup-dotnet` steps — that would break `cache: true`.
  Verify: the file contains exactly one `setup-dotnet` step, and `dotnet-version`
  is a multi-line block scalar listing all three versions.

> Covers acceptance criterion: **AC-5**.

---

## 6 — Devcontainer: upgrade base image and install additional runtimes

**File:** `.devcontainer/Dockerfile`

- [x] **6.1** Change the `FROM` line (line 5) from
  `mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim` to
  `mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim`.
  Verify: the first `FROM` line references `sdk:10.0-bookworm-slim`.

- [x] **6.2** Replace the single 8.0 runtime install (lines 6–8) with explicit
  installs of both the 8.0 and 9.0 runtimes via `dotnet-install.sh`, keeping both
  in the same `RUN` layer to avoid creating extra image layers:
  ```dockerfile
  RUN dotnet_version=8.0 \
      && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
         --runtime dotnet --channel $dotnet_version --install-dir /usr/share/dotnet \
      && dotnet_version=9.0 \
      && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
         --runtime dotnet --channel $dotnet_version --install-dir /usr/share/dotnet
  ```
  Verify: the file installs both 8.0 and 9.0 runtimes, and the base image is 10.0.

> Covers acceptance criterion: **AC-6** (devcontainer uses `sdk:10.0-bookworm-slim`
> with 8.0 and 9.0 runtimes installed).

---

## 7 — Spec: main library TargetFrameworks

**File:** `spec/tech-standards/build-system.md`, approx. lines 24–26

- [x] **7.1** In the "Main Library (HdrHistogram.csproj)" section, update the XML
  code block from:
  ```xml
  <TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>
  ```
  to:
  ```xml
  <TargetFrameworks>net10.0;net9.0;net8.0;netstandard2.0</TargetFrameworks>
  ```
  Verify: the code block in that section reflects all four targets in the correct order.

> Covers acceptance criterion: **AC-10** (spec updated at specified locations).

---

## 8 — Spec: test project TargetFramework

**File:** `spec/tech-standards/build-system.md`, approx. lines 35–37

- [x] **8.1** In the "Test Project" section, update the XML code block from:
  ```xml
  <TargetFramework>net8.0</TargetFramework>
  ```
  to:
  ```xml
  <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
  ```
  Note: the element name changes from the singular `TargetFramework` to the plural
  `TargetFrameworks` to match a multi-target declaration.
  Verify: the code block in that section shows all three targets.

> Covers acceptance criterion: **AC-10**.

---

## 9 — Spec: benchmarking project TargetFrameworks

**File:** `spec/tech-standards/build-system.md`, approx. lines 42–45

- [x] **9.1** In the "Benchmarking Project" section, update the description text
  "Targets the current LTS runtime only (developer tool, not a shipped library):" to
  reflect multi-targeting, and update the XML code block from:
  ```xml
  <TargetFrameworks>net8.0</TargetFrameworks>
  ```
  to:
  ```xml
  <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
  ```
  Verify: both the description and the code block are updated.

> Covers acceptance criterion: **AC-10**.

---

## 10 — Spec: benchmark configuration runtime list

**File:** `spec/tech-standards/build-system.md`, approx. lines 226–228

- [x] **10.1** In the "Benchmark Configuration" section, replace the single target entry:
  ```
  - `net8.0` (current LTS runtime)
  ```
  with a list of all three supported runtimes:
  ```
  - `net10.0` (current LTS runtime)
  - `net9.0` (STS runtime)
  - `net8.0` (LTS runtime)
  ```
  Verify: the bullet list covers all three TFMs.

- [x] **10.2** *(Conditional — only if task 4.3 was executed)* Update the BenchmarkDotNet
  version numbers in the "Benchmarking Project" dependencies code block
  (approx. lines 66–67) to match the upgraded version.
  Verify: the version string in the spec matches the version in
  `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`.

> Covers acceptance criterion: **AC-10**.

---

## 11 — Spec: prerequisites

**File:** `spec/tech-standards/build-system.md`, approx. line 254

- [x] **11.1** In the "Prerequisites" section, change:
  ```
  - .NET 8.0 SDK (or later)
  ```
  to:
  ```
  - .NET 10.0 SDK (or later)
  ```
  Verify: the prerequisites list references .NET 10.0 SDK.

> Covers acceptance criterion: **AC-10**.

---

## 12 — Verification: build, test, and pack

These tasks confirm all acceptance criteria related to running the toolchain.
They must be performed **after** all implementation tasks above are complete.

- [x] **12.1** Run a Release build of the main library and confirm it succeeds for
  all four target frameworks:
  ```bash
  dotnet build HdrHistogram/HdrHistogram.csproj -c Release
  ```
  Verify: build output shows `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`
  all built without errors.

- [x] **12.2** Run the full test suite and confirm all tests pass on all three
  modern runtimes:
  ```bash
  dotnet test HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release
  ```
  Verify: test results show three separate `net10.0`, `net9.0`, and `net8.0` runs,
  all green with zero failures.

- [x] **12.3** Pack the library and confirm all four TFM folders are present in
  the produced `.nupkg`:
  ```bash
  dotnet pack HdrHistogram/HdrHistogram.csproj -c Release --no-build
  ```
  Then inspect the package:
  ```bash
  unzip -l HdrHistogram/bin/Release/HdrHistogram.*.nupkg | grep "^.*lib/"
  ```
  Verify: the archive contains `lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/`,
  and `lib/netstandard2.0/` folders.

> Covers acceptance criteria: **AC-6** (build succeeds), **AC-7** (tests pass on
> all runtimes), **AC-8** (pack contains all four targets), **AC-9** (no regressions).

---

---

## 13 — Code review fixes

Issues found during post-implementation review and resolved.

- [x] **13.1** BenchmarkDotNet version regression: the branch base had `0.13.12` but
  `upstream/main` already upgraded to `0.15.8`.
  Updated `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` to `0.15.8`.
  Verified: `dotnet build -c Release -p:TargetFrameworks=net9.0` succeeds.

- [x] **13.2** `spec/tech-standards/build-system.md` BenchmarkDotNet version block
  also reflected the old `0.13.12`.
  Updated both package references in the spec to `0.15.8` to match the csproj.

- [x] **13.3** `spec/tech-standards/build-system.md` TFM description table in the
  "Main Library" section was missing rows for `net9.0` and `net10.0`.
  Added `net10.0` (current LTS target), `net9.0` (STS target), and `net8.0` (LTS target) rows.

---

## Acceptance Criterion Cross-Reference

| Criterion | Tasks |
|-----------|-------|
| AC-1: `HdrHistogram.csproj` targets `net10.0;net9.0;net8.0;netstandard2.0` | 1.1 |
| AC-2: `HdrHistogram.UnitTests.csproj` targets `net10.0;net9.0;net8.0` | 2.1 |
| AC-3: `HdrHistogram.Examples.csproj` targets `net10.0` | 3.1 |
| AC-4: `HdrHistogram.Benchmarking.csproj` targets `net10.0;net9.0;net8.0` | 4.1 |
| AC-5: CI installs SDKs 8.0.x, 9.0.x, 10.0.x via single `setup-dotnet` step | 5.1 |
| AC-6: `.devcontainer/Dockerfile` uses `sdk:10.0-bookworm-slim` + 8.0 and 9.0 runtimes | 6.1, 6.2 |
| AC-6b: `dotnet build -c Release` succeeds for all TFMs | 4.2, 12.1 |
| AC-7: `dotnet test` passes on net8.0, net9.0, net10.0 | 12.2 |
| AC-8: `dotnet pack` produces `.nupkg` with all four target folders | 1.2, 12.3 |
| AC-9: No regressions — all existing tests pass on all targets | 12.2 |
| AC-10: `spec/tech-standards/build-system.md` updated at all five locations | 7.1, 8.1, 9.1, 10.1, 11.1 |
