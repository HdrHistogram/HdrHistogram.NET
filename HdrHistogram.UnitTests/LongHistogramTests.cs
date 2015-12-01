using System;
using Xunit;

namespace HdrHistogram.UnitTests
{
    
    public class LongHistogramTests
    {
        private const long HighestTrackableValue = TimeSpan.TicksPerHour; // e.g. for 1 hr in ticks
        private const int NumberOfSignificantValueDigits = 3;
        private const long TestValueLevel = 4;

        [Theory]
        [InlineData(0, 1, NumberOfSignificantValueDigits, "lowestTrackableValue", "lowestTrackableValue must be >= 1")]
        [InlineData(1, 1, NumberOfSignificantValueDigits, "highestTrackableValue", "highestTrackableValue must be >= 2 * lowestTrackableValue")]
        [InlineData(1, HighestTrackableValue, 6, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        [InlineData(1, HighestTrackableValue, -1, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        public void ConstructorShouldRejectInvalidParameters(
            long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits,
            string errorParamName, string errorMessage)
        {
            var ex = Assert.Throws<ArgumentException>(() => { new LongHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits); });
            Assert.Equal(errorParamName, ex.ParamName);
            Assert.StartsWith(errorMessage, ex.Message);
        }


        [Theory]
        [InlineData(2, 2)]
        [InlineData(HighestTrackableValue, NumberOfSignificantValueDigits)]
        public void TestConstructionArgumentGets(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            var longHistogram = new LongHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            Assert.Equal(1, longHistogram.LowestTrackableValue);
            Assert.Equal(highestTrackableValue, longHistogram.HighestTrackableValue);
            Assert.Equal(numberOfSignificantValueDigits, longHistogram.NumberOfSignificantValueDigits);
        }

        [Theory]
        [InlineData(1, 2, 2)]
        [InlineData(10, HighestTrackableValue, NumberOfSignificantValueDigits)]
        public void TestConstructionArgumentGets(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            var longHistogram = new LongHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
            Assert.Equal(lowestTrackableValue, longHistogram.LowestTrackableValue);
            Assert.Equal(highestTrackableValue, longHistogram.HighestTrackableValue);
            Assert.Equal(numberOfSignificantValueDigits, longHistogram.NumberOfSignificantValueDigits);
        }

        [Fact]
        public void TestGetEstimatedFootprintInBytes2()
        {
            var longHistogram = new LongHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            var largestValueWithSingleUnitResolution = 2 * (long)Math.Pow(10, NumberOfSignificantValueDigits);
            var subBucketCountMagnitude = (int)Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution) / Math.Log(2));
            var subBucketSize = (int)Math.Pow(2, (subBucketCountMagnitude));
            var bucketCount = GetBucketsNeededToCoverValue(subBucketSize, HighestTrackableValue);

            var header = 512;
            var width = sizeof(long);
            var length = (bucketCount + 1) * (subBucketSize / 2);
            var expectedSize = header + (width * length);

            Assert.Equal(expectedSize, longHistogram.GetEstimatedFootprintInBytes());
        }


        [Fact]
        public void RecordValue_increments_TotalCount()
        {
            var longHistogram = new LongHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            for (int i = 1; i < 5; i++)
            {
                longHistogram.RecordValue(i);
                Assert.Equal(i, longHistogram.TotalCount);
            }
        }

        [Fact]
        public void RecordValue_increments_CountAtValue()
        {
            var longHistogram = new LongHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            for (int i = 1; i < 5; i++)
            {
                longHistogram.RecordValue(TestValueLevel);
                Assert.Equal(i, longHistogram.GetCountAtValue(TestValueLevel));
            }
        }

        [Fact]
        public void RecordValue_Overflow_ShouldThrowException()
        {
            var longHistogram = new LongHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.Throws<IndexOutOfRangeException>(() => longHistogram.RecordValue(HighestTrackableValue * 3));
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

    }
}
