using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class LongHistogramTests : HistogramTestBase
    {
        protected override int WordSize => sizeof(long);
        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new LongHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        }
        protected override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new LongHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
        }
    }
}
