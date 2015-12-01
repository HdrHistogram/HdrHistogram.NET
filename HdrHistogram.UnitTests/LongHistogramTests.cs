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

    }
}
