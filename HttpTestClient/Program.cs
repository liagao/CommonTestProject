using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpClientApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:4996");
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {content}");
        }
    }
}
