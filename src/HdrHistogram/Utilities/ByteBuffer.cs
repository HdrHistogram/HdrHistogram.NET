/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

using System;
using System.Diagnostics;

namespace HdrHistogram.Utilities
{
    // See http://stackoverflow.com/questions/1261543/equivalent-of-javas-bytebuffer-puttype-in-c-sharp
    // and http://stackoverflow.com/questions/18040012/what-is-the-equivalent-of-javas-bytebuffer-wrap-in-c
    // and http://stackoverflow.com/questions/1261543/equivalent-of-javas-bytebuffer-puttype-in-c-sharp
    // Java version http://docs.oracle.com/javase/7/docs/api/java/nio/ByteBuffer.html
    /// <summary>
    /// A byte buffer that tracks position and allows reads and writes of 32 and 64 bit integer values.
    /// </summary>
    public sealed class ByteBuffer
    {
        private readonly byte[] _internalBuffer;

        /// <summary>
        /// Creates a <see cref="ByteBuffer"/> with a specified capacity in bytes.
        /// </summary>
        /// <param name="bufferCapacity">The capacity of the buffer in bytes</param>
        /// <returns>A newly created <see cref="ByteBuffer"/>.</returns>
        public static ByteBuffer Allocate(int bufferCapacity)
        {
            return new ByteBuffer(bufferCapacity);
        }

        private ByteBuffer(int bufferCapacity)
        {
            _internalBuffer = new byte[bufferCapacity];
            Position = 0;
        }

        /// <summary>
        /// The buffer's current position in the underlying byte array
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Returns the capacity of the <see cref="ByteBuffer"/>
        /// </summary>
        /// <returns>The length of the internal byte array.</returns>
        public int Capacity()
        {
            return _internalBuffer.Length;
        }

        public int ReadFrom(System.IO.Stream source, int length)
        {
            return source.Read(_internalBuffer, Position, length);
        }
        public void WriteTo(System.IO.Stream target, int offset, int length)
        {
            target.Write(_internalBuffer, offset, length);
            target.Flush();
        }

        /// <summary>
        /// Gets the value of the current position as an <see cref="int"/> value, and advances the position to the next int.
        /// </summary>
        /// <returns>The value of the <see cref="int"/> at the current position.</returns>
        public int GetInt()
        {
            var intValue = BitConverter.ToInt32(_internalBuffer, Position);
            Position += sizeof(int);
            return intValue;
        }

        /// <summary>
        /// Gets the value of the current position as an <see cref="long"/> value, and advances the position to the next long.
        /// </summary>
        /// <returns>The value of the long at the current position.</returns>
        public long GetLong()
        {
            var longValue = BitConverter.ToInt64(_internalBuffer, Position);
            Position += sizeof(long);
            return longValue;
        }

        /// <summary>
        /// Sets the bytes at the current position to the value of the passed value, and advances the position.
        /// </summary>
        /// <param name="value">The value to set the current position to.</param>
        public void PutInt(int value)
        {
            var intAsBytes = BitConverter.GetBytes(value);
            Array.Copy(intAsBytes, 0, _internalBuffer, Position, intAsBytes.Length);
            Position += intAsBytes.Length;
        }

        /// <summary>
        /// Sets the bytes at the provided position to the value of the passed value, and does not advance the position.
        /// </summary>
        /// <param name="index">The position to set the value at.</param>
        /// <param name="value">The value to set.</param>
        internal void PutInt(int index, int value)
        {
            var intAsBytes = BitConverter.GetBytes(value);
            Array.Copy(intAsBytes, 0, _internalBuffer, index, intAsBytes.Length);
            // We don't increment the Position here, to match the Java behavior
        }

        /// <summary>
        /// Sets the bytes at the current position to the value of the passed value, and advances the position.
        /// </summary>
        /// <param name="value">The value to set the current position to.</param>
        public void PutLong(long value)
        {
            var longAsBytes = BitConverter.GetBytes(value);
            Array.Copy(longAsBytes, 0, _internalBuffer, Position, longAsBytes.Length);
            Position += longAsBytes.Length;
        }

        /// <summary>
        /// Gets a copy of the internal byte array.
        /// </summary>
        /// <returns>The a copy of the internal byte array.</returns>
        internal byte[] ToArray()
        {
            var copy = new byte[_internalBuffer.Length];
            Array.Copy(_internalBuffer, copy, _internalBuffer.Length);
            return copy;
        }

        internal CountingMemoryStream GetWriter()
        {
            return new CountingMemoryStream(_internalBuffer, Position, _internalBuffer.Length - Position);
        }

        internal void BlockCopy(Array src, int srcOffset, int dstOffset, int count)
        {
            Debug.WriteLine("  Buffer.BlockCopy - Copying {0} bytes INTO internalBuffer, scrOffset = {1}, targetOffset = {2}", count, srcOffset, dstOffset);
            Buffer.BlockCopy(src: src, srcOffset: srcOffset, dst: _internalBuffer, dstOffset: dstOffset, count: count);
            Position += count;
        }

        internal void BlockGet(Array target, int targetOffset, int sourceOffset, int count)
        {
            Debug.WriteLine("  Buffer.BlockCopy - Copying {0} bytes FROM internalBuffer, scrOffset = {1}, targetOffset = {2}", count, sourceOffset, targetOffset);
            Buffer.BlockCopy(src: _internalBuffer, srcOffset: sourceOffset, dst: target, dstOffset: targetOffset, count: count);
        }

        internal WrappedBuffer<short> AsShortBuffer()
        {
            return WrappedBuffer<short>.Create(this);
        }

        internal WrappedBuffer<int> AsIntBuffer()
        {
            return WrappedBuffer<int>.Create(this);
        }

        internal WrappedBuffer<long> AsLongBuffer()
        {
            return WrappedBuffer<long>.Create(this);
        }
    }
}