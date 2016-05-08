using System.Linq;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    public static class HistogramAssert
    {
        public static void AreEqual(HistogramBase expected, HistogramBase actual)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType());
            Assert.AreEqual(expected.TotalCount, actual.TotalCount);
            Assert.AreEqual(expected.StartTimeStamp, actual.StartTimeStamp);
            Assert.AreEqual(expected.EndTimeStamp, actual.EndTimeStamp);
            Assert.AreEqual(expected.LowestTrackableValue, actual.LowestTrackableValue);
            Assert.AreEqual(expected.HighestTrackableValue, actual.HighestTrackableValue);
            Assert.AreEqual(expected.NumberOfSignificantValueDigits, actual.NumberOfSignificantValueDigits);
            var expectedValues = expected.AllValues().ToArray();
            var actualValues = actual.AllValues().ToArray();
            CollectionAssert.AreEqual(expectedValues, actualValues, HistogramIterationValueComparer.Instance);
        }
    }
}