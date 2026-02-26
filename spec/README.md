# Specifications Index

This file provides guidance on where to find standards and specifications for this project.

## Claude Documentation

- Claude best practices and prompting tips: https://code.claude.com/docs/en/best-practices
- Agents: https://code.claude.com/docs/en/sub-agents
- Agent teams: https://code.claude.com/docs/en/agent-teams
- Skills and slash commands: https://code.claude.com/docs/en/skills
- Hooks: https://code.claude.com/docs/en/hooks-guide
- MCP: https://code.claude.com/docs/en/mcp

## Tech Standards

### Coding and Development

- **[Coding Standards](./tech-standards/coding-standards.md)** - C# naming conventions, code style, XML documentation, design patterns, performance guidelines
- **[Testing Standards](./tech-standards/testing-standards.md)** - Test framework stack (xUnit, FluentAssertions), test patterns, naming conventions, coverage guidelines
- **[Build System](./tech-standards/build-system.md)** - Project format, target frameworks, CI/CD (AppVeyor), versioning, NuGet packaging

### Architecture and API

- **[Architecture](./tech-standards/architecture.md)** - System mechanics, class hierarchy (HistogramBase, LongHistogram, etc.), Recorder pattern, thread safety, memory model
- **[API Reference](./tech-standards/api-reference.md)** - Public API surface, HistogramFactory, IRecorder interface, extension methods, persistence APIs, usage patterns
- **[Histogram Encoding](./tech-standards/histogram-encoding.md)** - Binary encoding format (V2), LEB128 count encoding, V2 log format specification, interoperability

### Tooling

- **[GitHub CLI Reference](./tech-standards/github.md)** - Milestones, issues, pull requests, `gh api` commands, quick reference

## Domain-Specific Keyword Index

| Keyword | Document |
|---------|----------|
| histogram, bucket, count, percentile | [Architecture](./tech-standards/architecture.md) |
| LongHistogram, IntHistogram, ShortHistogram | [Architecture](./tech-standards/architecture.md), [API Reference](./tech-standards/api-reference.md) |
| Recorder, thread-safe, concurrent | [Architecture](./tech-standards/architecture.md) |
| RecordValue, GetValueAtPercentile | [API Reference](./tech-standards/api-reference.md) |
| HistogramFactory, fluent API | [API Reference](./tech-standards/api-reference.md) |
| encoding, LEB128, DEFLATE, compression | [Histogram Encoding](./tech-standards/histogram-encoding.md) |
| log format, V2, persistence | [Histogram Encoding](./tech-standards/histogram-encoding.md) |
| xUnit, test, FluentAssertions | [Testing Standards](./tech-standards/testing-standards.md) |
| naming convention, XML docs, style | [Coding Standards](./tech-standards/coding-standards.md) |
| build, NuGet, AppVeyor, CI/CD | [Build System](./tech-standards/build-system.md) |
| milestone, issue, PR, GitHub | [GitHub CLI Reference](./tech-standards/github.md) |

## External Documentation

- **HdrHistogram Wiki**: https://github.com/HdrHistogram/HdrHistogram.NET/wiki
- **Java HdrHistogram**: https://github.com/HdrHistogram/HdrHistogram
