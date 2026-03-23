/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */

using HdrHistogram.Utilities;
using Xunit;

namespace HdrHistogram.UnitTests.Utilities
{
    public class BitwiseTests
    {
        [Theory]
        [InlineData(0L, 64)]
        [InlineData(1L, 63)]
        [InlineData(2L, 62)]
        [InlineData(4L, 61)]
        [InlineData(8L, 60)]
        [InlineData(16L, 59)]
        [InlineData(32L, 58)]
        [InlineData(64L, 57)]
        [InlineData(128L, 56)]
        [InlineData(256L, 55)]
        [InlineData(512L, 54)]
        [InlineData(1024L, 53)]
        [InlineData(2048L, 52)]
        [InlineData(4096L, 51)]
        [InlineData(8192L, 50)]
        [InlineData(16384L, 49)]
        [InlineData(32768L, 48)]
        [InlineData(65536L, 47)]
        [InlineData(1L << 30, 33)]
        [InlineData(1L << 31, 32)]
        [InlineData(1L << 32, 31)]
        [InlineData(1L << 62, 1)]
        [InlineData(long.MaxValue, 1)]
        public void NumberOfLeadingZeros_ReturnsCorrectValue(long value, int expected)
        {
            Assert.Equal(expected, Bitwise.NumberOfLeadingZeros(value));
        }
    }
}
