using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class IntConcurrentHistogramTests : ConcurrentHistogramTestBase
    {
        protected override int WordSize => sizeof(int);

        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new IntConcurrentHistogram(1, highestTrackableValue, numberOfSignificantValueDigits);
        }

        protected override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new IntConcurrentHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
        }
    }
}