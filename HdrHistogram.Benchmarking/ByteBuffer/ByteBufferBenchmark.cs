using BenchmarkDotNet.Attributes;
using HdrHistogram.Utilities;

namespace HdrHistogram.Benchmarking.ByteBuffer
{
    [MemoryDiagnoser]
    public class ByteBufferBenchmark
    {
        private Utilities.ByteBuffer _writeBuffer = null!;
        private Utilities.ByteBuffer _readBuffer = null!;
        private const int Iterations = 1000;

        [GlobalSetup]
        public void Setup()
        {
            _writeBuffer = Utilities.ByteBuffer.Allocate(Iterations * sizeof(long));
            _readBuffer = Utilities.ByteBuffer.Allocate(Iterations * sizeof(long));
            for (int i = 0; i < Iterations; i++)
            {
                _readBuffer.PutLong(i * 12345678L);
            }
        }

        [Benchmark]
        public void PutLong()
        {
            _writeBuffer.Position = 0;
            for (int i = 0; i < Iterations; i++)
            {
                _writeBuffer.PutLong(i * 12345678L);
            }
        }

        [Benchmark]
        public long GetLong()
        {
            _readBuffer.Position = 0;
            long last = 0;
            for (int i = 0; i < Iterations; i++)
            {
                last = _readBuffer.GetLong();
            }
            return last;
        }
    }
}
