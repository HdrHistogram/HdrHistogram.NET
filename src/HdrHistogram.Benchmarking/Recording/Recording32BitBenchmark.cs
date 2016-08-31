using System.Linq;
using BenchmarkDotNet.Attributes;

namespace HdrHistogram.Benchmarking.Recording
{
    [Config(typeof(ExhuastiveJobWithMemoryDiagnosisConfig))]
    public class Recording32BitBenchmark
    {
        private readonly long[] _testValues;
        private readonly LongHistogram _longHistogram;
        private readonly LongConcurrentHistogram _longConcurrentHistogram;
        private readonly IntHistogram _intHistogram;
        private readonly IntConcurrentHistogram _intConcurrentHistogram;
        private readonly ShortHistogram _shortHistogram;
        private readonly Recorder _longRecorder;
        private readonly Recorder _longConcurrentRecorder;
        private readonly Recorder _intRecorder;
        private readonly Recorder _intConcurrentRecorder;
        private readonly Recorder _shortRecorder;

        public Recording32BitBenchmark()
        {
            //Create array of +ve numbers in the 'maxBit' bit range (i.e. 32 bit or 64bit)
            var highestTrackableValue = TimeStamp.Minutes(10);
            _testValues = Enumerable.Range(0, 32)
                .Select(exp => new { Value = 1L << exp, LZC = 63 - exp })
                .SelectMany(x => new[]
                {
                    x.Value-1,
                    x.Value,
                    x.Value+1,
                })
                .Where(x => x > 0)
                .Where(x => x < highestTrackableValue)
                .Distinct()
                .ToArray();

            _longHistogram = new LongHistogram(highestTrackableValue, 3);
            _intHistogram = new IntHistogram(highestTrackableValue, 3);
            _shortHistogram = new ShortHistogram(highestTrackableValue, 3);

            _longConcurrentHistogram = new LongConcurrentHistogram(1, highestTrackableValue, 3);
            _intConcurrentHistogram = new IntConcurrentHistogram(1, highestTrackableValue, 3);

            _longRecorder = new Recorder(1, highestTrackableValue, 3, (id, low, hi, sf) => new LongHistogram(id, low, hi, sf));
            _longConcurrentRecorder = new Recorder(1, highestTrackableValue, 3, (id, low, hi, sf) => new LongConcurrentHistogram(id, low, hi, sf));
            _intRecorder = new Recorder(1, highestTrackableValue, 3, (id, low, hi, sf) => new IntHistogram(id, low, hi, sf));
            _intConcurrentRecorder = new Recorder(1, highestTrackableValue, 3, (id, low, hi, sf) => new IntConcurrentHistogram(id, low, hi, sf));
            _shortRecorder = new Recorder(1, highestTrackableValue, 3, (id, low, hi, sf) => new ShortHistogram(id, low, hi, sf));
        }

        [Benchmark(Baseline = true)]
        public long LongHistogramRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _longHistogram.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long LongConcurrentHistogramRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _longConcurrentHistogram.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long IntHistogramRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _intHistogram.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long IntConcurrentHistogramRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _intConcurrentHistogram.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long ShortHistogramRecording()
        {
            for (int i = 0; i < _testValues.Length; i++)
            {
                _shortHistogram.RecordValue(_testValues[i]);
            }
            return _shortHistogram.TotalCount;
        }

        [Benchmark]
        public long LongRecorderRecording()
        {
            long counter = 0L;

            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _longRecorder.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long LongConcurrentRecorderRecording()
        {
            long counter = 0L;

            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _longConcurrentRecorder.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long IntRecorderRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _intRecorder.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long IntConcurrentRecorderRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _intConcurrentRecorder.RecordValue(value);
                counter += value;
            }
            return counter;
        }

        [Benchmark]
        public long ShortRecorderRecording()
        {
            long counter = 0L;
            for (int i = 0; i < _testValues.Length; i++)
            {
                var value = _testValues[i];
                _shortRecorder.RecordValue(value);
                counter += value;
            }
            return counter;
        }
    }
}
