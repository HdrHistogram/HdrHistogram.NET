using System.Threading.Tasks;

namespace HdrHistogram.Examples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //SimpleHistogramExample.Run();
            using (var example = new RecorderExample())
            {
                await example.RunAsync();
            }
        }
    }
}
