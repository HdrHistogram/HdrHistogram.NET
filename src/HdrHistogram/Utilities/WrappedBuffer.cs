/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

namespace HdrHistogram.Utilities
{
    // This needs to be a view on-top of a byte array
    sealed class WrappedBuffer<T> where T : struct
    {
        private readonly ByteBuffer _underlyingBuffer;
        private readonly int _parentOffset;

        public static WrappedBuffer<T> Create(ByteBuffer underlyingBuffer)
        {
            return new WrappedBuffer<T>(underlyingBuffer);
        }

        private WrappedBuffer(ByteBuffer underlyingBuffer)
        {
            _underlyingBuffer = underlyingBuffer;
            _parentOffset = underlyingBuffer.Position;
        }

        internal void Put(T[] values, int index, int length)
        {
            _underlyingBuffer.BlockCopy(src: values, srcOffset: index, dstOffset: _parentOffset, count: length);
        }

        internal void Get(T[] destination, int index, int length)
        {
            _underlyingBuffer.BlockGet(target: destination, targetOffset: index, sourceOffset: _parentOffset, count: length);
        }

        internal void Rewind()
        {
            _underlyingBuffer.Position = _parentOffset;
        }
    }
}
