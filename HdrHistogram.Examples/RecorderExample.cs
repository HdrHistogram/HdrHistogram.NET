using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HdrHistogram.Examples
{
    /// <summary>
    /// A simple example of using HdrHistogram's Recorder: run for 20 seconds collecting the
    /// time it takes to perform a simple Datagram Socket create/close operation,
    /// and report a histogram of the times at the end.
    /// </summary>
    internal sealed class RecorderExample : IDisposable
    {
        private const string LogPath = "DatagramSocket.histogram.log";
        private static readonly TimeSpan RunPeriod = TimeSpan.FromSeconds(10);


        private readonly HistogramLogWriter _logWriter;
        private readonly FileStream _outputStream;
        private int _isCompleted = -1;

        public RecorderExample()
        {
            _outputStream = File.Create(LogPath);
            _logWriter = new HistogramLogWriter(_outputStream);
        }

        public async Task RunAsync()
        {
            if (HasRunBeenCalled())
            {
                throw new InvalidOperationException("Can only call run once.");
            }

            Console.WriteLine($"Running for {RunPeriod.TotalSeconds}sec.");

            //Write the headers, but no histograms (as we don't have any yet).
            await _logWriter.WriteAsync(DateTime.Now);

            //ThreadSafe-writes require a Concurrent implementation of a Histogram
            //ThreadSafe-reads require a recorder
            var recorder = HistogramFactory
                .With64BitBucketSize()                  //LongHistogram
                .WithValuesFrom(1)                      //Default value
                .WithValuesUpTo(TimeStamp.Minutes(10))  //Default value
                .WithPrecisionOf(3)                     //Default value
                .WithThreadSafeWrites()                 //Switches internal imp to concurrent version i.e. LongConcurrentHistogram
                .WithThreadSafeReads()                  //returns a Recorder that wraps the LongConcurrentHistogram
                .Create();

            //Probably better to Use a TaskCompletionSource instead of this ... (for less allocation)
            var writingToDisk = Task.Run(async () => await WriteToDiskAsync(recorder));
            RecordMeasurements(recorder);

            //Wait for the output thread to complete writing.
            await writingToDisk;
        }

        private bool HasRunBeenCalled()
        {
            var currentValue = Interlocked.CompareExchange(ref _isCompleted, 0, -1);
            return currentValue != -1;
        }

        private async Task WriteToDiskAsync(Recorder recorder)
        {
            //Sample every second until flagged as completed.
            var accumulatingHistogram = new LongHistogram(TimeStamp.Hours(1), 3);
            while (_isCompleted == 0)
            {
                Thread.Sleep(1000);

                var histogram = recorder.GetIntervalHistogram();
                accumulatingHistogram.Add(histogram);
                await _logWriter.AppendAsync(histogram);
                Console.WriteLine($"{DateTime.Now:o} Interval.TotalCount = {histogram.TotalCount,10:G}. Accumulated.TotalCount = {accumulatingHistogram.TotalCount,10:G}.");
            }
            _logWriter.Dispose();
            _outputStream.Dispose();


            Console.WriteLine("Log contents");
            Console.WriteLine(File.ReadAllText(LogPath));
            Console.WriteLine();
            Console.WriteLine("Percentile distribution (values reported in milliseconds)");
            accumulatingHistogram.OutputPercentileDistribution(Console.Out, outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds);

            Console.WriteLine("Output thread finishing.");
        }

        /// <summary>
        /// Shows a sample loop where an action is executed, and the latency of each execution is recorded.
        /// </summary>
        private void RecordMeasurements(IRecorder recorder)
        {
            var sut = new SocketTester("google.com");
            Action actionToMeasure = sut.CreateAndCloseDatagramSocket;
            var timer = Stopwatch.StartNew();
            do
            {
                recorder.Record(actionToMeasure);
            } while (timer.Elapsed < RunPeriod);
            Interlocked.Increment(ref _isCompleted);
        }

        public void Dispose()
        {
            _logWriter.Dispose();
            _outputStream.Dispose();
        }
    }
}