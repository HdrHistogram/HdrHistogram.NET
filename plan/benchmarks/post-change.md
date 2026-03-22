# Post-Change Benchmark Results

## Environment

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat) (container)
Intel Core i5-14400 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.201

Runtimes:

- .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
- .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
- .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3

## ByteBuffer Benchmarks

Run time: 2 min 24 sec, executed benchmarks: 6

| Benchmark | Runtime | Mean | StdDev | Allocated | Op/s |
|-----------|---------|-----:|-------:|----------:|-----:|
| `PutLong` | .NET 10.0 | 3.343 us | 0.0356 us | - | 299,141 |
| `GetLong` | .NET 10.0 | 1.616 us | 0.0242 us | - | 618,738 |
| `PutLong` | .NET 8.0 | 3.390 us | 0.0755 us | - | 294,951 |
| `GetLong` | .NET 8.0 | 1.708 us | 0.0147 us | - | 585,364 |
| `PutLong` | .NET 9.0 | 3.353 us | 0.0315 us | - | 298,248 |
| `GetLong` | .NET 9.0 | 1.622 us | 0.0214 us | - | 616,435 |

`Allocated = -` means zero heap allocations per operation.

## Serialisation Benchmarks

Run time: 4 min 4 sec, executed benchmarks: 12

| Benchmark | Runtime | Mean | StdDev | Allocated | Op/s |
|-----------|---------|-----:|-------:|----------:|-----:|
| `Encode` | .NET 10.0 | 144.15 us | 3.510 us | 113.94 KB | 6,937 |
| `Decode` | .NET 10.0 | 54.91 us | 0.700 us | 184.77 KB | 18,213 |
| `EncodeCompressed` | .NET 10.0 | 174.80 us | 4.541 us | 483.95 KB | 5,721 |
| `DecodeCompressed` | .NET 10.0 | 105.15 us | 2.473 us | 374.62 KB | 9,511 |
| `Encode` | .NET 8.0 | 138.58 us | 1.942 us | 113.94 KB | 7,216 |
| `Decode` | .NET 8.0 | 56.37 us | 1.415 us | 184.86 KB | 17,740 |
| `EncodeCompressed` | .NET 8.0 | 194.32 us | 5.137 us | 483.96 KB | 5,146 |
| `DecodeCompressed` | .NET 8.0 | 106.25 us | 2.340 us | 374.73 KB | 9,412 |
| `Encode` | .NET 9.0 | 133.50 us | 2.011 us | 113.94 KB | 7,491 |
| `Decode` | .NET 9.0 | 56.01 us | 1.009 us | 184.83 KB | 17,855 |
| `EncodeCompressed` | .NET 9.0 | 181.65 us | 3.267 us | 483.88 KB | 5,505 |
| `DecodeCompressed` | .NET 9.0 | 108.94 us | 2.445 us | 374.69 KB | 9,179 |

The serialisation allocation figures represent the full histogram encode/decode cycle including the codec's internal working buffers, not solely `ByteBuffer` operations.
The `ByteBuffer` `PutLong`/`GetLong` operations themselves allocate `0 B`, confirming the elimination of intermediate `byte[]` allocations.
