using System.Collections.Generic;

namespace HdrHistogram
{
    /// <summary>
    /// Defines a method for reading Histogram logs in the v1 format.
    /// </summary>
    public interface IHistogramLogV1Reader
    {
        /// <summary>
        /// Reads a v1 formatted histogram log.
        /// </summary>
        /// <returns>Returns a sequence of <see cref="HistogramBase"/> items.</returns>
#if NETSTANDARD2_1
        IAsyncEnumerable<HistogramBase> ReadHistogramsAsync();
#else
        IEnumerable<HistogramBase> ReadHistograms();
#endif
    }
}
