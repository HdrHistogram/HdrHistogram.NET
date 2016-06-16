using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    public abstract class HistogramTestBase
    {
        private const long HighestTrackableValue = 7716549600;//TimeStamp.Hours(1); // e.g. for 1 hr in system clock ticks (StopWatch.Frequency)
        private const int NumberOfSignificantValueDigits = 3;
        private const long TestValueLevel = 4;

        private static readonly IDictionary<int, Func<long, long, int, HistogramBase>> WordSizeToFactory =
            new Dictionary<int, Func<long, long, int, HistogramBase>>()
            {
                { 2, (low, high, sf) => new ShortHistogram(low, high, sf) },
                { 4, (low,high,sf) => new IntHistogram(low, high, sf) },
                { 8, (low,high,sf) => new LongHistogram(low, high, sf) }
            };

        [TestCase(0, 1, NumberOfSignificantValueDigits, "lowestTrackableValue", "lowestTrackableValue must be >= 1")]
        [TestCase(1, 1, NumberOfSignificantValueDigits, "highestTrackableValue", "highestTrackableValue must be >= 2 * lowestTrackableValue")]
        [TestCase(1, HighestTrackableValue, 6, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        [TestCase(1, HighestTrackableValue, -1, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        public void ConstructorShouldRejectInvalidParameters(
            long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits,
            string errorParamName, string errorMessage)
        {
            var ex = Assert.Throws<ArgumentException>(() => { Create(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits); });
            Assert.AreEqual(errorParamName, ex.ParamName);
            StringAssert.StartsWith(errorMessage, ex.Message);
        }


        [TestCase(2, 2)]
        [TestCase(HighestTrackableValue, NumberOfSignificantValueDigits)]
        public void TestConstructionArgumentGets(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            var longHistogram = Create(highestTrackableValue, numberOfSignificantValueDigits);
            Assert.AreEqual(1, longHistogram.LowestTrackableValue);
            Assert.AreEqual(highestTrackableValue, longHistogram.HighestTrackableValue);
            Assert.AreEqual(numberOfSignificantValueDigits, longHistogram.NumberOfSignificantValueDigits);
        }

        [TestCase(1, 2, 2)]
        [TestCase(10, HighestTrackableValue, NumberOfSignificantValueDigits)]
        public void TestConstructionArgumentGets(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            var longHistogram = Create(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
            Assert.AreEqual(lowestTrackableValue, longHistogram.LowestTrackableValue);
            Assert.AreEqual(highestTrackableValue, longHistogram.HighestTrackableValue);
            Assert.AreEqual(numberOfSignificantValueDigits, longHistogram.NumberOfSignificantValueDigits);
        }

        [Test]
        public void TestGetEstimatedFootprintInBytes2()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            var largestValueWithSingleUnitResolution = 2 * (long)Math.Pow(10, NumberOfSignificantValueDigits);
            var subBucketCountMagnitude = (int)Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution) / Math.Log(2));
            var subBucketSize = (int)Math.Pow(2, (subBucketCountMagnitude));
            var bucketCount = GetBucketsNeededToCoverValue(subBucketSize, HighestTrackableValue);

            var header = 512;
            var width = WordSize;
            var length = (bucketCount + 1) * (subBucketSize / 2);
            var expectedSize = header + (width * length);

            Assert.AreEqual(expectedSize, longHistogram.GetEstimatedFootprintInBytes());
        }


        [Test]
        public void RecordValue_increments_TotalCount()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            for (int i = 1; i < 5; i++)
            {
                longHistogram.RecordValue(i);
                Assert.AreEqual(i, longHistogram.TotalCount);
            }
        }

        [Test]
        public void RecordValue_increments_CountAtValue()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            for (int i = 1; i < 5; i++)
            {
                longHistogram.RecordValue(TestValueLevel);
                Assert.AreEqual(i, longHistogram.GetCountAtValue(TestValueLevel));
            }
        }

        [Test]
        public void RecordValue_Overflow_ShouldThrowException()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.Throws<IndexOutOfRangeException>(() => longHistogram.RecordValue(HighestTrackableValue * 3));
        }

        [TestCase(5)]
        [TestCase(100)]
        public void RecordValueWithCount_increments_TotalCount(long multiplier)
        {
            var histogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            for (int i = 1; i < 5; i++)
            {
                histogram.RecordValueWithCount(i, multiplier);
                Assert.AreEqual(i * multiplier, histogram.TotalCount);
            }
        }

        [TestCase(5)]
        [TestCase(100)]
        public void RecordValueWithCount_increments_CountAtValue(long multiplier)
        {
            var histogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            for (int i = 1; i < 5; i++)
            {
                histogram.RecordValueWithCount(TestValueLevel, multiplier);
                Assert.AreEqual(i * multiplier, histogram.GetCountAtValue(TestValueLevel));
            }
        }

        [Test]
        public void RecordValueWithCount_Overflow_ShouldThrowException()
        {
            var histogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.Throws<IndexOutOfRangeException>(() => histogram.RecordValueWithCount(HighestTrackableValue * 3, 10));
        }


        [Test]
        public void RecordValueWithExpectedInterval()
        {
            var intervalHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            var valueHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);

            intervalHistogram.RecordValueWithExpectedInterval(TestValueLevel, TestValueLevel / 4);
            valueHistogram.RecordValue(TestValueLevel);

            // The data will include corrected samples:
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 1) / 4));
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 2) / 4));
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 3) / 4));
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 4) / 4));
            Assert.AreEqual(4L, intervalHistogram.TotalCount);
            // But the raw data will not:
            Assert.AreEqual(0L, valueHistogram.GetCountAtValue((TestValueLevel * 1) / 4));
            Assert.AreEqual(0L, valueHistogram.GetCountAtValue((TestValueLevel * 2) / 4));
            Assert.AreEqual(0L, valueHistogram.GetCountAtValue((TestValueLevel * 3) / 4));
            Assert.AreEqual(1L, valueHistogram.GetCountAtValue((TestValueLevel * 4) / 4));
            Assert.AreEqual(1L, valueHistogram.TotalCount);
        }

        [Test]
        public void RecordAction_increments_TotalCount()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);

            longHistogram.Record(() => { });
            Assert.AreEqual(1, longHistogram.TotalCount);
        }

        [Test]
        public void RecordAction_records_in_correct_units()
        {
            var pause = TimeSpan.FromSeconds(1);
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);

            longHistogram.Record(() => Thread.Sleep(pause));

            var stringWriter = new StringWriter();
            longHistogram.OutputPercentileDistribution(stringWriter, 5, OutputScalingFactor.TimeStampToMilliseconds, true);
            //First column of second row.
            var recordedMilliseconds = GetCellValue(stringWriter.ToString(), 0, 1);
            var actual = double.Parse(recordedMilliseconds);
            var expected = pause.TotalMilliseconds;
            var delta = expected * 0.1;   //10% Variance to allow for slack in transitioning from Thread.Sleep
            Assert.AreEqual(expected, actual, delta);
        }

        [Test]
        public void Reset_sets_counts_to_zero()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            longHistogram.RecordValue(TestValueLevel);

            longHistogram.Reset();

            Assert.AreEqual(0L, longHistogram.GetCountAtValue(TestValueLevel));
            Assert.AreEqual(0L, longHistogram.TotalCount);
        }


        [Test]
        public void Add_should_sum_the_counts_from_two_histograms()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            var other = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            longHistogram.RecordValue(TestValueLevel);
            longHistogram.RecordValue(TestValueLevel * 1000);
            other.RecordValue(TestValueLevel);
            other.RecordValue(TestValueLevel * 1000);

            longHistogram.Add(other);

            Assert.AreEqual(2L, longHistogram.GetCountAtValue(TestValueLevel));
            Assert.AreEqual(2L, longHistogram.GetCountAtValue(TestValueLevel * 1000));
            Assert.AreEqual(4L, longHistogram.TotalCount);
        }

        [Test]
        public void Add_should_allow_small_range_hsitograms_to_be_added()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);

            longHistogram.RecordValue(TestValueLevel);
            longHistogram.RecordValue(TestValueLevel * 1000);

            var biggerOther = Create(HighestTrackableValue * 2, NumberOfSignificantValueDigits);
            biggerOther.RecordValue(TestValueLevel);
            biggerOther.RecordValue(TestValueLevel * 1000);

            // Adding the smaller histogram to the bigger one should work:
            biggerOther.Add(longHistogram);
            Assert.AreEqual(2L, biggerOther.GetCountAtValue(TestValueLevel));
            Assert.AreEqual(2L, biggerOther.GetCountAtValue(TestValueLevel * 1000));
            Assert.AreEqual(4L, biggerOther.TotalCount);
        }

        [Test]
        public void Add_throws_if_other_has_a_larger_range()
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            var biggerOther = Create(HighestTrackableValue * 2, NumberOfSignificantValueDigits);

            Assert.Throws<ArgumentOutOfRangeException>(() => { longHistogram.Add(biggerOther); });
        }

        [TestCase(1, 1)]
        [TestCase(2, 2500)]
        [TestCase(4, 8191)]
        [TestCase(8, 8192)]
        [TestCase(8, 10000)]
        public void SizeOfEquivalentValueRangeForValue(int expected, int value)
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected, longHistogram.SizeOfEquivalentValueRange(value));
            //Validate the scaling too.

            var scaledHistogram = Create(1024, HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected * 1024, scaledHistogram.SizeOfEquivalentValueRange(value * 1024));
        }

        [TestCase(10000, 10007)]
        [TestCase(10008, 10009)]
        public void LowestEquivalentValue_returns_the_smallest_value_that_would_be_assigned_to_the_same_count(int expected, int value)
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected, longHistogram.LowestEquivalentValue(value));
            //Validate the scaling too
            var scaledHistogram = Create(1024, HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected * 1024, scaledHistogram.LowestEquivalentValue(value * 1024));
        }

        [TestCase(8183, 8180)]
        [TestCase(8191, 8191)]
        [TestCase(8199, 8193)]
        [TestCase(9999, 9995)]
        [TestCase(10007, 10007)]
        [TestCase(10015, 10008)]
        public void HighestEquivalentValue_returns_the_smallest_value_that_would_be_assigned_to_the_same_count(int expected, int value)
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected, longHistogram.HighestEquivalentValue(value));
            //Validate the scaling too
            var scaledHistogram = Create(1024, HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected * 1024 + 1023, scaledHistogram.HighestEquivalentValue(value * 1024));
        }

        [TestCase(4, 4, 512)]
        [TestCase(5, 5, 512)]
        [TestCase(4001, 4000, 0)]
        [TestCase(8002, 8000, 0)]
        [TestCase(10004, 10007, 0)]
        public void TestMedianEquivalentValue(int expected, int value, int scaledHeader)
        {
            var longHistogram = Create(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected, longHistogram.MedianEquivalentValue(value));
            //Validate the scaling too
            var scaledHistogram = Create(1024, HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(expected * 1024 + scaledHeader, scaledHistogram.MedianEquivalentValue(value * 1024));
        }


        [Test]
        public void When_more_items_are_recorded_than_totalCount_can_hold_Then_set_HasOverflowed_to_True()
        {
            var histogram = Create(HighestTrackableValue, 2);
            Assert.False(histogram.HasOverflowed());

            histogram.RecordValueWithCount(TestValueLevel, long.MaxValue);
            histogram.RecordValueWithCount(TestValueLevel * 1024, long.MaxValue);

            Assert.True(histogram.HasOverflowed());
        }

        [Test]
        public void Can_add_Histograms_with_larger_wordSize_when_values_are_in_range()
        {
            var largerHistogramFactory = WordSizeToFactory.Where(kvp => kvp.Key >= WordSize).Select(kvp => kvp.Value);
            foreach (var sourceFactory in largerHistogramFactory)
            {
                CreateAndAdd(sourceFactory(1, HighestTrackableValue, NumberOfSignificantValueDigits));
            }
        }

        [Test]
        public void Copy_retains_all_public_properties()
        {
            var source = Create(1, HighestTrackableValue, NumberOfSignificantValueDigits);
            var copy = source.Copy();
            HistogramAssert.AreValueEqual(source, copy);
        }

        private void CreateAndAdd(HistogramBase source)
        {
            source.RecordValueWithCount(1, 100);
            source.RecordValueWithCount(int.MaxValue - 1, 1000);

            var target = Create(source.LowestTrackableValue, source.HighestTrackableValue, source.NumberOfSignificantValueDigits);
            target.Add(source);

            HistogramAssert.AreValueEqual(source, target);
        }

        private static int GetBucketsNeededToCoverValue(int subBucketSize, long value)
        {
            long trackableValue = (subBucketSize - 1);// << _unitMagnitude;
            int bucketsNeeded = 1;
            while (trackableValue < value)
            {
                trackableValue <<= 1;
                bucketsNeeded++;
            }
            return bucketsNeeded;
        }
        protected abstract int WordSize { get; }
        protected abstract HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits);
        protected abstract HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits);

        private static string GetCellValue(string csvData, int col, int row)
        {
            return csvData.Split('\n')[row].Split(',')[col];
        }
    }
}