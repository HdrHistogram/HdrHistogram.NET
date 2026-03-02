# Contributing

Thank you for your interest in contributing to HdrHistogram.NET.
We welcome pull requests and encourage contributors to engage with the project.

## Prerequisites

- .NET 8 SDK minimum (newer SDKs are also supported)
- Supported platforms: Windows, macOS, Linux

## Building

```sh
dotnet build HdrHistogram.sln
```

## Testing

```sh
dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release
```

## Code Style

- Code style is governed by `.editorconfig`
- Run `dotnet format` before submitting a PR
- Use the .NET 8 SDK to match the CI environment

## Git Workflow

- Branch naming prefixes: `feat/`, `fix/`, `chore/`
- One issue per PR
- PRs target `main`
- No direct commits to `main`

## Pull Request Guidelines

- Please first raise an issue before contributing so the team is not caught off guard by the pull request.
- Ensure the PR has a comment describing what it achieves and which issues it closes.
- If fixing an issue or bug, include a unit test proving the fix and reference the issue in the PR comments.

## Line Endings

- LF is the project convention.
- `.gitattributes` normalises line endings automatically on checkout.
- Windows batch files (`.cmd`, `.bat`) use CRLF by exception.
- Keep `core.autocrlf=false` or rely on `.gitattributes` to handle line endings correctly.

## Cross-Platform Notes

- Shell scripts require LF line endings.
- `.gitattributes` normalises line endings on checkout, so no manual intervention is needed.
- All platforms (Windows, macOS, Linux) with the .NET 8 SDK can build and test natively with no container required.
- `.devcontainer/` contains a VS Code / GitHub Codespaces devcontainer for human contributors.
- `autonomous/` contains the agent Docker infrastructure (`Dockerfile`, `agent-loop.sh`, etc.) used for autonomous operation.
- Host-side entry points for running agents are `scripts/run.sh` (single agent) and `scripts/fleet.sh` (multi-agent fleet).
