# Architecture

> System mechanics, class hierarchy, and key components of HdrHistogram.NET.

## Overview

HdrHistogram.NET is a .NET port of the Java [HdrHistogram](https://github.com/HdrHistogram/HdrHistogram) library. It provides low-latency, high-precision recording of value distributions with configurable precision and value range.

## Core Concepts

### What is HdrHistogram?

HdrHistogram (High Dynamic Range Histogram) records integer values within a configurable range with configurable precision. Key characteristics:

- **Fixed memory footprint** - Memory is allocated at construction, never during recording
- **O(1) recording** - Constant time value recording regardless of data size
- **Configurable precision** - 1-5 significant digits of precision
- **Configurable range** - From `lowestTrackableValue` to `highestTrackableValue`
- **Percentile queries** - Efficient percentile and distribution queries

### Value Recording Model

Values are recorded into "buckets" determined by:
1. **Significant digits** - Precision (1-5 digits, typically 3)
2. **Value range** - `lowestTrackableValue` to `highestTrackableValue`

The histogram uses a logarithmic bucketing scheme that provides constant relative precision across the entire value range.

## Class Hierarchy

```
HistogramBase (abstract)
├── LongHistogram          # 64-bit (long) count storage
├── IntHistogram           # 32-bit (int) count storage
├── ShortHistogram         # 16-bit (short) count storage
├── LongConcurrentHistogram # Thread-safe long histogram
└── IntConcurrentHistogram  # Thread-safe int histogram
```

### HistogramBase

Location: `HdrHistogram/HistogramBase.cs`

Abstract base class containing:
- Bucket calculation algorithms
- Value recording logic
- Percentile computation
- Serialization support

Key properties:
| Property | Description |
|----------|-------------|
| `LowestTrackableValue` | Minimum recordable value (>= 1) |
| `HighestTrackableValue` | Maximum recordable value |
| `NumberOfSignificantValueDigits` | Precision (1-5) |
| `TotalCount` | Sum of all recorded values |
| `BucketCount` | Number of buckets |
| `SubBucketCount` | Sub-buckets per bucket |
| `Tag` | Optional string tag |

Key methods:
| Method | Description |
|--------|-------------|
| `RecordValue(long)` | Record a single value |
| `RecordValueWithCount(long, long)` | Record value with count |
| `RecordValueWithExpectedInterval(long, long)` | Coordinated omission correction |
| `GetValueAtPercentile(double)` | Get value at percentile |
| `Add(HistogramBase)` | Merge histograms |
| `Copy()` | Create a copy |
| `Reset()` | Clear all counts |

### Concrete Implementations

| Class | Word Size | Count Type | Thread-Safe |
|-------|-----------|------------|-------------|
| `LongHistogram` | 8 bytes | `long[]` | No |
| `IntHistogram` | 4 bytes | `int[]` | No |
| `ShortHistogram` | 2 bytes | `short[]` | No |
| `LongConcurrentHistogram` | 8 bytes | `AtomicLongArray` | Yes |
| `IntConcurrentHistogram` | 4 bytes | `AtomicIntArray` | Yes |

Choose based on:
- **Maximum count per bucket** - Short (65K), Int (2B), Long (9 quintillion)
- **Memory constraints** - Smaller word size = less memory
- **Thread safety needs** - Use Concurrent variants for multi-threaded recording

### Recorder

Location: `HdrHistogram/Recorder.cs`

Provides thread-safe interval recording using double buffering:

```
┌──────────────┐     ┌──────────────┐
│   Active     │ <-> │   Inactive   │
│  Histogram   │     │  Histogram   │
└──────────────┘     └──────────────┘
        │                    │
   Recording            GetIntervalHistogram()
   happens here         returns this, then swaps
```

The `WriterReaderPhaser` ensures atomic swaps without locking during recording.

## Factory Pattern

Location: `HdrHistogram/Histogram.cs` (HistogramFactory class)

Fluent API for histogram creation:

```csharp
var histogram = HistogramFactory.With64BitBucketSize()
    .WithValuesFrom(1)                    // Lowest trackable value
    .WithValuesUpTo(TimeStamp.Hours(1))   // Highest trackable value
    .WithPrecisionOf(3)                   // Significant digits
    .Create();

// Thread-safe recorder
var recorder = HistogramFactory.With64BitBucketSize()
    .WithThreadSafeReads()
    .Create();  // Returns Recorder, not histogram
```

Factory methods:
- `With64BitBucketSize()` - Creates LongHistogram
- `With32BitBucketSize()` - Creates IntHistogram
- `With16BitBucketSize()` - Creates ShortHistogram

Builder methods:
- `WithValuesFrom(long)` - Set lowest trackable value
- `WithValuesUpTo(long)` - Set highest trackable value
- `WithPrecisionOf(int)` - Set significant digits (1-5)
- `WithThreadSafeWrites()` - Use concurrent histogram
- `WithThreadSafeReads()` - Use Recorder pattern

## Iteration

Location: `HdrHistogram/Iteration/`

Iterators provide different views of histogram data:

| Iterator | Purpose |
|----------|---------|
| `RecordedValuesEnumerator` | Only buckets with recorded values |
| `AllValuesEnumerator` | All buckets including zeros |
| `PercentileEnumerator` | Percentile-based iteration |
| `LinearEnumerator` | Linear value steps |
| `LogarithmicEnumerator` | Logarithmic value steps |

Usage:
```csharp
// Iterate recorded values
foreach (var value in histogram.RecordedValues())
{
    Console.WriteLine($"Value: {value.ValueIteratedTo}, Count: {value.CountAtValueIteratedTo}");
}

// Iterate percentiles
foreach (var value in histogram.Percentiles(5))
{
    Console.WriteLine($"P{value.Percentile:F2}: {value.ValueIteratedTo}");
}
```

## Persistence

Location: `HdrHistogram/Persistence/`

### Log Format

The V2 log format stores multiple histograms with timestamps:

```
#[StartTime: 1234567890.000]
#[BaseTime: 1234567890.000]
"StartTimestamp","Interval_Length","Interval_Max","Histogram"
0.001,1.000,999.0,HISTXXXX...
1.001,1.000,999.0,HISTXXXX...
```

Classes:
- `HistogramLogWriter` - Writes histograms to log files
- `HistogramLogReader` - Reads histograms from log files

### Binary Encoding

The encoded histogram format uses:
- DEFLATE compression (Zlib RFC-1950)
- Little Endian Base128 (LEB128) for counts
- ZigZag encoding for efficiency

See `histogram-encoding.md` for detailed format specification.

## Output Formatting

Location: `HdrHistogram/Output/`

Formatters implement `IOutputFormatter`:

| Class | Format | Description |
|-------|--------|-------------|
| `HgrmOutputFormatter` | HGRM | Standard histogram text format |
| `CsvOutputFormatter` | CSV | Comma-separated values |

Usage:
```csharp
// Output percentile distribution
histogram.OutputPercentileDistribution(
    writer: Console.Out,
    outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds);
```

## Utilities

### TimeStamp

Location: `HdrHistogram/TimeStamp.cs`

Helper methods for time unit conversion:

```csharp
TimeStamp.Hours(1)         // 3,600,000,000,000 (nanoseconds)
TimeStamp.Minutes(5)       // 300,000,000,000
TimeStamp.Seconds(30)      // 30,000,000,000
TimeStamp.Milliseconds(100)// 100,000,000
TimeStamp.Microseconds(50) // 50,000
```

### OutputScalingFactor

Predefined scaling constants for output:

```csharp
OutputScalingFactor.None                  // 1.0
OutputScalingFactor.TimeStampToSeconds    // 1e9
OutputScalingFactor.TimeStampToMilliseconds // 1e6
OutputScalingFactor.TimeStampToMicroseconds // 1e3
```

## Extension Methods

Location: `HdrHistogram/HistogramExtensions.cs`

```csharp
// Statistics
histogram.GetMean();
histogram.GetStdDeviation();
histogram.GetMaxValue();

// Recording helpers
recorder.Record(() => DoSomething());  // Time an action
using (recorder.RecordScope()) { ... }  // Using block pattern

// Output
histogram.OutputPercentileDistribution(writer, scalingFactor);
```

## Thread Safety

### Non-Concurrent Histograms

`LongHistogram`, `IntHistogram`, `ShortHistogram` are **not thread-safe**:
- Safe for single-threaded recording
- Safe for read-only access from multiple threads after recording completes

### Concurrent Histograms

`LongConcurrentHistogram`, `IntConcurrentHistogram` provide:
- Thread-safe recording from multiple threads
- Lock-free atomic operations
- Uses `AtomicLongArray` / `AtomicIntArray`

### Recorder Pattern

For concurrent recording with periodic reading:

```csharp
var recorder = HistogramFactory.With64BitBucketSize()
    .WithThreadSafeReads()
    .Create();

// Multiple threads can call:
recorder.RecordValue(latency);

// Periodic reader (single thread):
var snapshot = recorder.GetIntervalHistogram();
// snapshot is a stable copy, recording continues uninterrupted
```

## Memory Model

```
┌─────────────────────────────────────────────────────────────┐
│                      HistogramBase                          │
├─────────────────────────────────────────────────────────────┤
│ Configuration (immutable after construction)                │
│   - LowestTrackableValue                                    │
│   - HighestTrackableValue                                   │
│   - NumberOfSignificantValueDigits                          │
│   - BucketCount, SubBucketCount                             │
├─────────────────────────────────────────────────────────────┤
│ State (mutable)                                             │
│   - TotalCount                                              │
│   - StartTimeStamp, EndTimeStamp                            │
│   - Tag                                                     │
├─────────────────────────────────────────────────────────────┤
│ Counts array (implementation-specific)                      │
│   LongHistogram:   long[countsArrayLength]                  │
│   IntHistogram:    int[countsArrayLength]                   │
│   ShortHistogram:  short[countsArrayLength]                 │
└─────────────────────────────────────────────────────────────┘
```

Memory is allocated once at construction. No allocations occur during `RecordValue()` operations.
