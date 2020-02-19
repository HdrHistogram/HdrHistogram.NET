namespace HdrHistogram.Examples
{
    class Program
    {
        static void Main()
        {
            //SimpleHistogramExample.Run();
            using (var example = new RecorderExample())
            {
                example.Run();
            }
        }
    }
}
