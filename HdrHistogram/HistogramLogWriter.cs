using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HdrHistogram.Utilities;

namespace HdrHistogram
{
    /// <summary>
    /// Writes zero, one or many <see cref="HistogramBase"/> instances to a <see cref="Stream"/>.
    /// </summary>
    /// <seealso cref="HistogramLogReader"/>
    public sealed class HistogramLogWriter : IDisposable
    {
        private const string HistogramLogFormatVersion = "1.3";

        private readonly TextWriter _log;
        private bool _hasHeaderWritten = false;
        private int _isDisposed = 0;

        /// <summary>
        /// Writes the provided histograms to the underlying <see cref="Stream"/> with a given overall start time.
        /// </summary>
        /// <param name="outputStream">The <see cref="Stream"/> to write to.</param>
        /// <param name="startTime">The start time of the set of histograms.</param>
        /// <param name="histograms">The histograms to include in the output.</param>
        public static async Task WriteAsync(Stream outputStream, DateTime startTime, params HistogramBase[] histograms)
        {
            using (var writer = new HistogramLogWriter(outputStream))
            {
                await writer.WriteAsync(startTime, histograms).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Creates a <see cref="HistogramLogWriter"/> that writes to an underlying <see cref="Stream"/>.
        /// </summary>
        /// <param name="outputStream">
        /// The stream to write to. 
        /// The stream is left open for the consumer to close.
        /// </param>
        public HistogramLogWriter(Stream outputStream)
        {
            _log = new StreamWriter(outputStream, System.Text.Encoding.Unicode, 1024, true)
            {
                NewLine = "\n"
            };
        }

        /// <summary>
        /// Writes the provided histograms to the underlying <see cref="Stream"/> with a given overall start time.
        /// </summary>
        /// <param name="startTime">The start time of the set of histograms.</param>
        /// <param name="histograms">The histograms to include in the output.</param>
        public async Task WriteAsync(DateTime startTime, params HistogramBase[] histograms)
        {
            await WriteLogFormatVersionAsync().ConfigureAwait(false);
            await WriteStartTimeAsync(startTime).ConfigureAwait(false);
            await WriteLegendAsync().ConfigureAwait(false);
            _hasHeaderWritten = true;
            foreach (var histogram in histograms)
            {
                await WriteHistogramAsync(histogram).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Appends a Histogram to the log. 
        /// </summary>
        /// <param name="histogram">The histogram to write to the log.</param>
        public async Task AppendAsync(HistogramBase histogram)
        {
            if (!_hasHeaderWritten)
            {
                await WriteAsync(histogram.StartTimeStamp.ToDateFromMillisecondsSinceEpoch(), histogram).ConfigureAwait(false);
            }
            else
            {
                await WriteHistogramAsync(histogram).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Output a log format version to the log.
        /// </summary>
        private async Task WriteLogFormatVersionAsync()
        {
            await _log.WriteLineAsync($"#[Histogram log format version {HistogramLogFormatVersion}]").ConfigureAwait(false);
            await _log.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Log a start time in the log.
        /// </summary>
        /// <param name="startTimeWritten">Time the log was started.</param>
        private async Task WriteStartTimeAsync(DateTime startTimeWritten)
        {
            var secondsSinceEpoch = startTimeWritten.SecondsSinceUnixEpoch();
            await _log.WriteLineAsync($"#[StartTime: {secondsSinceEpoch:F3} (seconds since epoch), {startTimeWritten:o}]").ConfigureAwait(false);
            await _log.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteLegendAsync()
        {
            await _log.WriteLineAsync("\"StartTimestamp\",\"Interval_Length\",\"Interval_Max\",\"Interval_Compressed_Histogram\"").ConfigureAwait(false);
            await _log.FlushAsync().ConfigureAwait(false);
        }

        private async Task WriteHistogramAsync(HistogramBase histogram)
        {
            var targetBuffer = ByteBuffer.Allocate(histogram.GetNeededByteBufferCapacity());
            var compressedLength = histogram.EncodeIntoCompressedByteBuffer(targetBuffer);
            var compressedArray = new byte[compressedLength];
            targetBuffer.BlockGet(compressedArray, 0, 0, compressedLength);

            var startTimeStampSec = histogram.StartTimeStamp / 1000.0;
            var endTimeStampSec = histogram.EndTimeStamp / 1000.0;
            var intervalLength = endTimeStampSec - startTimeStampSec;
            var maxValueUnitRatio = 1000000.0;
            var intervalMax = histogram.GetMaxValue() / maxValueUnitRatio;

            var binary = Convert.ToBase64String(compressedArray);
            var payload = histogram.Tag == null
                ? $"{startTimeStampSec:F3},{intervalLength:F3},{intervalMax:F3},{binary}"
                : $"Tag={histogram.Tag},{startTimeStampSec:F3},{intervalLength:F3},{intervalMax:F3},{binary}";
            await _log.WriteLineAsync(payload).ConfigureAwait(false);
            await _log.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                using (_log) { }
            }
        }
    }
}