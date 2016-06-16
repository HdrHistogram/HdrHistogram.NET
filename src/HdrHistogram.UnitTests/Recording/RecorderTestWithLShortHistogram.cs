using NUnit.Framework;

namespace HdrHistogram.UnitTests.Recording
{
    [TestFixture]
    public sealed class RecorderTestWithLShortHistogram : RecorderTestsBase
    {
        protected override HistogramBase Create(long id, long min, long max, int sf)
        {
            return new ShortHistogram(id, min, max, sf);
        }
    }
}