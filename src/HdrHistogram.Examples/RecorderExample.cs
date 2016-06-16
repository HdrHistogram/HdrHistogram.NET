using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HdrHistogram.Examples
{
    /// <summary>
    /// A simple example of using HdrHistogram's Recorder: run for 20 seconds collecting the
    /// time it takes to perform a simple Datagram Socket create/close operation,
    /// and report a histogram of the times at the end.
    /// </summary>
    static class RecorderExample
    {
        private static readonly Recorder Recorder = new Recorder(1, TimeStamp.Hours(1), 3, (id, low, high, sf) => new LongHistogram(id, low, high, sf));
        private static readonly Lazy<AddressFamily> AddressFamily = new Lazy<AddressFamily>(() => GetAddressFamily("google.com"));
        private static readonly TimeSpan RunPeriod = TimeSpan.FromSeconds(10);
        private static readonly LongHistogram AccumulatingHistogram = new LongHistogram(TimeStamp.Hours(1), 3);
        private const string LogPath = "DatagramSocket.histogram.log";        
        private static HistogramLogWriter _logWriter;
        private static FileStream _outputStream;
        private static int _isCompleted = 0;

        public static void Run()
        {
            Console.WriteLine($"Running for {RunPeriod.TotalSeconds}sec.");

            _outputStream = File.Create(LogPath);
            _logWriter = new HistogramLogWriter(_outputStream);
            //Write the headers, but no histograms (as we don't have any yet).
            _logWriter.Write(DateTime.Now);

            var outputThread = new Thread(ts => WriteToDisk());
            outputThread.Start();
            RecordMeasurements();
        }

        private static void WriteToDisk()
        {
            //Sample every second until flagged as completed.
            while (_isCompleted == 0)
            {
                Thread.Sleep(1000);

                var histogram = Recorder.GetIntervalHistogram();
                AccumulatingHistogram.Add(histogram);
                _logWriter.Append(histogram);
                Console.WriteLine($"{DateTime.Now:o} Interval.TotalCount = {histogram.TotalCount,10:G}. Accumulated.TotalCount = {AccumulatingHistogram.TotalCount,10:G}.");
            }
            _logWriter.Dispose();
            _outputStream.Dispose();
            Console.WriteLine("Log contents");
            Console.WriteLine(File.ReadAllText(LogPath));
            Console.WriteLine("Output thread finishing.");
        }

        /// <summary>
        /// Shows a sample loop where an action is executed, and the latency of each execution is recorded.
        /// </summary>
        private static void RecordMeasurements()
        {
            var timer = Stopwatch.StartNew();
            Action actionToMeasure = CreateAndCloseDatagramSocket;
            do
            {
                Recorder.Record(actionToMeasure);
            } while (timer.Elapsed < RunPeriod);
            Interlocked.Increment(ref _isCompleted);
        }

        private static void CreateAndCloseDatagramSocket()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.Value, SocketType.Stream, ProtocolType.Tcp))
                {
                }
            }
            catch (SocketException)
            {
            }
        }

        private static AddressFamily GetAddressFamily(string url)
        {
            var hostIpAddress = Dns.GetHostEntry(url).AddressList[0];
            var hostIpEndPoint = new IPEndPoint(hostIpAddress, 80);
            return hostIpEndPoint.AddressFamily;
        }
    }
}