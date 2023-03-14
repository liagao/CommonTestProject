namespace HttpRequestSender
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;

    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                HttpWebRequest req = WebRequest.CreateHttp(@"http://localhost:89/debug/gc/?force=1");
                WebResponse res = req.GetResponse();    // GetResponse blocks until the response arrives
                Stream ReceiveStream = res.GetResponseStream();    // Read the stream into a string
                StreamReader sr = new StreamReader(ReceiveStream);
                Console.WriteLine(sr.ReadToEnd());

                Thread.Sleep(500);
            }
        }
    }
}
