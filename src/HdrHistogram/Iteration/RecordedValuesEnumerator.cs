/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

using System.Collections;
using System.Collections.Generic;

namespace HdrHistogram.Iteration
{
    /// <summary>
    /// An enumerator of <see cref="HistogramIterationValue"/> through the histogram using a <see cref="RecordedValuesEnumerator"/>
    /// </summary>
    sealed class RecordedValuesEnumerable : IEnumerable<HistogramIterationValue>
    {
        readonly HistogramBase _histogram;

        public RecordedValuesEnumerable(HistogramBase histogram)
        {
            _histogram = histogram;
        }

        public IEnumerator<HistogramIterationValue> GetEnumerator()
        {
            return new RecordedValuesEnumerator(_histogram);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    sealed class RecordedValuesEnumerator : AbstractHistogramEnumerator
    {
        private int _visitedSubBucketIndex;
        private int _visitedBucketIndex;

        /// <summary>
        /// The constructor for <see cref="RecordedValuesEnumerator"/>
        /// </summary>
        /// <param name="histogram">The histogram this iterator will operate on</param>
        public RecordedValuesEnumerator(HistogramBase histogram) :base(histogram)
        {
            _visitedSubBucketIndex = -1;
            _visitedBucketIndex = -1;
        }

        protected override void IncrementIterationLevel()
        {
            _visitedSubBucketIndex = CurrentSubBucketIndex;
            _visitedBucketIndex = CurrentBucketIndex;
        }

        protected override bool ReachedIterationLevel()
        {
            long currentIJCount = SourceHistogram.GetCountAt(CurrentBucketIndex, CurrentSubBucketIndex);
            return (currentIJCount != 0) &&
                    ((_visitedSubBucketIndex != CurrentSubBucketIndex) || (_visitedBucketIndex != CurrentBucketIndex));
        }
    }
}
