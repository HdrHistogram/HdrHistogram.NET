/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */

using System;

namespace HdrHistogram.Utilities
{
    /// <summary>
    /// Exposes optimised methods to get Leading Zero Count.
    /// </summary>
    public static class Bitwise
    {
        /// <summary>
        /// Returns the Leading Zero Count (lzc) of the <paramref name="value"/> for its binary representation.
        /// </summary>
        /// <param name="value">The value to find the number of leading zeros</param>
        /// <returns>The number of leading zeros.</returns>
        public static int NumberOfLeadingZeros(long value)
        {
            return System.Numerics.BitOperations.LeadingZeroCount((ulong)value);
        }
    }
}
