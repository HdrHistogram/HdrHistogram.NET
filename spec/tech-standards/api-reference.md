# API Reference

> Public API surface documentation for HdrHistogram.NET.

## Quick Start

### Creating a Histogram

```csharp
// Using factory (recommended)
var histogram = HistogramFactory.With64BitBucketSize()
    .WithValuesFrom(1)
    .WithValuesUpTo(TimeStamp.Hours(1))
    .WithPrecisionOf(3)
    .Create();

// Direct construction
var histogram = new LongHistogram(
    lowestTrackableValue: 1,
    highestTrackableValue: 3600000000000L,  // 1 hour in nanoseconds
    numberOfSignificantValueDigits: 3);
```

### Recording Values

```csharp
histogram.RecordValue(latency);
histogram.RecordValueWithCount(latency, 10);  // Record 10 occurrences
```

### Reading Percentiles

```csharp
var median = histogram.GetValueAtPercentile(50);
var p99 = histogram.GetValueAtPercentile(99);
var p999 = histogram.GetValueAtPercentile(99.9);
```

## Histogram Types

### Non-Thread-Safe

| Class | Count Type | Max Count Per Bucket |
|-------|------------|---------------------|
| `LongHistogram` | `long` | 9,223,372,036,854,775,807 |
| `IntHistogram` | `int` | 2,147,483,647 |
| `ShortHistogram` | `short` | 32,767 |

### Thread-Safe

| Class | Count Type | Description |
|-------|------------|-------------|
| `LongConcurrentHistogram` | `long` | Atomic 64-bit operations |
| `IntConcurrentHistogram` | `int` | Atomic 32-bit operations |

### Recorder

For concurrent recording with periodic snapshots:

```csharp
var recorder = HistogramFactory.With64BitBucketSize()
    .WithThreadSafeReads()
    .Create();

// Record from multiple threads
recorder.RecordValue(latency);

// Periodically get snapshot (single thread)
var snapshot = recorder.GetIntervalHistogram();
```

## HistogramFactory

### Factory Methods

```csharp
HistogramFactory.With64BitBucketSize()  // LongHistogram
HistogramFactory.With32BitBucketSize()  // IntHistogram
HistogramFactory.With16BitBucketSize()  // ShortHistogram
```

### Builder Methods

```csharp
.WithValuesFrom(long lowestTrackableValue)    // Default: 1
.WithValuesUpTo(long highestTrackableValue)   // Required
.WithPrecisionOf(int significantDigits)       // 1-5, default: 3
.WithThreadSafeWrites()                       // Use concurrent histogram
.WithThreadSafeReads()                        // Use Recorder pattern
.Create()                                     // Build histogram/recorder
```

## IRecorder Interface

```csharp
public interface IRecorder
{
    void RecordValue(long value);
    void RecordValueWithCount(long value, long count);
    void RecordValueWithExpectedInterval(long value, long expectedIntervalBetweenValueSamples);
}
```

## HistogramBase Properties

| Property | Type | Description |
|----------|------|-------------|
| `LowestTrackableValue` | `long` | Minimum recordable value |
| `HighestTrackableValue` | `long` | Maximum recordable value |
| `NumberOfSignificantValueDigits` | `int` | Precision (1-5) |
| `TotalCount` | `long` | Total recorded values |
| `StartTimeStamp` | `long` | Recording start time |
| `EndTimeStamp` | `long` | Recording end time |
| `Tag` | `string` | Optional identifier |
| `BucketCount` | `int` | Number of buckets |
| `SubBucketCount` | `int` | Sub-buckets per bucket |

## HistogramBase Methods

### Recording

```csharp
void RecordValue(long value)
void RecordValueWithCount(long value, long count)
void RecordValueWithExpectedInterval(long value, long expectedIntervalBetweenValueSamples)
```

### Percentiles and Statistics

```csharp
long GetValueAtPercentile(double percentile)
double GetPercentileAtOrBelowValue(long value)
long GetCountAtValue(long value)
long GetCountBetweenValues(long lowValue, long highValue)
```

### Histogram Operations

```csharp
void Add(HistogramBase other)           // Merge histograms
HistogramBase Copy()                     // Create a copy
void Reset()                             // Clear all counts
bool HasOverflowed()                     // Check for overflow
bool Equals(HistogramBase other)         // Value equality
```

### Iteration

```csharp
IEnumerable<HistogramIterationValue> RecordedValues()
IEnumerable<HistogramIterationValue> AllValues()
```

## Extension Methods (HistogramExtensions)

### Statistics

```csharp
double GetMean(this HistogramBase histogram)
double GetStdDeviation(this HistogramBase histogram)
long GetMaxValue(this HistogramBase histogram)
```

### Recording Helpers

```csharp
// Time an action
recorder.Record(() => DoSomething());

// Using block pattern
using (recorder.RecordScope())
{
    DoSomething();
}
```

### Output

```csharp
void OutputPercentileDistribution(
    this HistogramBase histogram,
    TextWriter writer,
    int percentileTicksPerHalfDistance = 5,
    double outputValueUnitScalingRatio = 1.0)
```

### Percentile Iteration

```csharp
IEnumerable<HistogramIterationValue> Percentiles(
    this HistogramBase histogram,
    int percentileTicksPerHalfDistance)
```

## HistogramIterationValue

Properties returned during iteration:

| Property | Type | Description |
|----------|------|-------------|
| `ValueIteratedTo` | `long` | Upper bound of current bucket |
| `ValueIteratedFrom` | `long` | Lower bound of current bucket |
| `CountAtValueIteratedTo` | `long` | Count in this bucket |
| `CountAddedInThisIterationStep` | `long` | Count added this step |
| `TotalCountToThisValue` | `long` | Cumulative count |
| `TotalValueToThisValue` | `long` | Cumulative value sum |
| `Percentile` | `double` | Percentile at this value |
| `PercentileLevelIteratedTo` | `double` | Target percentile |

## TimeStamp Utilities

Time unit helpers (values in nanoseconds):

```csharp
TimeStamp.Hours(1)          // 3,600,000,000,000
TimeStamp.Minutes(5)        // 300,000,000,000
TimeStamp.Seconds(30)       // 30,000,000,000
TimeStamp.Milliseconds(100) // 100,000,000
TimeStamp.Microseconds(50)  // 50,000
```

## OutputScalingFactor

Predefined scaling constants:

```csharp
OutputScalingFactor.None                    // 1.0
OutputScalingFactor.TimeStampToSeconds      // 1e9
OutputScalingFactor.TimeStampToMilliseconds // 1e6
OutputScalingFactor.TimeStampToMicroseconds // 1e3
```

## Persistence

### HistogramLogWriter

```csharp
using var stream = File.Create("latency.hlog");
using var writer = new HistogramLogWriter(stream);

writer.WriteStartTime(DateTime.UtcNow);
writer.WriteBaseTime(DateTime.UtcNow);
writer.Append(histogram);
```

### HistogramLogReader

```csharp
using var stream = File.OpenRead("latency.hlog");
using var reader = new HistogramLogReader(stream);

HistogramBase histogram;
while ((histogram = reader.ReadNextIntervalHistogram()) != null)
{
    // Process histogram
}

// Read with time range filter
while ((histogram = reader.ReadNextIntervalHistogram(
    startTime, endTime)) != null)
{
    // Process filtered histograms
}
```

### Static Methods

```csharp
// Quick write
HistogramLogWriter.Write(stream, startTime, histogram1, histogram2);

// Binary encoding
var buffer = ByteBuffer.Allocate(histogram.GetNeededByteBufferCapacity());
histogram.EncodeIntoCompressedByteBuffer(buffer);

// Decode
var decoded = HistogramEncoding.DecodeFromCompressedByteBuffer(buffer, 0);
```

## Output Formatters

### HGRM Output

```csharp
using var writer = new StreamWriter("output.hgrm");
histogram.OutputPercentileDistribution(
    writer,
    outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds);
```

### CSV Output

```csharp
// Using CsvOutputFormatter directly
var formatter = new CsvOutputFormatter(writer, scalingFactor);
formatter.WriteHeader();
foreach (var value in histogram.Percentiles(5))
{
    formatter.WriteRecord(value);
}
formatter.WriteFooter();
```

## Exception Handling

| Exception | Cause |
|-----------|-------|
| `ArgumentException` | Invalid constructor parameters |
| `IndexOutOfRangeException` | Recording value exceeds `HighestTrackableValue` |
| `OverflowException` | Count exceeds type capacity |

### Overflow Detection

```csharp
if (histogram.HasOverflowed())
{
    // Total count has exceeded long.MaxValue
    // or count type capacity exceeded
}
```

## Common Usage Patterns

### Latency Recording

```csharp
var recorder = HistogramFactory.With64BitBucketSize()
    .WithValuesUpTo(TimeStamp.Seconds(10))
    .WithPrecisionOf(3)
    .WithThreadSafeReads()
    .Create();

// In hot path
var start = Stopwatch.GetTimestamp();
DoWork();
var elapsed = Stopwatch.GetTimestamp() - start;
recorder.RecordValue(elapsed * 1000000000 / Stopwatch.Frequency);  // Convert to nanos

// Periodic reporting
var snapshot = recorder.GetIntervalHistogram();
Console.WriteLine($"P99: {snapshot.GetValueAtPercentile(99) / 1e6} ms");
```

### Aggregating Histograms

```csharp
var aggregate = new LongHistogram(highestValue, 3);
aggregate.Add(histogram1);
aggregate.Add(histogram2);
aggregate.Add(histogram3);
```

### Coordinated Omission Correction

```csharp
// When recording values that may have coordination issues
histogram.RecordValueWithExpectedInterval(
    actualLatency,
    expectedIntervalBetweenSamples);
```
