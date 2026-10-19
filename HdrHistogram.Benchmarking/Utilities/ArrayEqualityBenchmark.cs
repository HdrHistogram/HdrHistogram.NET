/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */
using BenchmarkDotNet.Attributes;
using HdrHistogram.Utilities;

namespace HdrHistogram.Benchmarking.Utilities
{
    /// <summary>
    /// Measures equality checks over separate, equal long arrays.
    /// </summary>
    [MemoryDiagnoser]
    public class ArrayEqualityBenchmark
    {
        private readonly long[] _source = new long[23552];
        private readonly long[] _other = new long[23552];

        /// <summary>
        /// Compares approximately 184 KiB of long values.
        /// </summary>
        /// <returns>Whether all elements are equal.</returns>
        [Benchmark]
        public bool EqualLongArrays()
        {
            return _source.IsSequenceEqual(_other);
        }
    }
}
