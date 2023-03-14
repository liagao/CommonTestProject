using System.Diagnostics;

namespace CommonTestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(0 % 1);
            Console.ReadLine();
            if (args.Length == 0)
            {
                StartInNode(0);
                StartInNode(1);
                StartInNode(2);
                StartInNode(3);
                StartInNode(4);
                StartInNode(5);
                StartInNode(6);
                StartInNode(7);

                Console.ReadLine();
            }
            else
            {
                var now = DateTime.Now;
                Parallel.For(0, 100,
                    o =>
                    {
                        while ((DateTime.Now - now) < TimeSpan.FromSeconds(10))
                        {
                            _ = new string[1000];
                        }
                    });
            }
        }

        private static void StartInNode(int nodeId)
        {
            Console.WriteLine($"Start in node: {nodeId}");
            var p = new Process();
            var processStartTime = DateTime.UtcNow;
            p.StartInfo = new ProcessStartInfo
            {
                FileName = @"cmd.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = $"/C \"start /Node {nodeId} ProcessorAffinityTest.exe {nodeId}\"",
            };

            p.Start();

            while ((DateTime.UtcNow - processStartTime) < TimeSpan.FromMinutes(1))
            {
                var list = Process.GetProcessesByName("ProcessorAffinityTest");
                foreach (var item in list)
                {
                    if (item.StartTime.ToUniversalTime() > processStartTime)
                    {
                        long mask = 0x0FFFFFFFF;
                        item.ProcessorAffinity = (System.IntPtr)(mask << ((nodeId%2) * 32));
                        Console.WriteLine($"Reset the process affinity: {nodeId}: {item.Id}");

                        item.WaitForExit();
                        return;
                    }
                }
            }
        }
    }
}
