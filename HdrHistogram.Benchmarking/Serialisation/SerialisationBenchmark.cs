using BenchmarkDotNet.Attributes;
using HdrHistogram.Encoding;

namespace HdrHistogram.Benchmarking.Serialisation
{
    [MemoryDiagnoser]
    public class SerialisationBenchmark
    {
        private LongHistogram _source = null!;
        private Utilities.ByteBuffer _encodeBuffer = null!;
        private Utilities.ByteBuffer _decodeBuffer = null!;
        private Utilities.ByteBuffer _compressedDecodeBuffer = null!;

        [GlobalSetup]
        public void Setup()
        {
            _source = new LongHistogram(3600_000_000L, 3);
            for (long i = 0; i < 10_000; i++)
            {
                _source.RecordValue(1000L * i);
            }

            var capacity = _source.GetNeededByteBufferCapacity();
            _encodeBuffer = Utilities.ByteBuffer.Allocate(capacity);

            // Pre-encode for uncompressed decode benchmark
            _source.Encode(_decodeBuffer = Utilities.ByteBuffer.Allocate(capacity), HistogramEncoderV2.Instance);

            // Pre-encode for compressed decode benchmark
            _source.EncodeIntoCompressedByteBuffer(_compressedDecodeBuffer = Utilities.ByteBuffer.Allocate(capacity));
        }

        [Benchmark]
        public int Encode()
        {
            _encodeBuffer.Position = 0;
            return _source.Encode(_encodeBuffer, HistogramEncoderV2.Instance);
        }

        [Benchmark]
        public HistogramBase Decode()
        {
            _decodeBuffer.Position = 0;
            return HistogramEncoding.DecodeFromByteBuffer(_decodeBuffer, 0);
        }

        [Benchmark]
        public int EncodeCompressed()
        {
            _encodeBuffer.Position = 0;
            return _source.EncodeIntoCompressedByteBuffer(_encodeBuffer);
        }

        [Benchmark]
        public HistogramBase DecodeCompressed()
        {
            _compressedDecodeBuffer.Position = 0;
            return HistogramEncoding.DecodeFromCompressedByteBuffer(_compressedDecodeBuffer, 0);
        }
    }
}
