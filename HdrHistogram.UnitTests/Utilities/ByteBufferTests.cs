using System;
using System.IO;
using FluentAssertions;
using HdrHistogram.Utilities;
using Xunit;

namespace HdrHistogram.UnitTests.Utilities
{
    public class ByteBufferTests
    {
        /// <summary>
        /// Reproduces Issue #99.
        /// <see cref="ByteBuffer.ReadFrom"/> must loop until all requested bytes
        /// have been read, because <see cref="Stream.Read"/> is permitted to
        /// return fewer bytes than requested — even when more data is available.
        /// <see cref="System.IO.Compression.DeflateStream"/> does exactly this
        /// at DEFLATE block boundaries.
        /// </summary>
        [Theory]
        [InlineData(1024, 100)]
        [InlineData(4096, 511)]
        [InlineData(8192, 1000)]
        public void ReadFrom_returns_all_bytes_when_stream_returns_partial_reads(
            int totalBytes, int maxBytesPerRead)
        {
            var data = new byte[totalBytes];
            new Random(42).NextBytes(data);
            var stream = new PartialReadStream(new MemoryStream(data), maxBytesPerRead);
            var buffer = ByteBuffer.Allocate(totalBytes);

            int bytesRead = buffer.ReadFrom(stream, totalBytes);

            Assert.Equal(totalBytes, bytesRead);
        }

        /// <summary>
        /// A stream wrapper that returns at most <c>maxBytesPerRead</c> bytes
        /// per <see cref="Read"/> call, simulating the behaviour of
        /// <see cref="System.IO.Compression.DeflateStream"/> at compression
        /// block boundaries.
        /// </summary>
        private sealed class PartialReadStream : Stream
        {
            private readonly Stream _inner;
            private readonly int _maxBytesPerRead;

            public PartialReadStream(Stream inner, int maxBytesPerRead)
            {
                _inner = inner;
                _maxBytesPerRead = maxBytesPerRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, Math.Min(count, _maxBytesPerRead));
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }

    public class ByteBufferReadWriteTests
    {
        [Theory]
        [InlineData(42)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void PutInt_and_GetInt_roundtrip(int value)
        {
            var buffer = ByteBuffer.Allocate(sizeof(int));
            buffer.PutInt(value);
            buffer.Position = 0;
            var result = buffer.GetInt();
            Assert.Equal(value, result);
            Assert.Equal(sizeof(int), buffer.Position);
        }

        [Theory]
        [InlineData(4, 12345)]
        [InlineData(8, -99999)]
        public void PutInt_at_index_and_GetInt_roundtrip(int index, int value)
        {
            var buffer = ByteBuffer.Allocate(index + sizeof(int));
            buffer.Position = index;
            int positionBefore = buffer.Position;
            buffer.PutInt(index, value);
            Assert.Equal(positionBefore, buffer.Position);
            buffer.Position = index;
            var result = buffer.GetInt();
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(100L)]
        [InlineData(-1L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void PutLong_and_GetLong_roundtrip(long value)
        {
            var buffer = ByteBuffer.Allocate(sizeof(long));
            buffer.PutLong(value);
            buffer.Position = 0;
            var result = buffer.GetLong();
            Assert.Equal(value, result);
            Assert.Equal(sizeof(long), buffer.Position);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(3.14159265358979)]
        public void PutDouble_and_GetDouble_roundtrip(double value)
        {
            var buffer = ByteBuffer.Allocate(sizeof(double));
            buffer.PutDouble(value);
            buffer.Position = 0;
            var result = buffer.GetDouble();
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(result));
            Assert.Equal(sizeof(double), buffer.Position);
        }

        [Fact]
        public void PutDouble_and_GetDouble_roundtrip_NaN()
        {
            var buffer = ByteBuffer.Allocate(sizeof(double));
            buffer.PutDouble(double.NaN);
            buffer.Position = 0;
            var result = buffer.GetDouble();
            Assert.Equal(BitConverter.DoubleToInt64Bits(double.NaN), BitConverter.DoubleToInt64Bits(result));
        }

        [Theory]
        [InlineData(new byte[] { 0x01, 0x00 }, (short)256)]
        [InlineData(new byte[] { 0x00, 0x7F }, (short)127)]
        public void GetShort_returns_big_endian_value(byte[] bytes, short expected)
        {
            var buffer = ByteBuffer.Allocate(bytes.Length);
            Buffer.BlockCopy(bytes, 0, ByteBufferTestHelper.GetInternalBuffer(buffer), 0, bytes.Length);
            buffer.Position = 0;
            var result = buffer.GetShort();
            Assert.Equal(expected, result);
            buffer.Position.Should().Be(sizeof(short));
        }
    }

    /// <summary>
    /// Test helper to access internal buffer via reflection for test setup.
    /// </summary>
    internal static class ByteBufferTestHelper
    {
        public static byte[] GetInternalBuffer(ByteBuffer buffer)
        {
            var field = typeof(ByteBuffer).GetField("_internalBuffer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (byte[])field!.GetValue(buffer)!;
        }
    }
}
