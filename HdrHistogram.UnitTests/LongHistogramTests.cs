using System;
using Xunit;
using FluentAssertions;

namespace HdrHistogram.UnitTests
{

    public class LongHistogramTests : HistogramTestBase
    {
        protected override int WordSize => sizeof(long);
        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            //return new LongHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            return HistogramFactory.With64BitBucketSize()
                .WithValuesUpTo(highestTrackableValue)
                .WithPrecisionOf(numberOfSignificantValueDigits)
                .Create();
        }
        protected override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            //return new LongHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
            return HistogramFactory.With64BitBucketSize()
                .WithValuesFrom(lowestTrackableValue)
                .WithValuesUpTo(highestTrackableValue)
                .WithPrecisionOf(numberOfSignificantValueDigits)
                .Create();
        }

        [Fact]
        public void RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException()
        {
            var histogram = HistogramFactory.With64BitBucketSize()
                .WithValuesUpTo((long)TimeSpan.FromMinutes(15).TotalMilliseconds)
                .WithPrecisionOf(3)
                .Create();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => histogram.RecordValue(-1));
            ex.Message.Should().Contain("non-negative");
            ex.Message.Should().Contain("-1");
        }
    }
}
