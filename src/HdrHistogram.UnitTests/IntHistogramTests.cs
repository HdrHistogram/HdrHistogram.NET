using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class IntHistogramTests : HistogramTestBase
    {
        protected override int WordSize => sizeof(int);

        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new IntHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        }

        protected override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new IntHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
        }
    }
}