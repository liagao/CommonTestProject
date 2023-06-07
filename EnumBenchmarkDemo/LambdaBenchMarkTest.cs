namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class LambdaBenchMarkTest
    {
        [Params(1000)]
        public int ListSize { get; set; }

        public List<int> List { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
            List = Enumerable.Range(1, ListSize).ToList();
        }

        [Benchmark]
        [InvocationCount(100000)]
        public int RunWithLambda()
        {
            return RunTestFuncWithLambda(List, i => i);
        }

        [Benchmark]
        [InvocationCount(100000)]
        public int RunTestFuncDirectly()
        {
            return RunTestFunc(List);
        }

        private int RunTestFuncWithLambda(List<int> list, Func<int, int> function)
        {
            int result = 0;
            foreach (var item in list) { result += function(item); }

            return result;
        }

        private int RunTestFunc(List<int> list)
        {
            int result = 0;
            foreach (var item in list) {
                result += item;
            }
            return result;
        }
    }
}

