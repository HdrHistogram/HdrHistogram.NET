# Testing Standards

> Testing approach, frameworks, and patterns for HdrHistogram.NET.

## Test Framework Stack

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.9.0 | Primary test framework |
| FluentAssertions | 6.12.0 | Assertion library |
| Xunit.Combinatorial | 1.6.24 | Combinatorial test generation |
| Microsoft.NET.Test.Sdk | 17.0.2 | Test host |
| xunit.runner.visualstudio | 2.2.0 | Visual Studio integration |

## Project Structure

```
HdrHistogram.UnitTests/
├── HistogramTestBase.cs              # Abstract base for histogram tests
├── LongHistogramTests.cs             # LongHistogram tests
├── IntHistogramTests.cs              # IntHistogram tests
├── ShortHistogramTests.cs            # ShortHistogram tests
├── LongConcurrentHistogramTests.cs   # Thread-safe long tests
├── IntConcurrentHistogramTests.cs    # Thread-safe int tests
├── Persistence/                      # Persistence tests
│   ├── HistogramLogReaderWriterTestBase.cs
│   └── HistogramEncodingTestBase.cs
├── Recording/                        # Recorder tests
│   └── RecorderTestsBase.cs
└── Resources/                        # Test resources
    ├── *.hgrm                        # Sample histogram files
    ├── *.hlog                        # Sample log files
    └── *.csv                         # Comparison files
```

## Test Hierarchy Pattern

Abstract base test classes define common test behavior:

```csharp
public abstract class HistogramTestBase
{
    // Default test parameters
    protected const int DefaultHighestTrackableValue = 3600 * 1000 * 1000;
    protected const int DefaultSignificantFigures = 3;

    // Factory method - subclasses override
    protected abstract HistogramBase Create(
        long highestTrackableValue,
        int numberOfSignificantValueDigits);

    // Common tests
    [Fact]
    public void TotalCount_is_zero_after_construction()
    {
        var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
        Assert.Equal(0, histogram.TotalCount);
    }
}

// Implementation-specific test class
public class LongHistogramTests : HistogramTestBase
{
    protected override HistogramBase Create(
        long highestTrackableValue,
        int numberOfSignificantValueDigits)
    {
        return new LongHistogram(highestTrackableValue, numberOfSignificantValueDigits);
    }
}
```

## Test Patterns

### Theory-Driven Tests

Use `[Theory]` with `[InlineData]` for parameterized tests:

```csharp
[Theory]
[InlineData(5)]
[InlineData(100)]
[InlineData(1000)]
public void RecordValueWithCount_increments_TotalCount(long multiplier)
{
    var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
    for (int i = 1; i < 5; i++)
    {
        histogram.RecordValueWithCount(i, multiplier);
        Assert.Equal(i * multiplier, histogram.TotalCount);
    }
}
```

### Combinatorial Tests

Use `[CombinatorialData]` for exhaustive parameter combinations:

```csharp
[Theory]
[CombinatorialData]
public void CanEncode_and_Decode(
    [CombinatorialValues(1, 1000, 3600000000)] long highestTrackable,
    [CombinatorialValues(1, 2, 3)] int significantDigits)
{
    // Test all combinations
}
```

### Fact Tests

Use `[Fact]` for single-case tests:

```csharp
[Fact]
public void Constructor_throws_if_highestTrackableValue_less_than_one()
{
    Assert.Throws<ArgumentException>(() =>
        Create(0, DefaultSignificantFigures));
}
```

## Assertion Style

Use FluentAssertions for readable assertions:

```csharp
// Instead of:
Assert.Equal(expected, actual);

// Prefer:
actual.Should().Be(expected);

// Complex assertions:
histogram.TotalCount.Should().Be(1000);
histogram.GetValueAtPercentile(50).Should().BeInRange(499, 501);
percentiles.Should().HaveCount(expectedCount);
```

## Test Naming Convention

Use descriptive names that explain the scenario:

```
{Method}_returns_{expected}_when_{condition}
{Method}_throws_{exception}_if_{condition}
{Property}_is_{value}_after_{action}
```

Examples:
- `RecordValue_increments_TotalCount`
- `Constructor_throws_ArgumentException_if_precision_exceeds_5`
- `TotalCount_is_zero_after_construction`
- `GetValueAtPercentile_returns_correct_value_for_median`

## Resource Files

Embed test resources for comparison testing:

```csharp
// Access embedded resources
var assembly = typeof(HistogramLogReaderWriterTests).Assembly;
var stream = assembly.GetManifestResourceStream(
    "HdrHistogram.UnitTests.Resources.sample.hlog");
```

Resource types:
- `.hgrm` - Histogram output format files
- `.hlog` - Histogram log files
- `.csv` - Expected output comparison files

## Performance Testing

Run tests in Release mode for accurate performance measurements:

```bash
dotnet test --configuration Release
```

Separate benchmarking project (`HdrHistogram.Benchmarking`) uses BenchmarkDotNet for micro-benchmarks.

## Test Organization Guidelines

1. **One test file per class** - `LongHistogramTests.cs` tests `LongHistogram`
2. **Group related tests** - Use regions or nested classes for logical grouping
3. **Shared setup** - Use constructor or `IClassFixture<T>` for test setup
4. **Test isolation** - Each test should be independent and repeatable
5. **No test interdependencies** - Tests should run in any order

## Coverage Guidelines

### Required Coverage

- All public API methods
- Constructor parameter validation
- Edge cases (zero, max values, overflow)
- Error conditions and exceptions
- Thread-safety guarantees (concurrent tests)

### Test Categories

| Category | Description |
|----------|-------------|
| Construction | Constructor behavior and validation |
| Recording | Value recording and counting |
| Percentiles | Percentile calculations |
| Iteration | Iterator behavior |
| Encoding | Serialization/deserialization |
| Persistence | Log file reading/writing |
| Concurrency | Thread-safety verification |

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbosity
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~LongHistogramTests"

# Run tests in Release mode (recommended for timing-sensitive tests)
dotnet test --configuration Release
```
