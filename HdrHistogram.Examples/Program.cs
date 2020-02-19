using System.Threading.Tasks;

namespace HdrHistogram.Examples
{
    class Program
    {
        public static async Task Main()
        {
            //await SimpleHistogramExample.RunAsync();
            using var example = new RecorderExample();
            await example.RunAsync().ConfigureAwait(false);
        }
    }
}
