using System.Threading.Tasks;
using HdrHistogram.Iteration;

namespace HdrHistogram.Output
{
    internal interface IOutputFormatter
    {
        Task WriteHeaderAsync();
        Task WriteValueAsync(HistogramIterationValue value);
        Task WriteFooterAsync(HistogramBase histogram);
    }
}
