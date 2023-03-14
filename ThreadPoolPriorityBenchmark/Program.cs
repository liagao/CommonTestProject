namespace ThreadPoolPriorityBenchmark
{
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static void Main(string[] args)
        {
            //BenchmarkRunner.Run<StringConcatBenchemark>();

            //BenchmarkRunner.Run<ThreadPoolPriorityTest>();

            BenchmarkRunner.Run<EnumToStringTest>();
            Console.ReadLine();
        }
    }
}