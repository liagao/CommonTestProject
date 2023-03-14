namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<EnumToStringTest>();
            Console.ReadLine();
        }
    }
}