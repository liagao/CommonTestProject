namespace ThreadPoolPriorityBenchmark
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
    using ThreadPoolPriorityBenchmark;

    [MemoryDiagnoser]
    public class ThreadPoolPriorityTest
    {
        const int ArraySize = 10000000;
        private List<Tuple<double, double>> tuples = new List<Tuple<double, double>>(ArraySize);

        public ThreadPoolPriorityTest()
        {
            Random ram = new Random();
            for (int i = 0; i < ArraySize; i++)
            {
                this.tuples.Add(new Tuple<double, double>(ram.NextDouble(), ram.NextDouble()));
            }
        }

        [Benchmark]
        public void TestWithNormal()
        {
            this.QueueWorkItemWithPriority(ThreadPriority.Normal);
        }

        [Benchmark]
        public void TestWithLowest()
        {
            this.QueueWorkItemWithPriority(ThreadPriority.Lowest);
        }

        [Benchmark]
        public void TestWithAboveNormal()
        {
            this.QueueWorkItemWithPriority(ThreadPriority.AboveNormal);
        }

        [Benchmark]
        public void TestWithHighest()
        {
            this.QueueWorkItemWithPriority(ThreadPriority.Highest);
        }

        [Benchmark]
        public void TestWithBelowNormal()
        {
            this.QueueWorkItemWithPriority(ThreadPriority.BelowNormal);
        }

        private void QueueWorkItemWithPriority(ThreadPriority priority)
        {
            WaitHandle[] handleList = Enumerable.Range(1, 64).Select(o => new AutoResetEvent(false)).ToArray();

            for (int i = 0; i < 64; i++)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    var signal = o as AutoResetEvent;

                    Thread.CurrentThread.Priority = priority;
                    for (int i = 0; i < this.tuples.Count; i++)
                    {
                        _ = HeavyCalculation(this.tuples[i].Item1, this.tuples[i].Item2);
                    }

                    if (signal != null)
                    {
                        signal.Set();
                    }
                }, handleList[i]);
            }

            WaitHandle.WaitAll(handleList);
        }

        private double HeavyCalculation(double item1, double item2)
        {
            return Math.Pow(item1, item2);
        }
    }
}
