namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class CommonBenchmarkTest
    {
        [Params(1000, 10000, 100000, 1000000)]
        public int HashSetSize { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
        }

        [Benchmark]
        public List<string> InitializeListWithLoop()
        {
            var list = new List<string>(HashSetSize);
            for (int i = 0; i < HashSetSize; i++)
            {
                list.Add(null);
            }

            return list;
        }

        [Benchmark]
        public List<string> InitializeListWithArray()
        {
            return new string[HashSetSize].ToList();
        }
    }
}
