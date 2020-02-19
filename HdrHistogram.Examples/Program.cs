namespace HdrHistogram.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //SimpleHistogramExample.Run();
            using (var example = new RecorderExample())
            {
                example.Run();
            }
        }
    }
}
