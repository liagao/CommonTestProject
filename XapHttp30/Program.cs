using System.Net;

namespace XapHttp30
{
    internal class Program
    {
        public static async IAsyncEnumerable<int> GenerateNumbersAsync(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(100); // Simulate asynchronous work
                yield return i;
            }
        }

        static async void Main(string[] args)
        {
            /*using var client = new HttpClient
            {
                DefaultRequestVersion = HttpVersion.Version30,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };


            Console.WriteLine("--- localhost:5001 ---");
            var task1 = client.GetAsync("https://cloudflare-quic.com");
            task1.Wait();
            var resp = task1.Result;
            var task2 = resp.Content.ReadAsStringAsync();
            task2.Wait();
            string body = task2.Result;

            Console.WriteLine(
                $"status: {resp.StatusCode}, version: {resp.Version}, " +
                $"body: {body.Substring(0, Math.Min(100, body.Length))}");*/

            await foreach (var number in GenerateNumbersAsync(10))
            {
                Console.WriteLine(number);
            }
        }
    }
}
