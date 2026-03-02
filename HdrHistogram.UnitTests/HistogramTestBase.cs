using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace HdrHistogram.UnitTests
{
    public abstract class HistogramTestBase
    {
        protected const long DefautltLowestDiscernibleValue = 1;
        protected const long DefaultHighestTrackableValue = 7716549600;//TimeStamp.Hours(1); // e.g. for 1 hr in system clock ticks (StopWatch.Frequency)
        protected const int DefaultSignificantFigures = 3;
        protected const long TestValueLevel = 4;

        private static readonly IDictionary<int, Func<long, long, int, HistogramBase>> WordSizeToFactory =
            new Dictionary<int, Func<long, long, int, HistogramBase>>()
            {
                { 2, (low, high, sf) => new ShortHistogram(low, high, sf) },
                { 4, (low,high,sf) => new IntHistogram(low, high, sf) },
                { 8, (low,high,sf) => new LongHistogram(low, high, sf) }
            };

        protected abstract int WordSize { get; }
        protected abstract HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits);
        protected abstract HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits);

        [Theory]
        [InlineData(0, 1, DefaultSignificantFigures, "lowestTrackableValue", "lowestTrackableValue must be >= 1")]
        [InlineData(DefautltLowestDiscernibleValue, 1, DefaultSignificantFigures, "highestTrackableValue", "highestTrackableValue must be >= 2 * lowestTrackableValue")]
        [InlineData(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, 6, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        [InlineData(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, -1, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        public void ConstructorShouldRejectInvalidParameters(
            long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits,
            string errorParamName, string errorMessage)
        {
            var ex = Assert.Throws<ArgumentException>(() => { Create(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits); });
            Assert.Equal(errorParamName, ex.ParamName);
            Assert.StartsWith(errorMessage, ex.Message);
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(DefaultHighestTrackableValue, DefaultSignificantFigures)]
        public void Test2ConstructionArgumentGets(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            var histogram = Create(highestTrackableValue, numberOfSignificantValueDigits);
            Assert.Equal(1, histogram.LowestTrackableValue);
            Assert.Equal(highestTrackableValue, histogram.HighestTrackableValue);
            Assert.Equal(numberOfSignificantValueDigits, histogram.NumberOfSignificantValueDigits);
        }

        [Theory]
        [InlineData(1, 2, 2)]
        [InlineData(10, DefaultHighestTrackableValue, DefaultSignificantFigures)]
        public void Test3ConstructionArgumentGets(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            var histogram = Create(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
            Assert.Equal(lowestTrackableValue, histogram.LowestTrackableValue);
            Assert.Equal(highestTrackableValue, histogram.HighestTrackableValue);
            Assert.Equal(numberOfSignificantValueDigits, histogram.NumberOfSignificantValueDigits);
        }

        [Fact]
        public void TestGetEstimatedFootprintInBytes2()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            var largestValueWithSingleUnitResolution = 2 * (long)Math.Pow(10, DefaultSignificantFigures);
            var subBucketCountMagnitude = (int)Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution) / Math.Log(2));
            var subBucketSize = (int)Math.Pow(2, (subBucketCountMagnitude));
            var bucketCount = GetBucketsNeededToCoverValue(subBucketSize, DefaultHighestTrackableValue);

            var header = 512;
            var width = WordSize;
            var length = (bucketCount + 1) * (subBucketSize / 2);
            var expectedSize = header + (width * length);

            Assert.Equal(expectedSize, histogram.GetEstimatedFootprintInBytes());
        }


        [Fact]
        public void RecordValue_increments_TotalCount()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            for (int i = 1; i < 5; i++)
            {
                histogram.RecordValue(i);
                Assert.Equal(i, histogram.TotalCount);
            }
        }

        [Fact]
        public void RecordValue_increments_CountAtValue()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            for (int i = 1; i < 5; i++)
            {
                histogram.RecordValue(TestValueLevel);
                Assert.Equal(i, histogram.GetCountAtValue(TestValueLevel));
            }
        }

        [Fact]
        public void RecordValue_Overflow_ShouldThrowException()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Throws<IndexOutOfRangeException>(() => histogram.RecordValue(DefaultHighestTrackableValue * 3));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(100)]
        public void RecordValueWithCount_increments_TotalCount(long multiplier)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            for (int i = 1; i < 5; i++)
            {
                histogram.RecordValueWithCount(i, multiplier);
                Assert.Equal(i * multiplier, histogram.TotalCount);
            }
        }

        [Theory]
        [InlineData(5)]
        [InlineData(100)]
        public void RecordValueWithCount_increments_CountAtValue(long multiplier)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            for (int i = 1; i < 5; i++)
            {
                histogram.RecordValueWithCount(TestValueLevel, multiplier);
                Assert.Equal(i * multiplier, histogram.GetCountAtValue(TestValueLevel));
            }
        }

        [Fact]
        public void RecordValueWithCount_Overflow_ShouldThrowException()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Throws<IndexOutOfRangeException>(() => histogram.RecordValueWithCount(DefaultHighestTrackableValue * 3, 10));
        }


        [Fact]
        public void RecordValueWithExpectedInterval()
        {
            var intervalHistogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            var valueHistogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);

            intervalHistogram.RecordValueWithExpectedInterval(TestValueLevel, TestValueLevel / 4);
            valueHistogram.RecordValue(TestValueLevel);

            // The data will include corrected samples:
            Assert.Equal(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 1) / 4));
            Assert.Equal(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 2) / 4));
            Assert.Equal(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 3) / 4));
            Assert.Equal(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 4) / 4));
            Assert.Equal(4L, intervalHistogram.TotalCount);
            // But the raw data will not:
            Assert.Equal(0L, valueHistogram.GetCountAtValue((TestValueLevel * 1) / 4));
            Assert.Equal(0L, valueHistogram.GetCountAtValue((TestValueLevel * 2) / 4));
            Assert.Equal(0L, valueHistogram.GetCountAtValue((TestValueLevel * 3) / 4));
            Assert.Equal(1L, valueHistogram.GetCountAtValue((TestValueLevel * 4) / 4));
            Assert.Equal(1L, valueHistogram.TotalCount);
        }

        [Fact]
        public void Record_increments_TotalCount()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);

            histogram.Record(() => { });
            Assert.Equal(1, histogram.TotalCount);
        }

        [Fact]
        public void Record_records_in_correct_units()
        {
            var pause = TimeSpan.FromSeconds(1);
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);

            histogram.Record(() =>
            {
                Spin.Wait(TimeSpan.FromSeconds(1));
            });

            var stringWriter = new StringWriter();
            histogram.OutputPercentileDistribution(stringWriter,
                percentileTicksPerHalfDistance: 5,
                outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds,
                useCsvFormat: true);

            //First column of second row.
            var recordedMilliseconds = GetCellValue(stringWriter.ToString(), 0, 1);
            var actual = double.Parse(recordedMilliseconds);
            var expected = pause.TotalMilliseconds;

            //10% Variance to allow for slack in transitioning from Thread.Sleep
            actual.Should().BeInRange(expected * 0.9, expected * 1.1);
        }

        [Fact]
        public void Reset_sets_counts_to_zero()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(TestValueLevel);

            histogram.Reset();

            Assert.Equal(0L, histogram.GetCountAtValue(TestValueLevel));
            Assert.Equal(0L, histogram.TotalCount);
        }


        [Fact]
        public void Add_should_sum_the_counts_from_two_histograms()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            var other = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(TestValueLevel);
            histogram.RecordValue(TestValueLevel * 1000);
            other.RecordValue(TestValueLevel);
            other.RecordValue(TestValueLevel * 1000);

            histogram.Add(other);

            Assert.Equal(2L, histogram.GetCountAtValue(TestValueLevel));
            Assert.Equal(2L, histogram.GetCountAtValue(TestValueLevel * 1000));
            Assert.Equal(4L, histogram.TotalCount);
        }

        [Fact]
        public void Add_should_allow_small_range_hsitograms_to_be_added()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);

            histogram.RecordValue(TestValueLevel);
            histogram.RecordValue(TestValueLevel * 1000);

            var biggerOther = Create(DefaultHighestTrackableValue * 2, DefaultSignificantFigures);
            biggerOther.RecordValue(TestValueLevel);
            biggerOther.RecordValue(TestValueLevel * 1000);

            // Adding the smaller histogram to the bigger one should work:
            biggerOther.Add(histogram);
            Assert.Equal(2L, biggerOther.GetCountAtValue(TestValueLevel));
            Assert.Equal(2L, biggerOther.GetCountAtValue(TestValueLevel * 1000));
            Assert.Equal(4L, biggerOther.TotalCount);
        }

        [Fact]
        public void Add_throws_if_other_has_a_larger_range()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            var biggerOther = Create(DefaultHighestTrackableValue * 2, DefaultSignificantFigures);

            Assert.Throws<ArgumentOutOfRangeException>(() => { histogram.Add(biggerOther); });
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2500)]
        [InlineData(4, 8191)]
        [InlineData(8, 8192)]
        [InlineData(8, 10000)]
        public void SizeOfEquivalentValueRangeForValue(int expected, int value)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected, histogram.SizeOfEquivalentValueRange(value));
            //Validate the scaling too.

            var scaledHistogram = Create(1024, DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected * 1024, scaledHistogram.SizeOfEquivalentValueRange(value * 1024));
        }

        [Theory]
        [InlineData(10000, 10007)]
        [InlineData(10008, 10009)]
        public void LowestEquivalentValue_returns_the_smallest_value_that_would_be_assigned_to_the_same_count(int expected, int value)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected, histogram.LowestEquivalentValue(value));
            //Validate the scaling too
            var scaledHistogram = Create(1024, DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected * 1024, scaledHistogram.LowestEquivalentValue(value * 1024));
        }

        [Theory]
        [InlineData(8183, 8180)]
        [InlineData(8191, 8191)]
        [InlineData(8199, 8193)]
        [InlineData(9999, 9995)]
        [InlineData(10007, 10007)]
        [InlineData(10015, 10008)]
        public void HighestEquivalentValue_returns_the_smallest_value_that_would_be_assigned_to_the_same_count(int expected, int value)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected, histogram.HighestEquivalentValue(value));
            //Validate the scaling too
            var scaledHistogram = Create(1024, DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected * 1024 + 1023, scaledHistogram.HighestEquivalentValue(value * 1024));
        }

        [Theory]
        [InlineData(4, 4, 512)]
        [InlineData(5, 5, 512)]
        [InlineData(4001, 4000, 0)]
        [InlineData(8002, 8000, 0)]
        [InlineData(10004, 10007, 0)]
        public void TestMedianEquivalentValue(int expected, int value, int scaledHeader)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected, histogram.MedianEquivalentValue(value));
            //Validate the scaling too
            var scaledHistogram = Create(1024, DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Equal(expected * 1024 + scaledHeader, scaledHistogram.MedianEquivalentValue(value * 1024));
        }


        [Fact]
        public void When_more_items_are_recorded_than_totalCount_can_hold_Then_set_HasOverflowed_to_True()
        {
            var histogram = Create(DefaultHighestTrackableValue, 2);
            Assert.False(histogram.HasOverflowed());

            histogram.RecordValueWithCount(TestValueLevel, long.MaxValue);
            histogram.RecordValueWithCount(TestValueLevel * 1024, long.MaxValue);

            Assert.True(histogram.HasOverflowed());
        }

        [Fact]
        public void Can_add_Histograms_with_larger_wordSize_when_values_are_in_range()
        {
            var largerHistogramFactory = WordSizeToFactory.Where(kvp => kvp.Key >= WordSize).Select(kvp => kvp.Value);
            foreach (var sourceFactory in largerHistogramFactory)
            {
                CreateAndAdd(sourceFactory(1, DefaultHighestTrackableValue, DefaultSignificantFigures));
            }
        }

        [Fact]
        public void Copy_retains_all_public_properties()
        {
            var source = Create(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures);
            var copy = source.Copy();
            HistogramAssert.AreValueEqual(source, copy);
        }

        [Theory]
        [InlineData("No spaces")]
        [InlineData("No,commas")]
        [InlineData("\r")]
        [InlineData("\n")]
        [InlineData(" ")]
        public void Setting_invalid_value_to_Tag_throws(string invalidTagValue)
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            Assert.Throws<ArgumentException>(() => histogram.Tag = invalidTagValue);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_EmptyHistogram_ReturnsZero()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.GetPercentileAtOrBelowValue(TestValueLevel).Should().Be(0.0);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_ValueAtOrAboveHighestTrackable_Returns100()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(TestValueLevel);
            histogram.GetPercentileAtOrBelowValue(DefaultHighestTrackableValue).Should().Be(100.0);
            histogram.GetPercentileAtOrBelowValue(long.MaxValue / 2).Should().Be(100.0);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_KnownValueSet_ReturnsExpectedPercentile()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            for (long i = 1; i <= 100; i++)
            {
                histogram.RecordValue(i);
            }
            histogram.GetPercentileAtOrBelowValue(50).Should().BeApproximately(50.0, 0.1);
            histogram.GetPercentileAtOrBelowValue(100).Should().BeApproximately(100.0, 0.1);
            histogram.GetPercentileAtOrBelowValue(1).Should().BeApproximately(1.0, 0.1);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_ResultIsAlwaysInRange()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            for (long i = 1; i <= 10; i++)
            {
                histogram.RecordValue(i * 100);
            }
            histogram.GetPercentileAtOrBelowValue(0).Should().Be(0.0);
            foreach (var queryValue in new long[] { 0, 100, 500, DefaultHighestTrackableValue })
            {
                histogram.GetPercentileAtOrBelowValue(queryValue).Should().BeInRange(0.0, 100.0);
            }
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_RoundTrip_ConsistentWithGetValueAtPercentile()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            foreach (var v in new long[] { 1, 10, 100, 1000 })
            {
                histogram.RecordValue(v);
            }
            foreach (var v in new long[] { 1, 10, 100, 1000 })
            {
                var p = histogram.GetPercentileAtOrBelowValue(v);
                var valueAtP = histogram.GetValueAtPercentile(p);
                var expected = histogram.HighestEquivalentValue(histogram.LowestEquivalentValue(v));
                valueAtP.Should().Be(expected);
            }
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_IsMonotonic()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            foreach (var v in new long[] { 1, 50, 100, 500, 1000 })
            {
                histogram.RecordValue(v);
            }
            var queryValues = new long[] { 1, 10, 50, 100, 500, 1000, 5000 };
            double previous = 0.0;
            foreach (var q in queryValues)
            {
                var current = histogram.GetPercentileAtOrBelowValue(q);
                current.Should().BeGreaterThanOrEqualTo(previous);
                previous = current;
            }
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_SingleRecordedValue_QueriesBelow_Return0()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(TestValueLevel);
            histogram.GetPercentileAtOrBelowValue(TestValueLevel - 1).Should().Be(0.0);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_SingleRecordedValue_QueriesAtOrAbove_Return100()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(TestValueLevel);
            histogram.GetPercentileAtOrBelowValue(TestValueLevel).Should().Be(100.0);
            histogram.GetPercentileAtOrBelowValue(TestValueLevel + 1000).Should().Be(100.0);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_BoundaryAtLowestTrackableValue()
        {
            var histogram = Create(1, DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(1);
            histogram.RecordValue(1000);
            histogram.GetPercentileAtOrBelowValue(1).Should().BeApproximately(50.0, 0.1);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_LargeMagnitudeValues_ReturnsCorrectPercentile()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(1000);
            histogram.RecordValue(1_000_000);
            // 1000 is 50% of the recorded values
            histogram.GetPercentileAtOrBelowValue(1000).Should().BeApproximately(50.0, 0.1);
            // 1_000_000 is 100% of the recorded values
            histogram.GetPercentileAtOrBelowValue(1_000_000).Should().BeApproximately(100.0, 0.1);
            // Something between 1000 and 1_000_000 is still only 50%
            histogram.GetPercentileAtOrBelowValue(500_000).Should().BeApproximately(50.0, 0.1);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_ZeroValue_WithPositiveRecordedValues_ReturnsZero()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(100);
            histogram.RecordValue(1000);
            // No recorded values are <= 0, so should return 0.0
            histogram.GetPercentileAtOrBelowValue(0).Should().Be(0.0);
        }

        [Fact]
        public void GetPercentileAtOrBelowValue_NegativeValue_ReturnsZero()
        {
            var histogram = Create(DefaultHighestTrackableValue, DefaultSignificantFigures);
            histogram.RecordValue(100);
            histogram.RecordValue(1000);
            // Negative values are below any trackable range, so should return 0.0
            histogram.GetPercentileAtOrBelowValue(-1).Should().Be(0.0);
            histogram.GetPercentileAtOrBelowValue(long.MinValue).Should().Be(0.0);
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

        private static string GetCellValue(string csvData, int col, int row)
        {
            return csvData.Split('\n')[row].Split(',')[col];
        }
    }
}
