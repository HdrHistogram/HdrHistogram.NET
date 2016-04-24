/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HdrHistogram.Iteration;
using HdrHistogram.Output;

namespace HdrHistogram
{
    /// <summary>
    /// Extension methods for the Histogram types.
    /// </summary>
    public static class HistogramExtensions
    {

        /// <summary>
        /// Get the highest recorded value level in the histogram
        /// </summary>
        /// <returns>the Max value recorded in the histogram</returns>
        public static long GetMaxValue(this HistogramBase histogram)
        {
            var max = histogram.RecordedValues().Select(hiv => hiv.ValueIteratedTo).LastOrDefault();
            return histogram.HighestEquivalentValue(max);
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
        /// Produce textual representation of the value distribution of histogram data by percentile. 
        /// The distribution is output with exponentially increasing resolution, with each exponentially decreasing 
        /// half-distance containing <paramref name="percentileTicksPerHalfDistance"/> percentile reporting tick points.
        /// </summary>
        /// <param name="histogram">The histogram to operate on</param>
        /// <param name="writer">The <see cref="TextWriter"/> into which the distribution will be output</param>
        /// <param name="percentileTicksPerHalfDistance">
        /// The number of reporting points per exponentially decreasing half-distance
        /// </param>
        /// <param name="outputValueUnitScalingRatio">
        /// The scaling factor by which to divide histogram recorded values units in output.
        /// Use the <see cref="OutputScalingFactor"/> constant values to help choose an appropriate output measurement.
        /// </param>
        /// <param name="useCsvFormat">Output in CSV (Comma Separated Values) format if <c>true</c>, else use plain text form.</param>
        public static void OutputPercentileDistribution(this HistogramBase histogram,
            TextWriter writer,
            int percentileTicksPerHalfDistance = 5,
            double outputValueUnitScalingRatio = OutputScalingFactor.TicksToMilliseconds,
            bool useCsvFormat = false)
        {
            var formatter = useCsvFormat
                ? (IOutputFormatter)new CsvOutputFormatter(writer, histogram.NumberOfSignificantValueDigits, outputValueUnitScalingRatio)
                : (IOutputFormatter)new HgrmOutputFormatter(writer, histogram.NumberOfSignificantValueDigits, outputValueUnitScalingRatio);

            try
            {
                formatter.WriteHeader();
                foreach (var iterationValue in histogram.Percentiles(percentileTicksPerHalfDistance))
                {
                    formatter.WriteValue(iterationValue);
                }
                formatter.WriteFooter(histogram);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Overflow conditions on histograms can lead to ArgumentOutOfRangeException on iterations:
                if (histogram.HasOverflowed())
                {
                    writer.Write("# Histogram counts indicate OVERFLOW values");
                }
                else
                {
                    // Re-throw if reason is not a known overflow:
                    throw;
                }
            }
        }
    }

}