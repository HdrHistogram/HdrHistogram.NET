using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class HistogramFactoryTests
    {
        #region 16bit recording factory tests

        [Test]
        public void CanCreateShortHistogram()
        {
            var actual = HistogramFactory.With16BitBucketSize()
                .Create();
            Assert.IsInstanceOf<ShortHistogram>(actual);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateShortHistogramWithSpecifiedRangeValues(long min, long max, int sf)
        {
            var actual = HistogramFactory.With16BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .Create();
            Assert.IsInstanceOf<ShortHistogram>(actual);
            Assert.AreEqual(min, actual.LowestTrackableValue);
            Assert.AreEqual(max, actual.HighestTrackableValue);
            Assert.AreEqual(sf, actual.NumberOfSignificantValueDigits);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateShortHistogramRecorder(long min, long max, int sf)
        {
            var actual = HistogramFactory.With16BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeReads()
                .Create();
            var histogram = actual.GetIntervalHistogram();
            Assert.IsInstanceOf<ShortHistogram>(histogram);
            Assert.AreEqual(min, histogram.LowestTrackableValue);
            Assert.AreEqual(max, histogram.HighestTrackableValue);
            Assert.AreEqual(sf, histogram.NumberOfSignificantValueDigits);
        }

        #endregion

        #region 32bit recording factory tests

        [Test]
        public void CanCreateIntHistogram()
        {
            var actual = HistogramFactory.With32BitBucketSize()
                .Create();
            Assert.IsInstanceOf<IntHistogram>(actual);
        }
        [Test]
        public void CanCreateIntConcurrentHistogram()
        {
            var actual = HistogramFactory.With32BitBucketSize()
                .WithThreadSafeWrites()
                .Create();
            Assert.IsInstanceOf<IntConcurrentHistogram>(actual);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateIntHistogramWithSpecifiedRangeValues(long min, long max, int sf)
        {
            var actual = HistogramFactory.With32BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .Create();
            Assert.IsInstanceOf<IntHistogram>(actual);
            Assert.AreEqual(min, actual.LowestTrackableValue);
            Assert.AreEqual(max, actual.HighestTrackableValue);
            Assert.AreEqual(sf, actual.NumberOfSignificantValueDigits);
        }
        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void IntConcurrentHistogramWithSpecifiedRangeValues(long min, long max, int sf)
        {
            var actual = HistogramFactory.With32BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeWrites()
                .Create();
            Assert.IsInstanceOf<IntConcurrentHistogram>(actual);
            Assert.AreEqual(min, actual.LowestTrackableValue);
            Assert.AreEqual(max, actual.HighestTrackableValue);
            Assert.AreEqual(sf, actual.NumberOfSignificantValueDigits);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateIntHistogramRecorder(long min, long max, int sf)
        {
            var actual = HistogramFactory.With32BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeReads()
                .Create();
            var histogram = actual.GetIntervalHistogram();
            Assert.IsInstanceOf<IntHistogram>(histogram);
            Assert.AreEqual(min, histogram.LowestTrackableValue);
            Assert.AreEqual(max, histogram.HighestTrackableValue);
            Assert.AreEqual(sf, histogram.NumberOfSignificantValueDigits);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateIntConcurrentHistogramRecorder(long min, long max, int sf)
        {
            var actual = HistogramFactory.With32BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeWrites()
                .WithThreadSafeReads()
                .Create();
            var histogram = actual.GetIntervalHistogram();
            Assert.IsInstanceOf<IntConcurrentHistogram>(histogram);
            Assert.AreEqual(min, histogram.LowestTrackableValue);
            Assert.AreEqual(max, histogram.HighestTrackableValue);
            Assert.AreEqual(sf, histogram.NumberOfSignificantValueDigits);
        }

        #endregion

        #region 64bit recording factory tests

        [Test]
        public void CanCreateLongHistogram()
        {
            var actual = HistogramFactory.With64BitBucketSize()
                .Create();
            Assert.IsInstanceOf<LongHistogram>(actual);
        }
        [Test]
        public void CanCreateLongConcurrentHistogram()
        {
            var actual = HistogramFactory.With64BitBucketSize()
                .WithThreadSafeWrites()
                .Create();
            Assert.IsInstanceOf<LongConcurrentHistogram>(actual);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateLongHistogramWithSpecifiedRangeValues(long min, long max, int sf)
        {
            var actual = HistogramFactory.With64BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .Create();
            Assert.IsInstanceOf<LongHistogram>(actual);
            Assert.AreEqual(min, actual.LowestTrackableValue);
            Assert.AreEqual(max, actual.HighestTrackableValue);
            Assert.AreEqual(sf, actual.NumberOfSignificantValueDigits);
        }
        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void LongConcurrentHistogramWithSpecifiedRangeValues(long min, long max, int sf)
        {
            var actual = HistogramFactory.With64BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeWrites()
                .Create();
            Assert.IsInstanceOf<LongConcurrentHistogram>(actual);
            Assert.AreEqual(min, actual.LowestTrackableValue);
            Assert.AreEqual(max, actual.HighestTrackableValue);
            Assert.AreEqual(sf, actual.NumberOfSignificantValueDigits);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateLongHistogramRecorder(long min, long max, int sf)
        {
            var actual = HistogramFactory.With64BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeReads()
                .Create();
            var histogram = actual.GetIntervalHistogram();
            Assert.IsInstanceOf<LongHistogram>(histogram);
            Assert.AreEqual(min, histogram.LowestTrackableValue);
            Assert.AreEqual(max, histogram.HighestTrackableValue);
            Assert.AreEqual(sf, histogram.NumberOfSignificantValueDigits);
        }

        [TestCase(1, 5000, 3)]
        [TestCase(1000, 100000, 5)]
        public void CanCreateLongConcurrentHistogramRecorder(long min, long max, int sf)
        {
            var actual = HistogramFactory.With64BitBucketSize()
                .WithValuesFrom(min)
                .WithValuesUpTo(max)
                .WithPrecisionOf(sf)
                .WithThreadSafeWrites()
                .WithThreadSafeReads()
                .Create();
            var histogram = actual.GetIntervalHistogram();
            Assert.IsInstanceOf<LongConcurrentHistogram>(histogram);
            Assert.AreEqual(min, histogram.LowestTrackableValue);
            Assert.AreEqual(max, histogram.HighestTrackableValue);
            Assert.AreEqual(sf, histogram.NumberOfSignificantValueDigits);
        }

        #endregion
    }
}