using System.Diagnostics;

namespace SearchHarderTrafficReplayer
{
    internal class QueryEmitter
    {
        private readonly string endpointURL;
        private readonly List<StringContent> requestBodyList;
        private readonly int interval;
        private readonly int runInHours;

        public QueryEmitter(string endpointURL, List<StringContent> requestBodyList, int interval, int runInHours)
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

            if (runInHours <= 0)
            {
                throw new ArgumentOutOfRangeException("runInHours can't be negtive!");
            }

            this.endpointURL = endpointURL;
            this.requestBodyList = requestBodyList;
            this.interval = interval;
            this.runInHours = runInHours;
        }

        public void Start()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var client = new HttpClient();

            while (sw.Elapsed.TotalHours < runInHours)
            {
                foreach (var request in requestBodyList)
                {
                    var res = client.PostAsync(endpointURL, request);
                    Thread.Sleep(interval);
                }

                Console.WriteLine($"[{DateTime.Now}] Finished current round of qury emit to {endpointURL}...");
            }

            Console.WriteLine($"[{DateTime.Now}] Finished run in {runInHours} hours...");
        }
    }
}
