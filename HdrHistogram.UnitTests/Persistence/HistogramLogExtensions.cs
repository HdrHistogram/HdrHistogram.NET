﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HdrHistogram.Utilities;

namespace HdrHistogram.UnitTests.Persistence
{
    public static class HistogramLogExtensions
    {
#if NETCOREAPP3_1
        public static async Task<HistogramBase[]> ReadHistogramsAsync(this byte[] data)
        {
            HistogramBase[] actualHistograms;
            using (var readerStream = new MemoryStream(data))
            {
                actualHistograms = await HistogramLogReader.ReadAsync(readerStream).ToArrayAsync().ConfigureAwait(false);
            }

            return actualHistograms;
        }
#else
        public static HistogramBase[] ReadHistograms(this byte[] data)
        {
            HistogramBase[] actualHistograms;
            using (var readerStream = new MemoryStream(data))
            {
                actualHistograms = HistogramLogReader.Read(readerStream).ToArray();
            }

            return actualHistograms;
        }
#endif

        public static async Task<byte[]> WriteLogAsync(this HistogramBase histogram)
        {
            var startTimeWritten = histogram.StartTimeStamp.ToDateFromMillisecondsSinceEpoch();
            byte[] data;
            using (var writerStream = new MemoryStream())
            {
                await HistogramLogWriter.WriteAsync(writerStream, startTimeWritten, histogram).ConfigureAwait(false);
                data = writerStream.ToArray();
            }
            return data;
        }
        public static void SetTimes(this HistogramBase histogram)
        {
            var startTimeWritten = DateTime.Now;
            var endTimeWritten = startTimeWritten.AddMinutes(30);
            histogram.StartTimeStamp = startTimeWritten.MillisecondsSinceUnixEpoch();
            histogram.EndTimeStamp = endTimeWritten.MillisecondsSinceUnixEpoch();
        }
    }
}
