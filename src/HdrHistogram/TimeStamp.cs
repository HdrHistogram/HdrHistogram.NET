using System.Diagnostics;

namespace HdrHistogram
{
    /// <summary>
    /// Helper methods to get time periods based in system stopwatch units.
    /// </summary>
    public static class TimeStamp
    {
        /// <summary>
        /// Return a <see cref="long"/> representing the number system timer ticks that occur over the provided number of seconds.
        /// </summary>
        /// <param name="seconds">A number seconds to represent.</param>
        /// <returns>The number of system timer ticks that represent the <paramref name="seconds"/>.</returns>
        public static long Seconds(int seconds)
        {
            return Stopwatch.Frequency*seconds;
        }

        /// <summary>
        /// Return a <see cref="long"/> representing the number system timer ticks that occur over the provided number of minutes.
        /// </summary>
        /// <param name="minutes">A number minutes to represent.</param>
        /// <returns>The number of system timer ticks that represent the <paramref name="minutes"/>.</returns>
        public static long Minutes(int minutes)
        {
            return Stopwatch.Frequency * minutes * 60L;
        }

        /// <summary>
        /// Return a <see cref="long"/> representing the number system timer ticks that occur over the provided number of hours.
        /// </summary>
        /// <param name="hours">A number hours to represent.</param>
        /// <returns>The number of system timer ticks that represent the <paramref name="hours"/>.</returns>
        public static long Hours(int hours)
        {
            return Stopwatch.Frequency * hours * 60L * 60L;
        }
    }
}