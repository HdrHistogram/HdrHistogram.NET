/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */

using System;
using System.Diagnostics;

namespace HdrHistogram
{
    /// <summary>
    /// Extension methods for the Histogram types.
    /// </summary>
    public static class HistogramExtensions
    {
        /// <summary>
        /// Get the highest value that is equivalent to the given value within the histogram's resolution.
        /// Where "equivalent" means that value samples recorded for any two equivalent values are counted in a common
        /// total count.
        /// </summary>
        /// <param name="histogram">The histogram to operate on</param>
        /// <param name="value">The given value</param>
        /// <returns>The highest value that is equivalent to the given value within the histogram's resolution.</returns>
        public static long HighestEquivalentValue(this HistogramBase histogram, long value)
        {
            return histogram.NextNonEquivalentValue(value) - 1;
        }
    }
}