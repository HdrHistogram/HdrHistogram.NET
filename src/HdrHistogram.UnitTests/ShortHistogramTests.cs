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

        [Test]
        public void Can_add_IntHistogram_with_values_in_range()
        {
            var intHistogram = new IntHistogram(short.MaxValue-1, 3);
            intHistogram.RecordValueWithCount(1, 100);
            intHistogram.RecordValueWithCount(short.MaxValue-1, 1000);

            var shortHistogram = new ShortHistogram(short.MaxValue-1, 3);
            shortHistogram.Add(intHistogram);

            HistogramAssert.AreValueEqual(intHistogram, shortHistogram);
        }
        [Test]
        public void Can_add_LongHistogram_with_values_in_range()
        {
            var longHistogram = new LongHistogram(short.MaxValue-1, 3);
            longHistogram.RecordValueWithCount(1, 100);
            longHistogram.RecordValueWithCount(short.MaxValue-1, 1000);

            var shortHistogram = new ShortHistogram(short.MaxValue-1, 3);
            shortHistogram.Add(longHistogram);

            HistogramAssert.AreValueEqual(longHistogram, shortHistogram);
        }
    }
}