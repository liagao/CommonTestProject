using System.Diagnostics;

namespace SearchHarderTrafficReplayer
{
    internal class QueryEmitter
    {
        private readonly string endpointURL;
        private readonly List<StringContent> requestBodyList;
        private readonly int interval;
        private readonly int runInSeconds;
        private readonly string logFolder;
        private readonly string resultSuffix;
        private readonly bool runoneround;
        private readonly bool recordStatistic;

        public QueryEmitter(string endpointURL, List<StringContent> requestBodyList, int interval, int runInSeconds, string logFolder, string resultSuffix, bool runoneround, bool recordStatistic)
        {
            if(string.IsNullOrWhiteSpace(endpointURL))
            {
                throw new ArgumentNullException("endpointURL can't be empty!");
            }

            if (requestBodyList == null || requestBodyList.Count == 0)
            {
                throw new ArgumentNullException("requestBodyList can't be empty!");
            }

            if(interval<=0)
            {
                throw new ArgumentOutOfRangeException("interval can't be negtive!");
            }

            if (runInSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException("runInSeconds can't be negtive!");
            }

            this.endpointURL = endpointURL;
            this.requestBodyList = requestBodyList;
            this.interval = interval;
            this.runInSeconds = runInSeconds;
            this.logFolder = logFolder;
            this.resultSuffix = resultSuffix;
            this.runoneround = runoneround;
            this.recordStatistic = recordStatistic;
        }

        public void Start()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Stopwatch swforquery = Stopwatch.StartNew();
            var client = new HttpClient();

            while (sw.Elapsed.TotalSeconds < runInSeconds)
            {
                for (int i = 0; i < requestBodyList.Count; i++)
                {
                    swforquery.Restart();
                    var response = client.PostAsync(endpointURL, requestBodyList[i]);

                    if(recordStatistic)
                    {
                        var res = response.Result;
                        var queryid = res.Headers.GetValues("X-XAP-QueryId")?.FirstOrDefault(); 
                        var message = res.Content.ReadAsStringAsync().Result;

                        File.AppendAllLines(Path.Combine(this.logFolder, $"{resultSuffix}.txt"), new[] { $"{i}\t{swforquery.ElapsedMilliseconds}\t{queryid}\t{message.Length}" });
                        File.WriteAllText(Path.Combine(this.logFolder, $"{i}-{resultSuffix}-response.txt"), message);

                        Console.WriteLine($"[{DateTime.Now}] Finished run the {i} query of {resultSuffix}...");
                    }

                    Thread.Sleep(interval);
                }

                Console.WriteLine($"[{DateTime.Now}] Finished run {this.resultSuffix} in {sw.Elapsed.TotalSeconds} seconds...");

                if(this.runoneround)
                {
                    break;
                }
            }
        }
    }
}
