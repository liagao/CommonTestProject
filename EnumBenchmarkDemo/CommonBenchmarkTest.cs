namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;
    using System.Diagnostics;
    using System.Text;

    [MemoryDiagnoser]
    public class CommonBenchmarkTest
    {
        private StringBuilder sb;
        [Params(1000, 10000, 100000, 1000000)]
        public int HashSetSize { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
            sb = new StringBuilder(1000000);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            sb.Clear();
        }

        /*        [Benchmark]
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
                }*/

        [Benchmark]
        public string InitializeListWithNew()
        {
            string str1 = new string('a', HashSetSize);
            str1 = new string('b', HashSetSize);

            return str1;
        }

        [Benchmark]
        public string InitializeListWithStringBuilder()
        {
            sb.Append(new string('a', HashSetSize));
            sb.Clear();
            sb.Append(new string('b', HashSetSize));
            return sb.ToString();
        }
    }
}
