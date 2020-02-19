using System.IO;
using System.Threading.Tasks;
using HdrHistogram.Iteration;

namespace HdrHistogram.Output
{
    internal sealed class CsvOutputFormatter : IOutputFormatter
    {
        private readonly string _percentileFormatString;
        private readonly string _lastLinePercentileFormatString;
        private readonly TextWriter _textWriter;
        private readonly double _outputValueUnitScalingRatio;

        public CsvOutputFormatter(TextWriter textWriter, int significantDigits, double outputValueUnitScalingRatio)
        {
            _textWriter = textWriter;
            _outputValueUnitScalingRatio = outputValueUnitScalingRatio;
            _percentileFormatString = "{0:F" + significantDigits + "},{1:F12},{2},{3:F2}\n";
            _lastLinePercentileFormatString = "{0:F" + significantDigits + "},{1:F12},{2},Infinity\n";
        }

        public async Task WriteHeaderAsync()
        {
            await _textWriter.WriteAsync("\"Value\",\"Percentile\",\"TotalCount\",\"1/(1-Percentile)\"\n").ConfigureAwait(false);
        }

        public async Task WriteValueAsync(HistogramIterationValue iterationValue)
        {
            var scaledValue = iterationValue.ValueIteratedTo / _outputValueUnitScalingRatio;
            var percentile = iterationValue.PercentileLevelIteratedTo / 100.0D;

            if (iterationValue.IsLastValue())
            {
                await _textWriter.WriteAsync(string.Format(_lastLinePercentileFormatString, scaledValue, percentile, iterationValue.TotalCountToThisValue)).ConfigureAwait(false);
            }
            else
            {
                await _textWriter.WriteAsync(string.Format(_percentileFormatString, scaledValue, percentile, iterationValue.TotalCountToThisValue, 1 / (1.0D - percentile))).ConfigureAwait(false);

            }
        }

#if NETSTANDARD2_0
        public Task WriteFooterAsync(HistogramBase histogram) => Task.CompletedTask;
#else
        private static readonly Task Net45CompletedTask = Task.FromResult(0);
        public Task WriteFooterAsync(HistogramBase histogram) => Net45CompletedTask;
#endif
    }
}
