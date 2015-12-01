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
    /// An enumerator of <see cref="HistogramIterationValue"/> through the histogram using a <see cref="LogarithmicEnumerator"/>
    /// </summary>
    sealed class LogarithmicBucketEnumerable : IEnumerable<HistogramIterationValue>
    {
        private readonly HistogramBase _histogram;
        private readonly int _valueUnitsInFirstBucket;
        private readonly double _logBase;

        public LogarithmicBucketEnumerable(HistogramBase histogram, int valueUnitsInFirstBucket, double logBase)
        {
            _histogram = histogram;
            _valueUnitsInFirstBucket = valueUnitsInFirstBucket;
            _logBase = logBase;
        }

        public IEnumerator<HistogramIterationValue> GetEnumerator()
        {
            return new LogarithmicEnumerator(_histogram, _valueUnitsInFirstBucket, _logBase);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Used for iterating through histogram values in logarithmically increasing levels. The iteration is
    /// performed in steps that start at<i>valueUnitsInFirstBucket</i> and increase exponentially according to
    /// <i>logBase</i>, terminating when all recorded histogram values are exhausted. Note that each iteration "bucket"
    /// includes values up to and including the next bucket boundary value.
    /// </summary>
    sealed class LogarithmicEnumerator : AbstractHistogramEnumerator
    {
        private readonly double _logBase;
        private long _nextValueReportingLevel;
        private long _nextValueReportingLevelLowestEquivalent;

        /// <summary>
        /// The constructor for the <see cref="LogarithmicEnumerator"/>
        /// </summary>
        /// <param name="histogram">The histogram this iterator will operate on</param>
        /// <param name="valueUnitsInFirstBucket">the size (in value units) of the first value bucket step</param>
        /// <param name="logBase">the multiplier by which the bucket size is expanded in each iteration step.</param>
        public LogarithmicEnumerator(HistogramBase histogram, int valueUnitsInFirstBucket, double logBase) : base(histogram)
        {
            _logBase = logBase;

            _nextValueReportingLevel = valueUnitsInFirstBucket;
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
            _nextValueReportingLevel *= (long)_logBase;
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
