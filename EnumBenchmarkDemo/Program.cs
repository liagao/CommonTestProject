namespace EnumBenchmarkDemo
{
    using BenchmarkDotNet.Running;
    using System;
    using System.Collections.Concurrent;

    public class Program
    {
        public static void Main(string[] args)
        {
            //BenchmarkRunner.Run<ShuffuleBenchmarkTest>();
            var dic = new ConcurrentDictionary<string, int>();
            int loop = 1000;
            while(loop-->0)
            {
                Console.WriteLine(loop);
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    var id = Thread.CurrentThread.Name;
                    dic.AddOrUpdate(id.ToString(), 1, (key, value) => value + 1);
                    Thread.Sleep(100);
                });
            }

            foreach(var item in dic)
            {
                Console.WriteLine(item.Key + "=>" + item.Value);
            }

            Console.ReadLine();
        }

        private static ArraySegment<byte> GenerateNewSegment(byte[] bytes)
        {
            return new ArraySegment<byte>(bytes);
        }
    }
}