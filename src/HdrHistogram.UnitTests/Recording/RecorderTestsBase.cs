using System;
using HdrHistogram.Utilities;
using NUnit.Framework;

namespace HdrHistogram.UnitTests.Recording
{
    public abstract class RecorderTestsBase
    {
        private const long DefautltLowestDiscernibleValue = 1;
        private const long DefaultHighestTrackableValue = 7716549600;//TimeStamp.Hours(1); // e.g. for 1 hr in system clock ticks (StopWatch.Frequency)
        private const int DefaultSignificantFigures = 3;

        protected abstract HistogramBase Create(long id, long min, long max, int sf);

        [TestCase(0, 1, DefaultSignificantFigures, "lowestTrackableValue", "lowestTrackableValue must be >= 1")]
        [TestCase(1, 1, DefaultSignificantFigures, "highestTrackableValue", "highestTrackableValue must be >= 2 * lowestTrackableValue")]
        [TestCase(1, DefaultHighestTrackableValue, 6, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        [TestCase(1, DefaultHighestTrackableValue, -1, "numberOfSignificantValueDigits", "numberOfSignificantValueDigits must be between 0 and 5")]
        public void ConstructorShouldRejectInvalidParameters(
           long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits,
           string errorParamName, string errorMessage)
        {
            var ex = Assert.Throws<ArgumentException>(() => 
                new Recorder(
                    lowestTrackableValue, 
                    highestTrackableValue, 
                    numberOfSignificantValueDigits, 
                    Create));
            Assert.AreEqual(errorParamName, ex.ParamName);
            StringAssert.StartsWith(errorMessage, ex.Message);
        }

        [Test]
        public void GetIntervalHistogram_returns_alternating_instances_from_factory()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            var a = recorder.GetIntervalHistogram();
            var b = recorder.GetIntervalHistogram(a);
            var c = recorder.GetIntervalHistogram(b);
            var d = recorder.GetIntervalHistogram(c);

            Assert.AreNotSame(a, b);
            Assert.AreSame(a, c);
            Assert.AreNotSame(a, d);
            Assert.AreSame(b, d);
        }

        [Test]
        public void GetIntervalHistogram_returns_current_histogram_values()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1);
            recorder.RecordValue(10);
            recorder.RecordValue(100);
            var histogram = recorder.GetIntervalHistogram();
            Assert.AreEqual(1, histogram.GetCountAtValue(1));
            Assert.AreEqual(1, histogram.GetCountAtValue(10));
            Assert.AreEqual(1, histogram.GetCountAtValue(100));
        }

        [Test]
        public void GetIntervalHistogram_causes_recording_to_happen_on_new_histogram()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1);
            var histogramPrimary = recorder.GetIntervalHistogram();
            Assert.AreEqual(1, histogramPrimary.GetCountAtValue(1));

            recorder.RecordValue(10);
            recorder.RecordValue(100);
            var histogramSecondary = recorder.GetIntervalHistogram(histogramPrimary);

            Assert.AreEqual(0, histogramSecondary.GetCountAtValue(1));
            Assert.AreEqual(1, histogramSecondary.GetCountAtValue(10));
            Assert.AreEqual(1, histogramSecondary.GetCountAtValue(100));
        }

        [Test]
        public void GetIntervalHistogram_resets_recycled_histogram()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1);
            recorder.RecordValue(10);
            recorder.RecordValue(100);
            var histogramPrimary = recorder.GetIntervalHistogram();

            recorder.RecordValue(1);
            recorder.RecordValue(10);
            recorder.RecordValue(100);
            var histogramSecondary = recorder.GetIntervalHistogram(histogramPrimary);

            Assert.AreEqual(0, histogramPrimary.GetCountAtValue(1));
            Assert.AreEqual(0, histogramPrimary.GetCountAtValue(10));
            Assert.AreEqual(0, histogramPrimary.GetCountAtValue(100));
            Assert.AreEqual(1, histogramSecondary.GetCountAtValue(1));
            Assert.AreEqual(1, histogramSecondary.GetCountAtValue(10));
            Assert.AreEqual(1, histogramSecondary.GetCountAtValue(100));
        }

        [Test]
        public void RecordValue_increments_TotalCount()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1000);
            var histogram = recorder.GetIntervalHistogram();
            Assert.AreEqual(1, histogram.TotalCount);
        }

        [Test]
        public void RecordValue_increments_CountAtValue()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1000);
            recorder.RecordValue(1000);
            recorder.RecordValue(1000);
            var histogram = recorder.GetIntervalHistogram();
            Assert.AreEqual(3, histogram.GetCountAtValue(1000));
        }

        [Test]
        public void RecordValue_Overflow_ShouldThrowException()
        {
            var highestTrackableValue = DefaultHighestTrackableValue;
            var recorder = new Recorder(DefautltLowestDiscernibleValue, highestTrackableValue, DefaultSignificantFigures, Create);
            Assert.Throws<IndexOutOfRangeException>(() => recorder.RecordValue(highestTrackableValue * 3));
        }

        [Test]
        public void RecordValueWithCount_increments_TotalCount()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValueWithCount(1000, 10);
            var histogram = recorder.GetIntervalHistogram();
            Assert.AreEqual(10, histogram.TotalCount);
        }

        [Test]
        public void RecordValueWithCount_increments_CountAtValue()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValueWithCount(1000, 10);
            recorder.RecordValueWithCount(1000, 10);
            recorder.RecordValueWithCount(5000, 20);
            var histogram = recorder.GetIntervalHistogram();
            Assert.AreEqual(20, histogram.GetCountAtValue(1000));
            Assert.AreEqual(20, histogram.GetCountAtValue(5000));
        }

        [Test]
        public void RecordValueWithCount_Overflow_ShouldThrowException()
        {
            var highestTrackableValue = DefaultHighestTrackableValue;
            var recorder = new Recorder(DefautltLowestDiscernibleValue, highestTrackableValue, DefaultSignificantFigures, Create);
            Assert.Throws<IndexOutOfRangeException>(() => recorder.RecordValueWithCount(highestTrackableValue * 3, 100));
        }

        [Test]
        public void RecordValueWithExpectedInterval()
        {
            var TestValueLevel = 4L;
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            var valueHistogram = new LongHistogram(DefaultHighestTrackableValue, DefaultSignificantFigures);

            recorder.RecordValueWithExpectedInterval(TestValueLevel, TestValueLevel / 4);
            valueHistogram.RecordValue(TestValueLevel);

            var intervalHistogram = recorder.GetIntervalHistogram();
            // The data will include corrected samples:
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 1) / 4));
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 2) / 4));
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 3) / 4));
            Assert.AreEqual(1L, intervalHistogram.GetCountAtValue((TestValueLevel * 4) / 4));
            Assert.AreEqual(4L, intervalHistogram.TotalCount);
            // But the raw data will not:
            Assert.AreEqual(0L, valueHistogram.GetCountAtValue((TestValueLevel * 1) / 4));
            Assert.AreEqual(0L, valueHistogram.GetCountAtValue((TestValueLevel * 2) / 4));
            Assert.AreEqual(0L, valueHistogram.GetCountAtValue((TestValueLevel * 3) / 4));
            Assert.AreEqual(1L, valueHistogram.GetCountAtValue((TestValueLevel * 4) / 4));
            Assert.AreEqual(1L, valueHistogram.TotalCount);
        }

        [Test]
        public void RecordAction_increments_TotalCount()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            
            recorder.Record(() => { });

            var longHistogram = recorder.GetIntervalHistogram();
            Assert.AreEqual(1, longHistogram.TotalCount);
        }

        [Test]
        public void Reset_clears_counts_for_instances()
        {
            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1);
            recorder.RecordValue(10);
            recorder.RecordValue(100);
            var histogramPrimary = recorder.GetIntervalHistogram();

            recorder.RecordValue(1);
            recorder.RecordValue(10);
            recorder.RecordValue(100);

            recorder.Reset();
            var histogramSecondary = recorder.GetIntervalHistogram(histogramPrimary);


            Assert.AreEqual(0, histogramPrimary.TotalCount);
            Assert.AreEqual(0, histogramSecondary.TotalCount);
        }

        [Test]
        public void GetIntervalHistogramInto_copies_data_over_provided_Histogram()
        {
            var originalStart = DateTime.Today.AddDays(-1).MillisecondsSinceUnixEpoch();
            var originalEnd = DateTime.Today.MillisecondsSinceUnixEpoch();
            var targetHistogram = new LongHistogram(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures);
            targetHistogram.StartTimeStamp = originalStart;
            targetHistogram.RecordValue(1);
            targetHistogram.RecordValue(10);
            targetHistogram.RecordValue(100);
            targetHistogram.EndTimeStamp = originalEnd;


            Assert.AreEqual(3, targetHistogram.TotalCount);
            Assert.AreEqual(1, targetHistogram.GetCountAtValue(1));
            Assert.AreEqual(1, targetHistogram.GetCountAtValue(10));
            Assert.AreEqual(1, targetHistogram.GetCountAtValue(100));

            var recorder = new Recorder(DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1000);
            recorder.RecordValue(10000);
            recorder.RecordValue(100000);
            
            recorder.GetIntervalHistogramInto(targetHistogram);
            
            Assert.AreEqual(3, targetHistogram.TotalCount);
            Assert.AreEqual(0, targetHistogram.GetCountAtValue(1));
            Assert.AreEqual(0, targetHistogram.GetCountAtValue(10));
            Assert.AreEqual(0, targetHistogram.GetCountAtValue(100));
            Assert.AreEqual(1, targetHistogram.GetCountAtValue(1000));
            Assert.AreEqual(1, targetHistogram.GetCountAtValue(10000));
            Assert.AreEqual(1, targetHistogram.GetCountAtValue(100000));
            Assert.AreNotEqual(originalStart, targetHistogram.StartTimeStamp);
            Assert.AreNotEqual(originalEnd, targetHistogram.EndTimeStamp);
        }

        [Test]
        public void Using_external_histogram_for_recycling_throws()
        {
            const int id = -1000;
            var externallyCreatedHistogram = Create(id, DefautltLowestDiscernibleValue, DefaultHighestTrackableValue, DefaultSignificantFigures);
            var recorder = new Recorder(1, DefaultHighestTrackableValue, DefaultSignificantFigures, Create);
            recorder.RecordValue(1000);

            Assert.Throws<InvalidOperationException>(() => recorder.GetIntervalHistogram(externallyCreatedHistogram));

            recorder.GetIntervalHistogramInto(externallyCreatedHistogram);
        }
    }
}