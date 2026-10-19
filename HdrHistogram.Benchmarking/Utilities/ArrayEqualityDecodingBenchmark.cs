/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */
using BenchmarkDotNet.Attributes;
using HdrHistogram.Encoding;
using HdrHistogram.Utilities;

namespace HdrHistogram.Benchmarking.Utilities
{
    /// <summary>
    /// Measures histogram decoding, whose constructor lookup compares Type arrays.
    /// </summary>
    [MemoryDiagnoser]
    public class ArrayEqualityDecodingBenchmark
    {
        private ByteBuffer _buffer = null!;

        /// <summary>
        /// Encodes a histogram outside the measured decoding operation.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var histogram = new LongHistogram(3600000000, 3);
            histogram.RecordValue(1000);
            histogram.RecordValue(1000000);
            _buffer = ByteBuffer.Allocate(histogram.GetNeededByteBufferCapacity());
            histogram.Encode(_buffer, HistogramEncoderV2.Instance);
        }

        /// <summary>
        /// Decodes a histogram through the public API.
        /// </summary>
        /// <returns>The decoded histogram.</returns>
        [Benchmark]
        public HistogramBase DecodeHistogram()
        {
            _buffer.Position = 0;
            return HistogramEncoding.DecodeFromByteBuffer(_buffer, 0);
        }
    }
}
