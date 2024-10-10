namespace SearchHarderTrafficReplayer
{
    using System;
    using System.Text;

    internal class Program
    {
        private static string SHTestEndpoint = "https://fabricrouter-external.falcon-core.ingress.kors.microsoft-falcon.net/xap-experiments.llmapps/SearchHarder";

        private static string SHNativeEndpoint = "https://fabricrouter-external.falcon-core.ingress.wus2.microsoft-falcon.net/xap-experiments.xaplangtool-searchhardernative/SearchHarder";
        private static string SHLangToolsEndpoint = "https://fabricrouter-external.falcon-core.ingress.wus2.microsoft-falcon.net/xap-experiments.xaplangtool-searchharder/SearchHarder";
        private static string SHNativePerfTestEndpoint = "https://fabricrouter-external.falcon-core.ingress.wus2.microsoft-falcon.net/xap-experiments.xaplangtool-searchhardernativenat2/SearchHarder";
        private static string SHLangToolsPerfTestEndpoint = "https://fabricrouter-external.falcon-core.ingress.wus2.microsoft-falcon.net/xap-experiments.xaplangtool-searchharder2/SearchHarder";
        private static string QuerySetFileName = "queryset.tsv";
        private static int QPS = 1;
        private static int RunInSeconds = 60*60;
        private static string MessageTemplate = "{{\"timestamp\":\"{0}\",\"messageType\":\"SearchHarder\",\"locationHints\":[{{\"SourceType\":2,\"CountryName\":\"{1}\",\"Admin1Name\":\"{2}\",\"PopulatedPlaceName\":\"{3}\",\"Center\":{{\"latitude\":{4},\"longitude\":{5}}},\"Name\":\"{6}\",\"RegionType\":2}}],\"privacy\":\"Internal\",\"text\":\"{7}\",\"locale\":\"{8}\",\"localeInfo\":{{\"trueuilanguage\":\"{9}\",\"language\":\"{10}\",\"region\":\"{11}\",\"market\":\"{12}\",\"truemarket\":\"{13}\",\"uilanguage\":\"{14}\"}},\"locationInfo\":{{\"country\":\"{15}\",\"state\":\"{16}\",\"city\":\"{17}\",\"sourceType\":1,\"isImplicitLocationIntent\":false}},\"market\":\"{18}\",\"region\":\"{19}\",\"searchHarderCacheKey\":\"{20}\"}}, \"optionsSets\": [\"searchharder\",\"shbindebug\"], \"options\": {{\"BingFirstPageWorkflowVariants\":{{\"mkt\":\"{21}\",\"shmuid\":\"bgm\",\"revip\":\"{22}\"}}}}, \"bingFirstPageAdultFilter\": \"2\", \"conversationId\": \"{23}\", \"RequestId\": \"{24}\", \"traceId\": \"{25}\" }}";
        private static string SHNativeBodyTemplate = "{{\"context\":{{\"UserMessage\": {0}  }}";
        private static string SHLangToolsBodyTemplate = "{{ \"isFireAndForget\": {{\"Value\": \"false\", \"CastTo\": \"bool\"}},  \"request\": {{\"Value\": '{{\"message\": {0}',\"CastTo\": \"string\"}} }}";
        private static string SHNativeSuffix = "native";
        private static string SHLangToolsNativeSuffix = "langtools";
        private static string LogFolder = @"D:\result\";

        static void Main(string[] args)
        {
            List<StringContent> shnativeMessageList = new List<StringContent>(120);
            List<StringContent> shlangtoolsMessageList = new List<StringContent>(120);

            int index = 0;
            foreach(var query in File.ReadAllLines(QuerySetFileName).Skip(1))
            {
                var message = FormatMessage(query.Split('\t'));
                var shnativeMessage = string.Format(SHNativeBodyTemplate, message);
                shnativeMessageList.Add(new StringContent(shnativeMessage, Encoding.UTF8, "application/json"));
                File.WriteAllText(Path.Combine(LogFolder, $"{index}-{SHNativeSuffix}-request.txt"), shnativeMessage);

                message = FormatMessage(query.Split('\t'));
                var shlangtoolsMessage = string.Format(SHLangToolsBodyTemplate, message);
                shlangtoolsMessageList.Add(new StringContent(shlangtoolsMessage, Encoding.UTF8, "application/json"));
                File.WriteAllText(Path.Combine(LogFolder, $"{index}-{SHLangToolsNativeSuffix}-request.txt"), shlangtoolsMessage);
                index++;
            }

            // 1. For perf test 
            //Task.Run(() => new QueryEmitter(SHNativePerfTestEndpoint, shnativeMessageList, 1000 / QPS, RunInSeconds, LogFolder, SHNativeSuffix, runoneround: true, recordStatistic: true).Start());
            //Task.Run(() => new QueryEmitter(SHLangToolsEndpoint, shlangtoolsMessageList, 1000 / QPS, RunInSeconds, LogFolder, SHLangToolsNativeSuffix, runoneround: true, recordStatistic: true).Start());

            // 2. For single endpoint test
            Task.Run(() => new QueryEmitter(SHTestEndpoint, shnativeMessageList, 1000 / QPS, RunInSeconds, LogFolder, SHNativeSuffix, runoneround: true, recordStatistic: true).Start());
            Console.ReadLine();
        }

        private static string FormatMessage(string[] columns)
        {
            var searchHarderCacheKey = $"SHScraper{Guid.NewGuid()}";
            return string.Format(MessageTemplate,
                DateTime.UtcNow.ToString("o"),
                columns[19],
                columns[18],
                columns[17],
                columns[15],
                columns[16],
                $"{columns[17]}, {columns[18]}, {columns[19]}",
                columns[14],
                $"{columns[8]}-{columns[9]}".ToLowerInvariant(),
                columns[8],
                columns[8],
                columns[9],
                columns[1],
                columns[5],
                columns[8],
                columns[19],
                columns[18],
                columns[17],
                columns[1],
                columns[9],
                searchHarderCacheKey,
                columns[1].ToLowerInvariant(),
                columns[9],
                searchHarderCacheKey,
                Guid.NewGuid(),
                Guid.NewGuid()
            );
        }
    }
}
