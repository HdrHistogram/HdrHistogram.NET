# Manual Intervention: Apply CI Workflow Change

This document provides instructions for a maintainer to apply the proposed CI workflow change that adds a `dotnet format` check step.

## Background

The local dry-run `dotnet format --verify-no-changes --verbosity diagnostic` was confirmed to exit with code 0, satisfying AC4.

The proposed change adds the step with `--verbosity diagnostic` so that failures clearly show which files need formatting, satisfying AC5.

The step inherits the existing `pull_request` trigger, satisfying AC3.

## Steps

1. Copy `plan/ready/ci.yml.proposed` to `.github/workflows/ci.yml`, or apply the diff manually.

2. Verify the step order in `.github/workflows/ci.yml` is: `checkout → setup-dotnet → restore → **format check** → build → test → pack → upload`.

3. Commit and push on the feature branch.

4. Confirm the "Check code formatting" step appears in the GitHub Actions run log.
