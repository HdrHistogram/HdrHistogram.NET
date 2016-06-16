using NUnit.Framework;

namespace HdrHistogram.UnitTests.Recording
{
    [TestFixture]
    public sealed class RecorderTestWithIntHistogram : RecorderTestsBase
    {
        protected override HistogramBase Create(long id, long min, long max, int sf)
        {
            return new IntHistogram(id, min, max, sf);
        }
    }
}