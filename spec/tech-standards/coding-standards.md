# Coding Standards

> C# coding conventions and style guidelines for HdrHistogram.NET.

## Language Version

- **C# Version**: Latest stable (targeting .NET 8.0 and .NET Standard 2.0)
- **Code Style**: Default Resharper/Visual Studio rules
- **Documentation**: XML comments required on all public members

## Naming Conventions

### Types

| Kind | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `LongHistogram`, `HistogramLogWriter` |
| Interfaces | `I` prefix + PascalCase | `IRecorder`, `IEncoder`, `IOutputFormatter` |
| Abstract classes | PascalCase + `Base` suffix | `HistogramBase`, `HistogramTestBase` |
| Enums | PascalCase | `IterationMode` |
| Structs | PascalCase | `HistogramIterationValue` |

### Members

| Kind | Convention | Example |
|------|------------|---------|
| Public methods | PascalCase | `RecordValue()`, `GetValueAtPercentile()` |
| Public properties | PascalCase | `TotalCount`, `HighestTrackableValue` |
| Private fields | `_camelCase` (underscore prefix) | `_counts`, `_totalCount`, `_subBucketMask` |
| Protected members | PascalCase | `Counts`, `BucketCount` |
| Constants | PascalCase | `DefaultHighestTrackableValue` |
| Static readonly | PascalCase | `TimeStampToSeconds` |
| Parameters | camelCase | `lowestTrackableValue`, `highestTrackableValue` |
| Local variables | camelCase | `bucketIndex`, `valueCount` |

## Namespace Organization

```
HdrHistogram                    # Root namespace - core types
├── HdrHistogram.Encoding       # Serialization/encoding formats
├── HdrHistogram.Iteration      # Value iteration mechanisms
├── HdrHistogram.Output         # Output formatters (CSV, HGRM)
├── HdrHistogram.Persistence    # Log reading/writing
└── HdrHistogram.Utilities      # Helper utilities
```

## File Headers

All source files should include the license header:

```csharp
/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */
```

## XML Documentation

All public APIs must have XML documentation comments:

```csharp
/// <summary>
/// Records a value in the histogram.
/// </summary>
/// <param name="value">The value to record.</param>
/// <exception cref="IndexOutOfRangeException">
/// Thrown when <paramref name="value"/> exceeds <see cref="HighestTrackableValue"/>.
/// </exception>
/// <remarks>
/// Values below <see cref="LowestTrackableValue"/> are recorded as
/// <see cref="LowestTrackableValue"/>.
/// </remarks>
public void RecordValue(long value)
```

Required documentation tags:
- `<summary>` - Required on all public members
- `<param>` - Required for all parameters
- `<returns>` - Required for non-void methods
- `<exception>` - Document thrown exceptions
- `<remarks>` - Optional, for additional context
- `<see>` / `<seealso>` - Cross-references to related types

## Design Patterns

### Template Method

`HistogramBase` defines the algorithm structure; subclasses implement storage:

```csharp
public abstract class HistogramBase : IRecorder
{
    // Template methods defining algorithm
    public void RecordValue(long value) { ... }

    // Abstract methods for subclass implementation
    protected abstract long GetCountAtIndex(int index);
    protected abstract void SetCountAtIndex(int index, long value);
}
```

### Factory Pattern (Fluent Builder)

```csharp
var histogram = HistogramFactory.With64BitBucketSize()
    .WithValuesFrom(1)
    .WithValuesUpTo(TimeStamp.Hours(1))
    .WithPrecisionOf(3)
    .Create();
```

### Strategy Pattern

Output formatters implement `IOutputFormatter`:

```csharp
public interface IOutputFormatter
{
    void WriteHeader();
    void WriteRecord(HistogramIterationValue value);
    void WriteFooter();
}

// Implementations: CsvOutputFormatter, HgrmOutputFormatter
```

### Double Buffering

The `Recorder` class uses double buffering for thread-safe snapshots:

```csharp
public class Recorder
{
    private HistogramBase _activeHistogram;
    private HistogramBase _inactiveHistogram;
    private readonly WriterReaderPhaser _phaser;

    public HistogramBase GetIntervalHistogram()
    {
        // Atomically swap active/inactive histograms
    }
}
```

## Performance Guidelines

1. **Minimize allocations** in hot paths (recording values)
2. **Use arrays** for contiguous memory access in count storage
3. **Direct index calculations** - avoid iteration in bucket calculations
4. **Reuse enumerators** where possible (noted in comments)
5. **Atomic operations** for thread-safe variants (`AtomicLongArray`)

## Defensive Programming

```csharp
// Parameter validation with ArgumentException
if (highestTrackableValue < 1)
    throw new ArgumentException(
        "highestTrackableValue must be >= 1",
        nameof(highestTrackableValue));

// Debug assertions for internal invariants
Debug.Assert(bucketIndex >= 0 && bucketIndex < BucketCount);

// Overflow detection
public bool HasOverflowed() => _totalCount < 0;
```

## Resource Management

Implement `IDisposable` correctly:

```csharp
public class HistogramLogWriter : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Cleanup resources
    }
}
```

## Code Organization Principles

1. **Separation of Concerns** - Clear separation between recording, iteration, persistence, and output
2. **Composition over Inheritance** - Extension methods preferred over deep inheritance
3. **Immutability Where Possible** - Configuration values set at construction time
4. **Zero External Dependencies** - Core library has no external package dependencies
5. **Port Fidelity** - Maintains compatibility with Java HdrHistogram design and semantics
