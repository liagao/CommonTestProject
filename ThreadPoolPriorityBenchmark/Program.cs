namespace ThreadPoolPriorityBenchmark
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static void Main(string[] args)
        {
            //BenchmarkRunner.Run<StringConcatBenchemark>();

            //BenchmarkRunner.Run<ThreadPoolPriorityTest>();

            BenchmarkRunner.Run<TestWithParamsBenchemark>();

            Console.ReadLine();
        }
    }


    [MemoryDiagnoser]
    public class TestWithParamsBenchemark
    {
        public TestWithParamsBenchemark()
        {
        }

        [Params(10000)]
        public int Loop;

        public int[] array;

        [GlobalSetup]
        public void Setup()
        {
            this.array = new int[Loop];
        }

        [Benchmark]
        public void TestWithToString()
        {
            var testVar = Array.Empty<byte>();
            if(testVar.Length != 0)
            {
                throw new Exception("!!!");
            }
        }

        [Benchmark]
        public void TestWithNameOf()
        {
            var testVar = new byte[0];
            if (testVar.Length != 0)
            {
                throw new Exception("!!!");
            }
        }

        private int HandleHashCode(string test, int i)
        {
            return test.GetHashCode() + i;
        }
    }
}