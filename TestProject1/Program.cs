using System.Collections.Concurrent;

namespace TestProject1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var set = new ConcurrentDictionary<string, string>();
            Parallel.ForEach (Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt"), file  =>
            {
                Console.WriteLine($"Parsing file: {file}!");
                foreach (var line in File.ReadAllLines(file))
                {
                    if (line.Contains("\"Xap.BingFirstPageResults\""))
                    {
                        var queryid = line.Substring(line.IndexOf('(') + 1, 32);
                        set.TryAdd(queryid, line);
                        Console.WriteLine($"Found one BFPR: {queryid}!!");
                    }

                    if (line.Contains("queryflags=2"))
                    {
                        var queryid = line.Substring(line.IndexOf('(') + 1, 32);
                        Console.WriteLine($"Found one timed out query: {queryid}!!");
                        if (set.ContainsKey(queryid))
                        {
                            Console.WriteLine($"Found one timed out BFPR query: {queryid}!!");
                            Console.WriteLine(set[queryid]);
                        }
                        else
                        {
                            Console.WriteLine($"Not BFPR timed out query: {queryid}!!");
                        }
                    }
                }
            });

            Console.WriteLine("Done!");

            Console.ReadLine();
        }
    }
}
