using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HdrHistogram.UnitTests.Persistence
{

    public sealed class IntHistogramLogReaderWriterTests : HistogramLogReaderWriterTestBase
    {
        protected override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            return new IntHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        }
        [Theory]
        [MemberData(nameof(PowersOfTwo))]
        public async Task CanRoundTripSingleHistogramsWithFullRangesOfCountsAndValuesAsync(long count)
        {
            await RoundTripSingleHistogramsWithFullRangesOfCountsAndValuesAsync(count).ConfigureAwait(false);
        }

        public static IEnumerable<object[]> PowersOfTwo()
        {
            return TestCaseGenerator.PowersOfTwo(31)
                .Select(v => new object[1] { v });
        }
    }
}
