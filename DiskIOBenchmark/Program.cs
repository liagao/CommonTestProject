namespace DiskIOBenchmark
{
    using System.Diagnostics;
    class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("COMPlus_Thread_UseAllCpuGroups", "1");
            Dictionary<string, int> summary = new Dictionary<string, int>(100000);
            var dir = args[0];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Parallel.ForEach(Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories), file =>
            {
                summary.Add(file, File.ReadAllBytes(file).Length/1000);
            });
            sw.Stop();
            Console.WriteLine($"Total files: {summary.Count}, total read: {summary.Values.Sum()} KB, time elapsed: {sw.Elapsed.TotalSeconds} sec");
        }
    }
}