using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HdrHistogram.Iteration;

namespace HdrHistogram
{
    /// <summary>
    /// Extension methods for the Histogram types.
    /// </summary>
    public static class HistogramExtensions
    {
        /// <summary>
        /// Get the lowest recorded value level in the histogram
        /// </summary>
        /// <returns>the Min value recorded in the histogram</returns>
        public static long GetMinValue(this HistogramBase histogram)
        {
            var min = histogram.RecordedValues().Select(hiv => hiv.ValueIteratedTo).FirstOrDefault();
            return histogram.LowestEquivalentValue(min);
        }

        /// <summary>
        /// Get the highest recorded value level in the histogram
        /// </summary>
        /// <returns>the Max value recorded in the histogram</returns>
        public static long GetMaxValue(this HistogramBase histogram)
        {
            var max = histogram.RecordedValues().Select(hiv => hiv.ValueIteratedTo).LastOrDefault();
            return histogram.LowestEquivalentValue(max);
        }

        /// <summary>
        /// Get the computed mean value of all recorded values in the histogram
        /// </summary>
        /// <returns>the mean value (in value units) of the histogram data</returns>
        public static double GetMean(this HistogramBase histogram)
        {
            var totalValue = histogram.RecordedValues().Select(hiv => hiv.TotalValueToThisValue).LastOrDefault();
            return (totalValue * 1.0) / histogram.TotalCount;
        }

        /// <summary>
        /// Get the computed standard deviation of all recorded values in the histogram
        /// </summary>
        /// <returns>the standard deviation (in value units) of the histogram data</returns>
        public static double GetStdDeviation(this HistogramBase histogram)
        {
            var mean = histogram.GetMean();
            var geometricDeviationTotal = 0.0;
            foreach (var iterationValue in histogram.RecordedValues())
            {
                double deviation = (histogram.MedianEquivalentValue(iterationValue.ValueIteratedTo) * 1.0) - mean;
                geometricDeviationTotal += (deviation * deviation) * iterationValue.CountAddedInThisIterationStep;
            }
            var stdDeviation = Math.Sqrt(geometricDeviationTotal / histogram.TotalCount);
            return stdDeviation;
        }

        /// <summary>
        /// Copy this histogram into the target histogram, overwriting it's contents.
        /// </summary>
        /// <param name="source">The source histogram</param>
        /// <param name="targetHistogram">the histogram to copy into</param>
        public static void CopyInto(this HistogramBase source, HistogramBase targetHistogram)
        {
            targetHistogram.Reset();
            targetHistogram.Add(source);
            targetHistogram.StartTimeStamp = source.StartTimeStamp;
            targetHistogram.EndTimeStamp = source.EndTimeStamp;
        }

        /// <summary>
        /// Produce textual representation of the value distribution of histogram data by percentile. 
        /// The distribution is output with exponentially increasing resolution, with each exponentially decreasing 
        /// half-distance containing <paramref name="percentileTicksPerHalfDistance"/> percentile reporting tick points.
        /// </summary>
        /// <param name="histogram">The histogram to operate on</param>
        /// <param name="printStream">Stream into which the distribution will be output</param>
        /// <param name="percentileTicksPerHalfDistance">
        /// The number of reporting points per exponentially decreasing half-distance
        /// </param>
        /// <param name="outputValueUnitScalingRatio">
        /// The scaling factor by which to divide histogram recorded values units in output.
        /// Use the <see cref="OutputScalingFactor"/> constant values to help choose an appropriate output measurement.
        /// </param>
        /// <param name="useCsvFormat">Output in CSV (Comma Separated Values) format if <c>true</c>, else use plain text form.</param>
        public static void OutputPercentileDistribution(this HistogramBase histogram,
            TextWriter printStream,
            int percentileTicksPerHalfDistance = 5,
            double outputValueUnitScalingRatio = OutputScalingFactor.TicksToMilliseconds,
            bool useCsvFormat = false)
        {
            if (useCsvFormat)
            {
                printStream.Write("\"Value\",\"Percentile\",\"TotalCount\",\"1/(1-Percentile)\"\n");
            }
            else
            {
                printStream.Write("{0,12} {1,14} {2,10} {3,14}\n\n", "Value", "Percentile", "TotalCount", "1/(1-Percentile)");
            }


            string percentileFormatString;
            string lastLinePercentileFormatString;
            if (useCsvFormat)
            {
                percentileFormatString = "{0:F" + histogram.NumberOfSignificantValueDigits + "},{1:F12},{2},{3:F2}\n";
                lastLinePercentileFormatString = "{0:F" + histogram.NumberOfSignificantValueDigits + "},{1:F12},{2},Infinity\n";
            }
            else
            {
                percentileFormatString = "{0,12:F" + histogram.NumberOfSignificantValueDigits + "}" + " {1,2:F12} {2,10} {3,14:F2}\n";
                lastLinePercentileFormatString = "{0,12:F" + histogram.NumberOfSignificantValueDigits + "} {1,2:F12} {2,10}\n";
            }

            try
            {
                foreach (var iterationValue in histogram.Percentiles(percentileTicksPerHalfDistance))
                {
                    if (iterationValue.PercentileLevelIteratedTo != 100.0D)
                    {
                        printStream.Write(percentileFormatString,
                            iterationValue.ValueIteratedTo / outputValueUnitScalingRatio,
                            iterationValue.PercentileLevelIteratedTo / 100.0D,
                            iterationValue.TotalCountToThisValue,
                            1 / (1.0D - (iterationValue.PercentileLevelIteratedTo / 100.0D)));
                    }
                    else
                    {
                        printStream.Write(lastLinePercentileFormatString,
                            iterationValue.ValueIteratedTo / outputValueUnitScalingRatio,
                            iterationValue.PercentileLevelIteratedTo / 100.0D,
                            iterationValue.TotalCountToThisValue);
                    }
                }

                if (!useCsvFormat)
                {
                    // Calculate and output mean and std. deviation.
                    // Note: mean/std. deviation numbers are very often completely irrelevant when
                    // data is extremely non-normal in distribution (e.g. in cases of strong multi-modal
                    // response time distribution associated with GC pauses). However, reporting these numbers
                    // can be very useful for contrasting with the detailed percentile distribution
                    // reported by outputPercentileDistribution(). It is not at all surprising to find
                    // percentile distributions where results fall many tens or even hundreds of standard
                    // deviations away from the mean - such results simply indicate that the data sampled
                    // exhibits a very non-normal distribution, highlighting situations for which the std.
                    // deviation metric is a useless indicator.

                    var mean = histogram.GetMean() / outputValueUnitScalingRatio;
                    var stdDeviation = histogram.GetStdDeviation() / outputValueUnitScalingRatio;
                    printStream.Write("#[Mean    = {0,12:F" + histogram.NumberOfSignificantValueDigits + "}, " +
                                      "StdDeviation   = {1,12:F" + histogram.NumberOfSignificantValueDigits + "}]\n", mean, stdDeviation);
                    printStream.Write("#[Max     = {0,12:F" + histogram.NumberOfSignificantValueDigits + "}, Total count    = {1,12}]\n",
                        histogram.GetMaxValue() / outputValueUnitScalingRatio, histogram.TotalCount);
                    printStream.Write("#[Buckets = {0,12}, SubBuckets     = {1,12}]\n",
                        histogram.BucketCount, histogram.SubBucketCount);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Overflow conditions on histograms can lead to ArgumentOutOfRangeException on iterations:
                if (histogram.HasOverflowed())
                {
                    printStream.Write("# Histogram counts indicate OVERFLOW values");
                }
                else
                {
                    // Re-throw if reason is not a known overflow:
                    throw;
                }
            }
        }

        /// <summary>
        /// Provide a means of iterating through histogram values according to percentile levels. 
        /// The iteration is performed in steps that start at 0% and reduce their distance to 100% according to the
        /// <paramref name="percentileTicksPerHalfDistance"/> parameter, ultimately reaching 100% when all recorded
        /// histogram values are exhausted.
        /// </summary>
        /// <param name="histogram">The histogram to operate on</param>
        /// <param name="percentileTicksPerHalfDistance">
        /// The number of iteration steps per half-distance to 100%.
        /// </param>
        /// <returns>
        /// An enumerator of <see cref="HistogramIterationValue"/> through the histogram using a
        /// <see cref="PercentileEnumerator"/>.
        /// </returns>
        public static IEnumerable<HistogramIterationValue> Percentiles(this HistogramBase histogram, int percentileTicksPerHalfDistance)
        {
            return new PercentileEnumerable(histogram, percentileTicksPerHalfDistance);
        }

        /// <summary>
        /// Provide a means of iterating through histogram values using linear steps. The iteration is performed in
        /// steps of <paramref name="valueUnitsPerBucket"/> in size, terminating when all recorded histogram values
        /// are exhausted.
        /// </summary>
        /// <param name="histogram">The histogram to operate on</param>
        /// <param name="valueUnitsPerBucket">The size (in value units) of the linear buckets to use</param>
        /// <returns>
        /// An enumerator of <see cref="HistogramIterationValue"/> through the histogram using a 
        /// <see cref="LinearEnumerator"/>.
        /// </returns>
        public static IEnumerable<HistogramIterationValue> LinearBucketValues(this HistogramBase histogram, int valueUnitsPerBucket)
        {
            return new LinearBucketEnumerable(histogram, valueUnitsPerBucket);
        }

        /// <summary>
        /// Provide a means of iterating through histogram values at logarithmically increasing levels. 
        /// The iteration is performed in steps that start at<i>valueUnitsInFirstBucket</i> and increase exponentially
        /// according to <paramref name="logBase"/>, terminating when all recorded histogram values are exhausted.
        /// </summary>
        /// <param name="histogram">The histogram to operate on</param>
        /// <param name="valueUnitsInFirstBucket">The size (in value units) of the first bucket in the iteration</param>
        /// <param name="logBase">The multiplier by which bucket sizes will grow in each iteration step</param>
        /// <returns>
        /// An enumerator of <see cref="HistogramIterationValue"/> through the histogram using a
        /// <see cref="LogarithmicEnumerator"/>.
        /// </returns>
        public static IEnumerable<HistogramIterationValue> LogarithmicBucketValues(this HistogramBase histogram, int valueUnitsInFirstBucket, double logBase)
        {
            return new LogarithmicBucketEnumerable(histogram, valueUnitsInFirstBucket, logBase);
        }


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

        /// <summary>
        /// Executes the action and records the time to complete the action. The time is recorded in ticks.
        /// </summary>
        /// <param name="histogram">The Histogram to record the latency in.</param>
        /// <param name="action">The functionality to execute and measure</param>
        /// <remarks>
        /// Ticks are used as the unit of recording here as they are the smallest unit that .NET can measure
        /// and require no conversion at time of recording. Instead conversion (scaling) can be done at time
        /// of output to microseconds, milliseconds, seconds or other appropriate unit.
        /// </remarks>
        public static void RecordLatency(this HistogramBase histogram, Action action)
        {
            var start = Stopwatch.GetTimestamp();
            action();
            var elapsedTicks = (Stopwatch.GetTimestamp() - start);
            histogram.RecordValue(elapsedTicks);
        }
    }
}