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
        
        [Test]
        public void Can_add_LongHistogram_with_values_in_range()
        {
            var longHistogram = new LongHistogram(int.MaxValue - 1, 3);
            longHistogram.RecordValueWithCount(1, 100);
            longHistogram.RecordValueWithCount(int.MaxValue - 1, 1000);

            var shortHistogram = new ShortHistogram(int.MaxValue - 1, 3);
            shortHistogram.Add(longHistogram);

            HistogramAssert.AreValueEqual(longHistogram, shortHistogram);
        }
    }
}