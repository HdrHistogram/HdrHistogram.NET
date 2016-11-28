using System.Linq;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    public static class HistogramAssert
    {
        public static void AreEqual(HistogramBase expected, HistogramBase actual)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType());
            AreValueEqual(expected, actual);
        }

        public static void AreValueEqual(HistogramBase expected, HistogramBase actual)
        {
            Assert.AreEqual(expected.TotalCount, actual.TotalCount, "TotalCount property is not equal.");
            Assert.AreEqual(expected.Tag, actual.Tag, "Tag property is not equal.");
            Assert.AreEqual(expected.StartTimeStamp, actual.StartTimeStamp, "StartTimeStamp property is not equal.");
            Assert.AreEqual(expected.EndTimeStamp, actual.EndTimeStamp, "EndTimeStamp property is not equal.");
            Assert.AreEqual(expected.LowestTrackableValue, actual.LowestTrackableValue, "LowestTrackableValue property is not equal.");
            Assert.AreEqual(expected.HighestTrackableValue, actual.HighestTrackableValue, "HighestTrackableValue property is not equal.");
            Assert.AreEqual(expected.NumberOfSignificantValueDigits, actual.NumberOfSignificantValueDigits, "NumberOfSignificantValueDigits property is not equal.");
            var expectedValues = expected.AllValues().ToArray();
            var actualValues = actual.AllValues().ToArray();
            CollectionAssert.AreEqual(expectedValues, actualValues, HistogramIterationValueComparer.Instance, "Recorded values differ");
        }
    }
}