using BenchmarkDotNet.Attributes;

namespace ThreadPoolPriorityBenchmark
{
    [MemoryDiagnoser]
    public class StringConcatBenchemark
    {
        private string pluginName = "VeryLongNameTestPlugin";
        private string pluginVersion = "1.23456";

        public StringConcatBenchemark()
        {
        }

        [Benchmark]
        public void TestWithToString()
        {
            for(int i = 0; i< 1000000; i++)
            {
                var testVar = pluginName + "_" + pluginVersion;
                HandleHashCode(testVar, i);
            }
        }

        [Benchmark]
        public void TestWithNameOf()
        {
            for (int i = 0; i < 1000000; i++)
            {
                var test = string.Concat(pluginName, "_", pluginVersion);
                HandleHashCode(test, i);
            }
        }

        private int HandleHashCode(string test, int i)
        {
            return test.GetHashCode() + i;
        }
    }
}
