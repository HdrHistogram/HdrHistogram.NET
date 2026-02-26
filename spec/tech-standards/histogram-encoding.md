# Histogram Encoding Format

> Technical specification for histogram binary encoding and log file formats.

## Overview

HdrHistogram uses two related encoding formats:
1. **Encoded Histogram Format** - Binary format for a single histogram
2. **V2 Log Encoding** - Text format for multiple timestamped histograms

## Encoded Histogram Format (Binary)

### Structure

```
┌────────────────────────────────────────┐
│ Cookie (4 bytes)                       │  Identifies format version
├────────────────────────────────────────┤
│ Payload Length (4 bytes)               │  Length of compressed data
├────────────────────────────────────────┤
│ Compressed Payload                     │  DEFLATE-compressed histogram
└────────────────────────────────────────┘
```

### Cookie Value

| Version | Cookie (decimal) | Cookie (hex) | Base64 prefix |
|---------|------------------|--------------|---------------|
| V2 | 478450452 | 0x1c849314 | "HIST" |

The cookie value is chosen so that Base64-encoded histograms begin with "HIST".

### Compression

- **Algorithm**: DEFLATE (Zlib RFC-1950)
- **Input**: Uncompressed header + counts array
- **Output**: Variable-length compressed data

### Uncompressed Header

The 40-byte header contains:

| Field | Type | Bytes | Description |
|-------|------|-------|-------------|
| Cookie | int32 | 4 | Format identifier (478450452) |
| Payload length | int32 | 4 | Length of payload in bytes |
| Normalizing index offset | int32 | 4 | Index normalization |
| Significant value digits | int32 | 4 | Precision (1-5) |
| Lowest trackable value | int64 | 8 | Minimum recordable value |
| Highest trackable value | int64 | 8 | Maximum recordable value |
| Conversion ratio | double | 8 | Output scaling ratio |

Total header: 40 bytes

### Count Encoding (LEB128)

Counts are encoded using **Little Endian Base128** (LEB128) with ZigZag encoding:

```
Standard value encoding (positive):
┌─────────┬─────────┬─────────┬───┐
│ 7 bits  │ 7 bits  │ 7 bits  │...│
│ + cont  │ + cont  │ + cont  │   │
└─────────┴─────────┴─────────┴───┘

Each byte: 7 data bits + 1 continuation bit
```

| Value Range | Bytes Used |
|-------------|------------|
| 0 - 127 | 1 |
| 128 - 16383 | 2 |
| 16384 - 2097151 | 3 |
| ... | ... |
| Max (2^63) | 9 |

**Note**: Maximum 9 bytes per 64-bit value (not 10 as in standard LEB128).

### Zero Run-Length Encoding

Consecutive zero counts are compressed:

```
Standard count: positive integer
Zero run:       negative integer = -(number of consecutive zeros)
```

Example:
```
Actual counts: [5, 0, 0, 0, 3, 0, 2]
Encoded as:    [5, -3, 3, -1, 2]
```

This provides significant space savings for sparse histograms.

### Encoding Process

```
1. Create 40-byte header
2. Encode counts with ZigZag LEB128
3. Apply zero run-length encoding
4. Compress with DEFLATE
5. Prepend cookie and length
6. (Optional) Base64 encode for text transport
```

### Decoding Process

```
1. Read cookie and verify version
2. Read payload length
3. Decompress with INFLATE
4. Read 40-byte header
5. Decode LEB128 counts
6. Expand zero runs
7. Reconstruct histogram
```

## V2 Log Encoding (Text)

### File Structure

```
# Comment lines (optional)
#[StartTime: 1234567890.123]
#[BaseTime: 1234567890.123]
"StartTimestamp","Interval_Length","Interval_Max","Histogram"
0.001,1.000,999.0,HISTxxxx...
1.001,1.000,999.0,HISTxxxx...
```

### Metadata Lines

Lines starting with `#`:

| Prefix | Description |
|--------|-------------|
| `#[StartTime: X.XXX]` | Log start time (seconds since epoch) |
| `#[BaseTime: X.XXX]` | Reference time for relative timestamps |
| `# anything else` | Comment (ignored) |

### Column Header

```
"StartTimestamp","Interval_Length","Interval_Max","Histogram"
```

Human-readable but ignored by parsers.

### Histogram Records

Each line contains comma-separated fields:

```
Tag=mytag,0.001,1.000,999.0,HISTFAAAAxxxxxx...
```

| Field | Type | Description |
|-------|------|-------------|
| Tag (optional) | string | Custom tag (no commas/spaces/newlines) |
| LogTimeStamp | decimal | Seconds offset from BaseTime |
| IntervalLengthSeconds | decimal | Duration of this interval |
| MaxTime | decimal | End time (LogTimeStamp + IntervalLengthSeconds) |
| CompressedPayload | string | Base64-encoded histogram |

### Tag Format

- Optional field at the beginning
- Format: `Tag=value,` (include trailing comma)
- Restrictions: No commas, spaces, or line breaks in value
- If absent, omit entirely (no empty `Tag=,`)

### Example Log File

```
#[StartTime: 1609459200.000]
#[BaseTime: 1609459200.000]
# This is a latency log
"StartTimestamp","Interval_Length","Interval_Max","Histogram"
0.000,1.000,42.0,HISTFAAAACx4nJNpmSz...
1.000,1.000,38.0,HISTFAAAACx4nJNpmSz...
Tag=api,2.000,1.000,55.0,HISTFAAAACx4nJNpmSz...
Tag=db,2.000,1.000,120.0,HISTFAAAACx4nJNpmSz...
```

## API Usage

### Writing Logs

```csharp
using var stream = File.Create("latency.hlog");
using var writer = new HistogramLogWriter(stream);

// Set start time
writer.WriteStartTime(DateTime.UtcNow);

// Write histogram intervals
writer.Append(histogram);
```

### Reading Logs

```csharp
using var stream = File.OpenRead("latency.hlog");
using var reader = new HistogramLogReader(stream);

HistogramBase histogram;
while ((histogram = reader.ReadNextIntervalHistogram()) != null)
{
    Console.WriteLine($"Time: {histogram.StartTimeStamp}, Count: {histogram.TotalCount}");
}
```

### Binary Encoding

```csharp
// Encode
var buffer = ByteBuffer.Allocate(histogram.GetNeededByteBufferCapacity());
histogram.EncodeIntoCompressedByteBuffer(buffer);

// Decode
var decoded = HistogramEncoding.DecodeFromCompressedByteBuffer(buffer, 0);
```

## Interoperability

The encoding formats are compatible across HdrHistogram implementations:

- **Java**: Original HdrHistogram
- **.NET**: HdrHistogram.NET (this library)
- **Go**: github.com/HdrHistogram/hdrhistogram-go
- **Python**: hdrhistogram
- **C**: HdrHistogram_c

Log files and encoded histograms can be exchanged between implementations.

## Byte Order

- **Header fields**: Big-endian (network byte order)
- **LEB128 counts**: Little-endian base128

## Validation

When decoding:
1. Verify cookie matches expected version
2. Check payload length is reasonable
3. Validate header field ranges
4. Verify count array length matches expectations
5. Check for overflow conditions
