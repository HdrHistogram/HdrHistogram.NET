using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class ShortHistogramTests : HistogramTestBase
    {
        protected override int WordSize => sizeof(short);

        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new ShortHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        }

        protected override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new ShortHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
        }
    }
}