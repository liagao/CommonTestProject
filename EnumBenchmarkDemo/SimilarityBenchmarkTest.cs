namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class SimilarityBenchmarkTest
    {
        [Params(1000, 10000, 100000, 1000000)]
        public int HashSetSize { get; set; }

        public HashSet<string> hs1 { get; set; }

        public HashSet<string> hs2 { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
            this.hs1 = new HashSet<string>(Enumerable.Range(1, HashSetSize).Select(o =>o.ToString()));
            this.hs2 = new HashSet<string>(Enumerable.Range(HashSetSize + 1, HashSetSize).Select(o =>o.ToString()));
        }

        [Benchmark]
        public double HashSetSimilarityWithPerformance()
        {
            if ((hs1.Count + hs2.Count) == 0)
            {
                return 0;
            }

            int intersect = 0;

            foreach (var h in hs2)
            {
                if (hs1.Contains(h))
                {
                    intersect++;
                }
            }

            return intersect * 1.0 / (hs1.Count + hs2.Count - intersect);
        }

        [Benchmark]
        public double HashSetSimilarityWithLinq()
        {
            double union = hs1 == null || hs2 == null ? 0 : hs1.Union(hs2, StringComparer.OrdinalIgnoreCase).Count();
            return union > 0 ? hs1.Intersect(hs2, StringComparer.OrdinalIgnoreCase).Count() / union : 0.0;
        }
    }
}
