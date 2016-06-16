using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace HdrHistogram.UnitTests
{
    [TestFixture]
    public class TimeStampTests
    {
        [Test]
        public void TimeStamp_values_are_accurate()
        {
            var delay = TimeSpan.FromSeconds(1);
            var expected = TimeStamp.Seconds(delay.Seconds);

            var start = Stopwatch.GetTimestamp();
            Thread.Sleep(delay);
            var end = Stopwatch.GetTimestamp();
            var actual = end - start;
            
            Assert.AreEqual(expected, actual, expected * 0.05);
            Assert.AreEqual(TimeStamp.Seconds(60), TimeStamp.Minutes(1));
            Assert.AreEqual(TimeStamp.Minutes(60), TimeStamp.Hours(1));
        }        
    }
}