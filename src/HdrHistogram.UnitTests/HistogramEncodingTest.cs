/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

using HdrHistogram.Encoding;
using HdrHistogram.Utilities;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public sealed class LongHistogramEncodingTests
    {
        private static readonly HistogramEncoderV2 EncoderV2 = new Encoding.HistogramEncoderV2();
        private const long HighestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units

        

        [Test]
        public void Given_a_populated_Histogram_When_encoded_and_decoded_Then_data_is_preserved()
        {
            var source = Create(HighestTrackableValue, 3);
            Load(source);
            var result = EncodeDecode(source);
            HistogramAssert.AreEqual(source, result);
        }

        [Test]
        public void Given_a_populated_Histogram_When_encoded_and_decoded_with_compression_Then_data_is_preserved()
        {
            var source = Create(HighestTrackableValue, 3);
            Load(source);
            var result = CompressedEncodeDecode(source);
            HistogramAssert.AreEqual(source, result);
        }

        [Test]
        public void Given_a_Histogram_populated_with_full_range_of_values_When_encoded_and_decoded_Then_data_is_preserved()
        {
            var source = Create(HighestTrackableValue, 3);
            LoadFullRange(source);
            var result = EncodeDecode(source);
            HistogramAssert.AreEqual(source, result);
        }

        [Test]
        public void Given_a_Histogram_populated_with_full_range_of_values_When_encoded_and_decoded_with_compression_Then_data_is_preserved()
        {
            var source = Create(HighestTrackableValue, 3);
            LoadFullRange(source);
            var result = CompressedEncodeDecode(source);
            HistogramAssert.AreEqual(source, result);
        }

        private LongHistogram Create(long highestTrackableValue, int numberOfSignificantDigits)
        {
            return new LongHistogram(highestTrackableValue, numberOfSignificantDigits);
        }

        private static LongHistogram EncodeDecode(LongHistogram source)
        {
            var targetBuffer = ByteBuffer.Allocate(source.GetNeededByteBufferCapacity());
            source.Encode(targetBuffer, EncoderV2);
            targetBuffer.Position = 0;
            return (LongHistogram)HistogramEncoding.DecodeFromByteBuffer(targetBuffer, 0);
        }

        private static LongHistogram CompressedEncodeDecode(LongHistogram source)
        {
            var targetBuffer = ByteBuffer.Allocate(source.GetNeededByteBufferCapacity());
            source.EncodeIntoCompressedByteBuffer(targetBuffer);
            targetBuffer.Position = 0;
            return (LongHistogram)HistogramEncoding.DecodeFromCompressedByteBuffer(targetBuffer, 0);
        }

        private static void Load(LongHistogram source)
        {
            for (long i = 0L; i < 10000L; i++)
            {
                source.RecordValue(1000L * i);
            }
        }

        private static void LoadFullRange(LongHistogram source)
        {
            for (long i = 0L; i < HighestTrackableValue; i += 100L)
            {
                source.RecordValue(i);
            }
            source.RecordValue(HighestTrackableValue);
        }

    }
}
