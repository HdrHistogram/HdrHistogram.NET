using NUnit.Framework;

namespace HdrHistogram.UnitTests.Recording
{
    [TestFixture]
    public sealed class RecorderTestWithLongHistogram : RecorderTestsBase
    {
        protected override HistogramBase Create(long id, long min, long max, int sf)
        {
            return new LongHistogram(id, min, max, sf);
        }
    }
}