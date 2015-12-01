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
    /// An enumerator of <see cref="HistogramIterationValue"/> through the histogram using a <see cref="LinearEnumerator"/>
    /// </summary>
    sealed class LinearBucketEnumerable : IEnumerable<HistogramIterationValue>
    {
        private readonly HistogramBase _histogram;
        private readonly int _valueUnitsPerBucket;

        public LinearBucketEnumerable(HistogramBase histogram, int valueUnitsPerBucket)
        {
            this._histogram = histogram;
            this._valueUnitsPerBucket = valueUnitsPerBucket;
        }

        public IEnumerator<HistogramIterationValue> GetEnumerator()
        {
            return new LinearEnumerator(_histogram, _valueUnitsPerBucket);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// Used for iterating through histogram values in linear steps. The iteration is
    /// performed in steps of<i>valueUnitsPerBucket</i> in size, terminating when all recorded histogram
    /// values are exhausted. Note that each iteration "bucket" includes values up to and including
    /// the next bucket boundary value.
    /// </summary>
    sealed class LinearEnumerator : AbstractHistogramEnumerator
    {
        private readonly long _valueUnitsPerBucket;
        private long _nextValueReportingLevel;
        private long _nextValueReportingLevelLowestEquivalent;

        /// <summary>
        /// The constructor for the <see cref="LinearEnumerator"/>
        /// </summary>
        /// <param name="histogram">The histogram this iterator will operate on</param>
        /// <param name="valueUnitsPerBucket">The size (in value units) of each bucket iteration.</param>
        public LinearEnumerator(HistogramBase histogram, int valueUnitsPerBucket) :base(histogram)
        {
            _valueUnitsPerBucket = valueUnitsPerBucket;
            _nextValueReportingLevel = valueUnitsPerBucket;
            _nextValueReportingLevelLowestEquivalent = histogram.LowestEquivalentValue(_nextValueReportingLevel);
        }

        protected override bool HasNext()
        {
            if (base.HasNext())
            {
                return true;
            }
            // If next iterate does not move to the next sub bucket index (which is empty if
            // if we reached this point), then we are not done iterating... Otherwise we're done.
            return (_nextValueReportingLevelLowestEquivalent < NextValueAtIndex);
        }

        protected override void IncrementIterationLevel()
        {
            _nextValueReportingLevel += _valueUnitsPerBucket;
            _nextValueReportingLevelLowestEquivalent = SourceHistogram.LowestEquivalentValue(_nextValueReportingLevel);
        }

        protected override long GetValueIteratedTo()
        {
            return _nextValueReportingLevel;
        }

        protected override bool ReachedIterationLevel()
        {
            return (CurrentValueAtIndex >= _nextValueReportingLevelLowestEquivalent);
        }
    }
}
