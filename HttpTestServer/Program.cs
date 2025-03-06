using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpListener server = new HttpListener();
            server.Prefixes.Add("http://*:4996/");

            server.Start();

            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = server.GetContext();
                HttpListenerResponse response = context.Response;

                Task.Run(()=>HandleResponse(response));
            }

        }

        private static void HandleResponse(HttpListenerResponse response)
        {
            byte[] buffer = Encoding.UTF8.GetBytes("1234567899");

            response.ContentLength64 = buffer.Length;
            Stream st = response.OutputStream;
            st.Write(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}
