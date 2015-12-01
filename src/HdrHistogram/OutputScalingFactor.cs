using System;

namespace HdrHistogram
{
    /// <summary>
    /// Provides constants to use in selecting a scaling factor for output of a histograms recordings.
    /// </summary>
    public static class OutputScalingFactor
    {
        /// <summary>
        /// For use when values are recorded in ticks and output should be in ticks
        /// </summary>
        public const double None = 1.0;

        /// <summary>
        /// For use when values are recorded in ticks and output should be measured in microseconds.
        /// </summary>
        public const double TicksToMicroseconds = 10.0;

        /// <summary>
        /// For use when values are recorded in ticks and output should be measured in milliseconds.
        /// </summary>
        public const double TicksToMilliseconds = TimeSpan.TicksPerMillisecond;

        /// <summary>
        /// For use when values are recorded in ticks and output should be measured in seconds.
        /// </summary>
        public const double TicksToSeconds = TimeSpan.TicksPerSecond;
    }
}