namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;
    using System;

    [MemoryDiagnoser]
    public class ShuffuleBenchmarkTest
    {
        [Params(1000, 10000)]
        public int ListSize { get; set; }

        public List<string> list { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
            this.list = new List<string>(Enumerable.Range(1, ListSize).Select(o =>o.ToString()));
        }

        [Benchmark]
        public void ShuffuleWithSortBy()
        {
            Random random = new Random();
            list.OrderBy(a => random.Next());
        }

        [Benchmark]
        public void ShuffuleWithDefaultWay()
        {
            Random random = new Random();
            var len = this.list.Count;

            for (var i = 0; i < len; i++)
            {
                var j = random.Next(i, len);

                var tmp = this.list[i];
                this.list[i] = this.list[j];
                this.list[j] = tmp;
            }
        }
    }
}
