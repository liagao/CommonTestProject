namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Running;

    public class Program
    {
        static bool[] testarray = { true, true, true, true, true, true, true, true, true };
        public static void Main(string[] args)
        {
            Array.Clear(testarray);

            Console.WriteLine(testarray[0]);
            Console.WriteLine(testarray[7]);

            //BenchmarkRunner.Run<LambdaBenchMarkTest>();

            Console.ReadLine();
        }
    }
}