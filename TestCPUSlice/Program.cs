// See https://aka.ms/new-console-template for more information

using System.Web;

class Program
{
    static async Task Main(string[] args)
    {
        var tuple = new Tuple<string, string, string>(null, null, null);
        Console.WriteLine(tuple.Item1+ "!!!" + tuple.Item2);  
        Console.ReadLine();
    }
    private static void TestCPUSlice()
    {
        var start = DateTime.Now;
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(1);
        }
        Console.WriteLine($"TimeSlice: {(DateTime.Now - start).TotalMilliseconds / 100}");
    }
}
