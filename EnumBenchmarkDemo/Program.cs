namespace EnumBenchmarkDemo
{
    using Benchmark;
    using BenchmarkDotNet.Running;
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<UnmanagedHeapAllocatorBenchmark>();

            Console.ReadLine();
        }

        private static ArraySegment<byte> GenerateNewSegment(byte[] bytes)
        {
            return new ArraySegment<byte>(bytes);
        }
    }
}