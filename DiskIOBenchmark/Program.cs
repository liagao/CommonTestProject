namespace DiskIOBenchmark
{
    using System.Diagnostics;
    using System.Reflection;

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
                try
                {
                    Assembly.LoadFile(file);
                }
                catch 
                {
                    summary.Add(file, 0);
                    return; 
                }

                summary.Add(file, 1);
            });
            sw.Stop();
            Console.WriteLine($"Total dlls: {summary.Count}, total succeed: {summary.Values.Sum()}, time elapsed: {sw.Elapsed.TotalSeconds} sec");
        }
    }
}