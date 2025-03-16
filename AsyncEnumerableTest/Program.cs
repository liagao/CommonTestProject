using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Set timeout to 5 seconds

        try
        {
            await foreach (var number in new AsyncNumberGenerator(10))
            {
                Console.WriteLine(number);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was canceled.");
        }
    }
}

public class AsyncNumberGenerator : IAsyncEnumerable<int>
{
    private readonly int[] _numbers;
    private readonly List<Task<int>> srcs;

    public AsyncNumberGenerator(int size)
    {
        _numbers = new int[size];
        srcs = new List<Task<int>>();
    }

    public async IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        Random random = new Random();
        for (int i = 0; i < _numbers.Length; i++)
        {
            int index = i;
            srcs.Add(Task.Run(() =>
            {
                Thread.Sleep(random.Next(5000)); // Simulate a long running operation
                return index;
            }));
        }

        while (true)
        {
            if(srcs.Count > 0)
            {
                var task = Task.WhenAny(srcs);
                //yield return task.Result.Result;
                srcs.Remove(task.Result);
            }
            else
            yield break;
        }
    }
}