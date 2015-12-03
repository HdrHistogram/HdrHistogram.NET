/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

using System;
using HdrHistogram.Utilities;
using LongBuffer = HdrHistogram.Utilities.WrappedBuffer<long>;

namespace HdrHistogram
{
    /// <summary>
    /// A High Dynamic Range (HDR) Histogram
    /// </summary>
    /// <remarks>
    /// Histogram supports the recording and analyzing sampled data value counts across a configurable integer value
    /// range with configurable value precision within the range.
    /// Value precision is expressed as the number of significant digits in the value recording, and provides control 
    /// over value quantization behavior across the value range and the subsequent value resolution at any given level.
    /// <para>
    /// For example, a Histogram could be configured to track the counts of observed integer values between 0 and
    /// 36,000,000,000 while maintaining a value precision of 3 significant digits across that range.
    /// Value quantization within the range will thus be no larger than 1/1,000th (or 0.1%) of any value.
    /// This example Histogram could be used to track and analyze the counts of observed response times ranging between
    /// 1 tick (100 nanoseconds) and 1 hour in magnitude, while maintaining a value resolution of 100 nanosecond up to 
    /// 100 microseconds, a resolution of 1 millisecond(or better) up to one second, and a resolution of 1 second 
    /// (or better) up to 1,000 seconds.
    /// At it's maximum tracked value(1 hour), it would still maintain a resolution of 3.6 seconds (or better).
    /// </para>
    /// Histogram tracks value counts in <see cref="long"/> fields.
    /// Smaller field types are available in the <see cref="IntHistogram"/> and <see cref="ShortHistogram"/> 
    /// implementations of <see cref="HistogramBase"/>.
    /// </remarks>
    public class LongHistogram : HistogramBase
    {
        private readonly long[] _counts;

        // We try to cache the LongBuffer used in output cases, as repeated output from the same histogram using the same buffer is likely.
        private LongBuffer _cachedDstLongBuffer;
        private ByteBuffer _cachedDstByteBuffer;
        private int _cachedDstByteBufferPosition;
        private long _totalCount;

        /// <summary>
        /// Construct a Histogram given the highest value to be tracked and a number of significant decimal digits. 
        /// The histogram will be constructed to implicitly track(distinguish from 0) values as low as 1.
        /// </summary>
        /// <param name="highestTrackableValue">The highest value to be tracked by the histogram. Must be a positive integer that is &gt;= 2.</param>
        /// <param name="numberOfSignificantValueDigits">The number of significant decimal digits to which the histogram will maintain value resolution and separation.
        /// Must be a non-negative integer between 0 and 5.
        /// </param>
        public LongHistogram(long highestTrackableValue, int numberOfSignificantValueDigits)
            : this(1, highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        /// <summary>
        /// Construct a <see cref="LongHistogram"/> given the lowest and highest values to be tracked and a number of significant decimal digits.
        /// Providing a <paramref name="lowestTrackableValue"/> is useful is situations where the units used for the histogram's values are much smaller that the minimal accuracy required. 
        /// For example when tracking time values stated in tick (100 nanosecond units), where the minimal accuracy required is a microsecond, the proper value for <paramref name="lowestTrackableValue"/> would be 10.
        /// </summary>
        /// <param name="lowestTrackableValue">
        /// The lowest value that can be tracked (distinguished from 0) by the histogram.
        /// Must be a positive integer that is &gt;= 1. 
        /// May be internally rounded down to nearest power of 2.
        /// </param>
        /// <param name="highestTrackableValue">The highest value to be tracked by the histogram. Must be a positive integer that is &gt;= (2 * <paramref name="lowestTrackableValue"/>).</param>
        /// <param name="numberOfSignificantValueDigits">The number of significant decimal digits to which the histogram will maintain value resolution and separation.
        /// Must be a non-negative integer between 0 and 5.
        /// </param>
        public LongHistogram(long lowestTrackableValue, long highestTrackableValue,
                         int numberOfSignificantValueDigits)
            : base(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits)
        {
            _counts = new long[CountsArrayLength];
        }


        /// <summary>
        /// Gets the total number of recorded values.
        /// </summary>
        public override long TotalCount { get { return _totalCount; } protected set { _totalCount = value; } }

        /// <summary>
        /// Returns the word size of this implementation
        /// </summary>
        protected override int WordSizeInBytes => 8;

        /// <summary>
        /// Create a copy of this histogram, complete with data and everything.
        /// </summary>
        /// <returns>A distinct copy of this histogram.</returns>
        public override HistogramBase Copy()
        {
            var copy = new LongHistogram(LowestTrackableValue, HighestTrackableValue, NumberOfSignificantValueDigits);
            copy.Add(this);
            return copy;
        }

        /// <summary>
        /// Get a copy of this histogram, corrected for coordinated omission.
        /// </summary>
        /// <param name="expectedIntervalBetweenValueSamples">If <paramref name="expectedIntervalBetweenValueSamples"/> is larger than 0, add auto-generated value records as appropriate if value is larger than <c>expectedIntervalBetweenValueSamples</c></param>
        /// <returns>a copy of this histogram, corrected for coordinated omission.</returns>
        /// <remarks>
        /// To compensate for the loss of sampled values when a recorded value is larger than the expected interval between value samples, 
        /// the new histogram will include an auto-generated additional series of decreasingly-smaller(down to the <paramref name="expectedIntervalBetweenValueSamples"/>) 
        /// value records for each count found in the current histogram that is larger than the expectedIntervalBetweenValueSamples.
        /// <para>
        /// Note: This is a post-correction method, as opposed to the at-recording correction method provided by <seealso cref="HistogramBase.RecordValueWithExpectedInterval"/>. 
        /// The two methods are mutually exclusive, and only one of the two should be be used on a given data set to correct for the same coordinated omission issue.
        /// </para>
        /// See notes in the description of the Histogram calls for an illustration of why this corrective behavior is important.
        /// </remarks>
        public override HistogramBase CopyCorrectedForCoordinatedOmission(long expectedIntervalBetweenValueSamples)
        {
            var toHistogram = new LongHistogram(LowestTrackableValue, HighestTrackableValue, NumberOfSignificantValueDigits);
            toHistogram.AddWhileCorrectingForCoordinatedOmission(this, expectedIntervalBetweenValueSamples);
            return toHistogram;
        }

        /// <summary>
        /// Construct a new histogram by decoding it from a ByteBuffer.
        /// </summary>
        /// <param name="buffer">The buffer to decode from</param>
        /// <param name="minBarForHighestTrackableValue">Force highestTrackableValue to be set at least this high</param>
        /// <returns>The newly constructed histogram</returns>
        public static LongHistogram DecodeFromByteBuffer(ByteBuffer buffer, long minBarForHighestTrackableValue)
        {
            return DecodeFromByteBuffer<LongHistogram>(buffer, minBarForHighestTrackableValue);
        }

        /// <summary>
        /// Gets the number of recorded values at a given index.
        /// </summary>
        /// <param name="index">The index to get the count for</param>
        /// <returns>The number of recorded values at the given index.</returns>
        protected override long GetCountAtIndex(int index)
        {
            return _counts[index];
        }

        /// <summary>
        /// Increments the count at the given index. Will also increment the <see cref="HistogramBase.TotalCount"/>.
        /// </summary>
        /// <param name="index">The index to increment the count at.</param>
        protected override void IncrementCountAtIndex(int index)
        {
            _counts[index]++;
            _totalCount++;
        }

        /// <summary>
        /// Adds the specified amount to the count of the provided index. Also increments the <see cref="HistogramBase.TotalCount"/> by the same amount.
        /// </summary>
        /// <param name="index">The index to increment.</param>
        /// <param name="addend">The amount to increment by.</param>
        protected override void AddToCountAtIndex(int index, long addend)
        {
            _counts[index] += addend;
            _totalCount += addend;
        }

        /// <summary>
        /// Clears the counts of this implementation.
        /// </summary>
        protected override void ClearCounts()
        {
            Array.Clear(_counts, 0, _counts.Length);
            _totalCount = 0;
        }

        /// <summary>
        /// Copies data from the provided buffer into the internal counts array.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="length">The length of the buffer to read.</param>
        protected override void FillCountsArrayFromBuffer(ByteBuffer buffer, int length)
        {
            lock (UpdateLock)
            {
                buffer.AsLongBuffer().Get(_counts, 0, length);
            }
        }

        /// <summary>
        /// Writes the data from the internal counts array into the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write to</param>
        /// <param name="length">The length to write.</param>
        protected override void FillBufferFromCountsArray(ByteBuffer buffer, int length)
        {
            lock (UpdateLock)
            {
                if ((_cachedDstLongBuffer == null) ||
                    (buffer != _cachedDstByteBuffer) ||
                    (buffer.Position != _cachedDstByteBufferPosition))
                {
                    _cachedDstByteBuffer = buffer;
                    _cachedDstByteBufferPosition = buffer.Position;
                    _cachedDstLongBuffer = buffer.AsLongBuffer();
                }
                _cachedDstLongBuffer.Rewind();
                _cachedDstLongBuffer.Put(_counts, 0, length);
            }
        }
    }
}
