using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HdrHistogram.Utilities;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class HistogramLogReaderWriterTests
    {
        [Test]
        public void CanReadEmptyLog()
        {
            byte[] data;
            var startTimeWritten = DateTime.Now;
            var expectedStartTime = startTimeWritten.SecondsSinceUnixEpoch()
                .Round(3)
                .ToDateFromSecondsSinceEpoch();

            using (var writerStream = new MemoryStream())
            {
                Histogram.Write(writerStream, startTimeWritten);
                data = writerStream.ToArray();
            }

            using (var readerStream = new MemoryStream(data))
            {
                var reader = new HistogramLogReader(readerStream);
                CollectionAssert.IsEmpty(reader.ReadHistograms().ToList());
                var actualStartTime = reader.GetStartTime();
                Assert.AreEqual(expectedStartTime, actualStartTime);
            }
        }

        [TestCase(3600L * 1000 * 1000, 3, 1000)]
        [TestCase(long.MaxValue / 2, 3, 1000)]
        public void CanRoundTripSingleHistogram(long highestTrackableValue, int significantDigits, int multiplier)
        {
            var histogram = CreatePopulatedHistogram(highestTrackableValue, significantDigits, multiplier);
            var startTimeWritten = DateTime.Now;
            var endTimeWritten = startTimeWritten.AddMinutes(30);

            histogram.StartTimeStamp = startTimeWritten.MillisecondsSinceUnixEpoch();
            histogram.EndTimeStamp = endTimeWritten.MillisecondsSinceUnixEpoch();

            var data = WriteLog(startTimeWritten, histogram);
            var actualHistograms = ReadHistograms(data);

            Assert.AreEqual(1, actualHistograms.Length);
            HistogramAssert.AreEqual(histogram, actualHistograms.Single());
        }

        [Test, TestCaseSource(nameof(PowersOfTwo))]
        public void CanRoundTripSingleHistogramsWithFullRangesOfCountsAndValues(long count)
        {
            var value = 1;
            var highestTrackableValue = long.MaxValue - 1L;
            var significantDigits = 3;
            var histogram = new LongHistogram(highestTrackableValue, significantDigits);
            histogram.RecordValueWithCount(value,count);
            
            var startTimeWritten = DateTime.Now;
            var endTimeWritten = startTimeWritten.AddMinutes(30);

            histogram.StartTimeStamp = startTimeWritten.MillisecondsSinceUnixEpoch();
            histogram.EndTimeStamp = endTimeWritten.MillisecondsSinceUnixEpoch();

            var data = WriteLog(startTimeWritten, histogram);
            var actualHistograms = ReadHistograms(data);

            Assert.AreEqual(1, actualHistograms.Length);
            HistogramAssert.AreEqual(histogram, actualHistograms.Single());
        }

        [Test]
        public void CanRoundTripSingleHistogramsWithSparseValues()
        {
            var histogram = new LongHistogram(highestTrackableValue:long.MaxValue-1, numberOfSignificantValueDigits:3);
            histogram.RecordValue(1);
            histogram.RecordValue((long.MaxValue / 2)+1);

            var startTimeWritten = DateTime.Now;
            var endTimeWritten = startTimeWritten.AddMinutes(30);
            histogram.StartTimeStamp = startTimeWritten.MillisecondsSinceUnixEpoch();
            histogram.EndTimeStamp = endTimeWritten.MillisecondsSinceUnixEpoch();

            var data = WriteLog(startTimeWritten, histogram);
            var actualHistograms = ReadHistograms(data);

            Assert.AreEqual(1, actualHistograms.Length);
            HistogramAssert.AreEqual(histogram, actualHistograms.Single());
        }

        [TestCase("jHiccup-2.0.7S.logV2.hlog")]
        public void CanReadv2Logs(string logPath)
        {
            var readerStream = GetEmbeddedFileStream(logPath);
            var reader = new HistogramLogReader(readerStream);
            int histogramCount = 0;
            long totalCount = 0;
            var accumulatedHistogram = new LongHistogram(85899345920838, 3);
            foreach (var histogram in reader.ReadHistograms())
            {
                histogramCount++;
                Assert.IsInstanceOf<HistogramBase>(histogram, "Expected integer value histograms in log file");

                totalCount += histogram.TotalCount;
                accumulatedHistogram.Add(histogram);
            }

            Assert.AreEqual(62, histogramCount);
            Assert.AreEqual(48761, totalCount);
            Assert.AreEqual(1745879039, accumulatedHistogram.GetValueAtPercentile(99.9));
            Assert.AreEqual(1796210687, accumulatedHistogram.GetMaxValue());
            Assert.AreEqual(1441812279.474, reader.GetStartTime().SecondsSinceUnixEpoch());
        }


        [TestCase("jHiccup-2.0.1.logV0.hlog", 0, int.MaxValue, 81, 61256, 1510998015, 1569718271, 1438869961.225)]
        [TestCase("jHiccup-2.0.1.logV0.hlog", 19, 25, 25, 18492, 459007, 623103, 1438869961.225)]
        [TestCase("jHiccup-2.0.1.logV0.hlog", 45, 34, 34, 25439, 1209008127, 1234173951, 1438869961.225)]
        public void CanReadv0Logs(string logPath, int skip, int take,
            int expectedHistogramCount, int expectedCombinedValueCount,
            int expectedCombined999, long expectedCombinedMaxLength,
            double expectedStartTime)
        {
            var readerStream = GetEmbeddedFileStream(logPath);
            var reader = new HistogramLogReader(readerStream);

            int histogramCount = 0;
            long totalCount = 0;
            var accumulatedHistogram = new LongHistogram(3600L * 1000 * 1000 * 1000, 3);
            var histograms = ((IHistogramLogV1Reader)reader).ReadHistograms()
                .Skip(skip)
                .Take(take);
            foreach (var histogram in histograms)
            {
                histogramCount++;
                totalCount += histogram.TotalCount;
                accumulatedHistogram.Add(histogram);
            }
            Assert.AreEqual(expectedHistogramCount, histogramCount);
            Assert.AreEqual(expectedCombinedValueCount, totalCount);
            Assert.AreEqual(expectedCombined999, accumulatedHistogram.GetValueAtPercentile(99.9));
            Assert.AreEqual(expectedCombinedMaxLength, accumulatedHistogram.GetMaxValue());
            Assert.AreEqual(expectedStartTime, reader.GetStartTime().SecondsSinceUnixEpoch());
        }

        [TestCase("jHiccup-2.0.6.logV1.hlog", 0, int.MaxValue, 88, 65964, 1829765119, 1888485375, 1438867590.285)]
        [TestCase("jHiccup-2.0.6.logV1.hlog", 5, 15, 15, 11213, 1019740159, 1032323071, 1438867590.285)]
        [TestCase("jHiccup-2.0.6.logV1.hlog", 50, 29, 29, 22630, 1871708159, 1888485375, 1438867590.285)]
        [TestCase("ycsb.logV1.hlog", 0, int.MaxValue, 602, 300056, 1214463, 1546239, 1438613579.295)]
        [TestCase("ycsb.logV1.hlog", 0, 180, 180, 89893, 1375231, 1546239, 1438613579.295)]
        [TestCase("ycsb.logV1.hlog", 180, 520, 422, 210163, 530, 17775, 1438613579.295)]
        public void CanReadv1Logs(string logPath, int skip, int take,
            int expectedHistogramCount, int expectedCombinedValueCount,
            int expectedCombined999, long expectedCombinedMaxLength,
            double expectedStartTime)
        {
            var readerStream = GetEmbeddedFileStream(logPath);
            var reader = new HistogramLogReader(readerStream);
            int histogramCount = 0;
            long totalCount = 0;

            HistogramBase accumulatedHistogram = new LongHistogram(3600L * 1000 * 1000 * 1000, 3);
            var histograms = reader.ReadHistograms()
                .Skip(skip)
                .Take(take);
            foreach (var histogram in histograms)
            {
                histogramCount++;
                totalCount += histogram.TotalCount;
                accumulatedHistogram.Add(histogram);
            }

            Assert.AreEqual(expectedHistogramCount, histogramCount);
            Assert.AreEqual(expectedCombinedValueCount, totalCount);
            Assert.AreEqual(expectedCombined999, accumulatedHistogram.GetValueAtPercentile(99.9));
            Assert.AreEqual(expectedCombinedMaxLength, accumulatedHistogram.GetMaxValue());
            Assert.AreEqual(expectedStartTime, reader.GetStartTime().SecondsSinceUnixEpoch());
        }

        [TestCase("ycsb.logV1.hlog", 0, 180, 180, 90033, 1375231, 1546239, 1438613579.295)]
        [TestCase("ycsb.logV1.hlog", 180, 520, 421, 209686, 530, 17775, 1438613579.295)]
        public void CanReadv1Logs_Skip_PreStart(string logPath, int skip, int take,
            int expectedHistogramCount, int expectedCombinedValueCount,
            int expectedCombined999, long expectedCombinedMaxLength,
            double expectedStartTime)
        {
            var readerStream = GetEmbeddedFileStream(logPath);
            var reader = new HistogramLogReader(readerStream);
            int histogramCount = 0;
            long totalCount = 0;

            HistogramBase accumulatedHistogram = new LongHistogram(3600L * 1000 * 1000 * 1000, 3);
            var histograms = reader.ReadHistograms()
                .Where(h => h.StartTimeStamp >= reader.GetStartTime().MillisecondsSinceUnixEpoch())
                .Skip(skip)
                .Take(take);
            foreach (var histogram in histograms)
            {
                histogramCount++;
                totalCount += histogram.TotalCount;
                accumulatedHistogram.Add(histogram);
            }

            Assert.AreEqual(expectedHistogramCount, histogramCount);
            Assert.AreEqual(expectedCombinedValueCount, totalCount);
            Assert.AreEqual(expectedCombined999, accumulatedHistogram.GetValueAtPercentile(99.9));
            Assert.AreEqual(expectedCombinedMaxLength, accumulatedHistogram.GetMaxValue());
            Assert.AreEqual(expectedStartTime, reader.GetStartTime().SecondsSinceUnixEpoch());
        }



        private static HistogramBase[] ReadHistograms(byte[] data)
        {
            HistogramBase[] actualHistograms;
            using (var readerStream = new MemoryStream(data))
            {
                actualHistograms = Histogram.Read(readerStream).ToArray();
            }
            return actualHistograms;
        }

        private static byte[] WriteLog(DateTime startTimeWritten, LongHistogram histogram)
        {
            byte[] data;
            using (var writerStream = new MemoryStream())
            {
                Histogram.Write(writerStream, startTimeWritten, histogram);
                data = writerStream.ToArray();
            }
            return data;
        }

        private static LongHistogram CreatePopulatedHistogram(long highestTrackableValue, int significantDigits, int multiplier)
        {
            var histogram = new LongHistogram(highestTrackableValue, significantDigits);
            //Ensure reasonable number of counts
            long i;
            for (i = 0; i < 10000; i++)
            {
                histogram.RecordValue(i * multiplier);
            }
            //Ensure values recorded across the full range of buckets.
            i = 1;
            do
            {
                histogram.RecordValue(i);
            } while ((i <<= 1) < highestTrackableValue && i > 0);
            return histogram;
        }

        private Stream GetEmbeddedFileStream(string filename)
        {
            var fileName = string.Format(CultureInfo.InvariantCulture, "HdrHistogram.UnitTests.Resources.{0}", filename);
            return GetType()
                .Assembly
                .GetManifestResourceStream(fileName);
        }

        private static IEnumerable<long> PowersOfTwo()
        {
            for (int i = 0; i < 63; i++)
            {
                yield return (1L << i);
            }
        }
    }
}