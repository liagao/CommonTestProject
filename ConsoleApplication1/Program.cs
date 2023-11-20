namespace ConsoleApp4
{
    using Microsoft.Cloud.Metrics.Client;
    using Microsoft.Cloud.Metrics.Client.Metrics;
    using Microsoft.Online.Metrics.Serialization.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    class Program
    {
        public static ConnectionInfo ConnectionInfo = new ConnectionInfo();

        public const string EnvironmentKey = "EnvironmentName";
        public const string MachineFunctionKey = "MachineFunction";
        public const string WorkflowKey = "Workflow";
        public const string NodeKey = "Node";
        public const string PremiumKey = "Is Premium QOR";
        public const string MarketKey = "Market";
        public const string DefaultWorkflowName = "Xap.BingFirstPageResults";


        public const string MachineFunction = "AH";
        public const double StartHours = -6;
        public const double EndHours = -1;

        static void Main(string[] args)
        {
            //GenerateRegressedNodeList("JA-JP", "premium");
            //GenerateRegressedNodeList("EN-AU", "premium");
            //GenerateRegressedNodeList("EN-IN", "premium");
            //GenerateRegressedNodeList("ZH-CN", "premium");
            var format = @"https://portal.microsoftgeneva.com/dashboard/BingPlat_XAP/XAP%2520DRI/Flight%2520Shipment?overrides=[{{""query"":""//*[id='Workflow']"",""key"":""value"",""replacement"":""{0}""}},{{""query"":""//*[id='Flight']"",""key"":""value"",""replacement"":""{1}""}},{{""query"":""//*[id='Flight']"",""key"":""value"",""replacement"":""""}},{{""query"":""//*[id='EnvironmentName']"",""key"":""value"",""replacement"":""{2}""}}]%20";


            var startTimeFormat = "yyyy-MM-dd HH:mm:ss";
            var startTime = "2023-09-02T00:00:00Z";
            var endTime = "2023-09-02T16:00:00Z";
            Console.WriteLine(string.Format(format, "Xap.BingFirstPageResult", string.Join(",", GetMdmCounterValue(
                                                "Xap-Prod-CO4", 
                                                null, 
                                                DateTime.ParseExact(startTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                                                DateTime.ParseExact(endTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime())), "Xap-Prod-CO4"));
            Console.ReadLine();
        }

        private static List<string> GetMdmCounterValue(string environmentName, string workflowName, DateTime? startTime, DateTime? endTime)
        {
            var startResultList = new List<string>();
            var endResultList = new List<string>();

            MetricReader reader;

            try
            {
                reader = new MetricReader(ConnectionInfo);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            var dimensionFilters = new List<DimensionFilter>();

            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                dimensionFilters.Add(DimensionFilter.CreateIncludeFilter("EnvironmentName", environmentName));
            }

            if (!string.IsNullOrWhiteSpace(workflowName))
            {
                dimensionFilters.Add(DimensionFilter.CreateIncludeFilter("Workflow", workflowName));
            }

            dimensionFilters.Add(DimensionFilter.CreateIncludeFilter("Flight"));

            var mdmCounter = new MetricIdentifier("BingPlat_XAP", "AppHost.Workflow", "All Flights Latency");
            var samplingType = SamplingType.Count;

            if (startTime != null)
            {
                var value = reader.GetTimeSeriesAsync(
                    mdmCounter,
                    dimensionFilters,
                    startTime.Value.AddMinutes(-60),
                    startTime.Value.AddMinutes(60),
                    new[]
                    {
                        samplingType
                    });

                foreach (var item in value.Result.Results)
                {
                    double first = item.GetTimeSeriesValues(samplingType).First();
                    double last = item.GetTimeSeriesValues(samplingType).Last();
                    if ((first == 0 || double.IsNaN(first)) && last > 0)
                    {
                        startResultList.Add(item.DimensionList.First(o => o.Key == "Flight").Value);
                    }
                }
            }

            if (endTime != null)
            {
                var value = reader.GetTimeSeriesAsync(
                    mdmCounter,
                    dimensionFilters,
                    endTime.Value.AddMinutes(-60),
                    endTime.Value.AddMinutes(60),
                    new[]
                    {
                        samplingType
                    });

                foreach (var item in value.Result.Results)
                {
                    double first = item.GetTimeSeriesValues(samplingType).First();
                    double last = item.GetTimeSeriesValues(samplingType).Last();
                    if ((last == 0 || double.IsNaN(last)) && first > 0)
                    {
                        endResultList.Add(item.DimensionList.First(o => o.Key == "Flight").Value);
                    }
                }
            }

            if (startTime == null && endTime != null)
            {
                return endResultList;
            }

            if (endTime == null && startTime != null)
            {
                return startResultList;
            }

            if (startTime != null && endTime != null)
            {
                return startResultList.Intersect(endResultList).ToList();
            }

            return new List<string>();
        }

        private static void GenerateRegressedNodeList(string market, string isPreminumQoR)
        {
            var suffix = $"{market}_{isPreminumQoR}";
            var avgFile = GenerateDiffFile(
                "Request Latency",
                SamplingType.Average,
                "BingPlat_XAP_Plugins_Hk",
                "AppHost.Plugin",
                new List<DimensionFilter>() {
                    DimensionFilter.CreateIncludeFilter(EnvironmentKey, "Xap-Prod-HKG01"),
                    DimensionFilter.CreateIncludeFilter(MachineFunctionKey, MachineFunction),
                    DimensionFilter.CreateExcludeFilter(NodeKey, string.Empty),
                    DimensionFilter.CreateIncludeFilter(WorkflowKey, DefaultWorkflowName),
                    DimensionFilter.CreateIncludeFilter(PremiumKey, isPreminumQoR),
                    DimensionFilter.CreateIncludeFilter(MarketKey, market) },
                "BingPlat_XAP_Plugins_Pu",
                "AppHost.Plugin",
                new List<DimensionFilter>() {
                    DimensionFilter.CreateIncludeFilter(EnvironmentKey, "Xap-Prod-PUSE01"),
                    DimensionFilter.CreateIncludeFilter(MachineFunctionKey, MachineFunction),
                    DimensionFilter.CreateExcludeFilter(NodeKey, string.Empty),
                    DimensionFilter.CreateIncludeFilter(WorkflowKey, DefaultWorkflowName),
                    DimensionFilter.CreateIncludeFilter(PremiumKey, isPreminumQoR),
                    DimensionFilter.CreateIncludeFilter(MarketKey, market) }, 
                    suffix);

            var p95File = GenerateDiffFile(
                "Request Latency",
                SamplingType.Percentile95th,
                "BingPlat_XAP_Plugins_Hk",
                "AppHost.Plugin",
                new List<DimensionFilter>() {
                    DimensionFilter.CreateIncludeFilter(EnvironmentKey, "Xap-Prod-HKG01"),
                    DimensionFilter.CreateIncludeFilter(MachineFunctionKey, MachineFunction),
                    DimensionFilter.CreateExcludeFilter(NodeKey, string.Empty),
                    DimensionFilter.CreateIncludeFilter(WorkflowKey, DefaultWorkflowName),
                    DimensionFilter.CreateIncludeFilter(PremiumKey, isPreminumQoR),
                    DimensionFilter.CreateIncludeFilter(MarketKey, market) },
                "BingPlat_XAP_Plugins_Pu",
                "AppHost.Plugin",
                new List<DimensionFilter>() {
                    DimensionFilter.CreateIncludeFilter(EnvironmentKey, "Xap-Prod-PUSE01"),
                    DimensionFilter.CreateIncludeFilter(MachineFunctionKey, MachineFunction),
                    DimensionFilter.CreateExcludeFilter(NodeKey, string.Empty),
                    DimensionFilter.CreateIncludeFilter(WorkflowKey, DefaultWorkflowName),
                    DimensionFilter.CreateIncludeFilter(PremiumKey, isPreminumQoR),
                    DimensionFilter.CreateIncludeFilter(MarketKey, market) },
                    suffix);
            
            var countFile = GenerateSingleValeFile(
                "BingPlat_XAP_Plugins_Pu",
                "AppHost.Plugin",
                "Request Latency",
                SamplingType.Count,
                new List<DimensionFilter>() {
                    DimensionFilter.CreateIncludeFilter(EnvironmentKey, "Xap-Prod-PUSE01"),
                    DimensionFilter.CreateIncludeFilter(MachineFunctionKey, MachineFunction),
                    DimensionFilter.CreateExcludeFilter(NodeKey, string.Empty),
                    DimensionFilter.CreateIncludeFilter(WorkflowKey, DefaultWorkflowName),
                    DimensionFilter.CreateIncludeFilter(PremiumKey, isPreminumQoR),
                    DimensionFilter.CreateIncludeFilter(MarketKey, market) },
                    suffix);

            var cpNodeListFile = GenerateSingleValeFile(
                "BingPlat_XAP",
                "AppHost.Workflow",
                "Critical Path node latency with weight(Node Latency * In CP Percentage)",
                SamplingType.Average,
                new List<DimensionFilter>() {
                    DimensionFilter.CreateIncludeFilter(EnvironmentKey, "Xap-Prod-PUSE01"),
                    DimensionFilter.CreateIncludeFilter(MachineFunctionKey, MachineFunction),
                    DimensionFilter.CreateExcludeFilter(NodeKey, string.Empty),
                    DimensionFilter.CreateIncludeFilter(WorkflowKey, DefaultWorkflowName) },
                    suffix);

            Dictionary<string, double> p95Dic = new Dictionary<string, double>();
            Dictionary<string, double> averageDic = new Dictionary<string, double>();
            Dictionary<string, double> countDic = new Dictionary<string, double>();
            Dictionary<string, double> cpDic = new Dictionary<string, double>();
            Dictionary<string, string> ownerDic = new Dictionary<string, string>();
            Dictionary<string, Tuple<double, double, string, double, string>> summaryDic = new Dictionary<string, Tuple<double, double, string, double, string>>();

            foreach (var line in File.ReadAllLines(p95File))
            {
                if (!line.StartsWith("Node") && !line.StartsWith("Column"))
                {
                    var parts = line.Split(',');
                    p95Dic.Add(parts[0], double.Parse(parts[1]));
                }
            }

            foreach (var line in File.ReadAllLines(avgFile))
            {
                if (!line.StartsWith("Node") && !line.StartsWith("Column"))
                {
                    var parts = line.Split(',');
                    averageDic.Add(parts[0], double.Parse(parts[1]));
                }
            }

            foreach (var line in File.ReadAllLines(countFile))
            {
                if (!line.StartsWith("Node") && !line.StartsWith("Column"))
                {
                    var parts = line.Split(',');
                    countDic.Add(parts[0], double.Parse(parts[1]));
                }
            }

            foreach (var line in File.ReadAllLines(cpNodeListFile))
            {
                if (!line.StartsWith("Node") && !line.StartsWith("Column"))
                {
                    var parts = line.Split(',');
                    var value = double.Parse(parts[1]);
                    if (value > 0)
                    {
                        cpDic.Add(parts[0], value);
                    }
                }
            }

            foreach (var line in File.ReadAllLines(@"D:\BusanMigration\RegressedNodesInBusan.txt"))
            {
                if (!line.StartsWith("Node") && !line.StartsWith("Column"))
                {
                    var parts = line.Split(',');
                    var owner = $"{parts[5]},{parts[6]},{parts[7]},{parts[8]},{parts[9]}";
                    var nodePrefix = parts[0].Split('.')[0];
                    if (!string.IsNullOrWhiteSpace(owner) && !ownerDic.ContainsKey(nodePrefix))
                    {
                        ownerDic.Add(nodePrefix, owner);
                    }

                    summaryDic.Add(parts[0], new Tuple<double, double, string, double, string>(-100, -100, parts[3], -100, owner));
                }
            }

            foreach (var item in p95Dic)
            {
                if (item.Value > 1)
                {
                    if (summaryDic.ContainsKey(item.Key))
                    {
                        var value = summaryDic[item.Key];
                        summaryDic[item.Key] = new Tuple<double, double, string, double, string>(item.Value, value.Item2, value.Item3, value.Item4, value.Item5);
                    }
                    else
                    {
                        summaryDic.Add(item.Key, new Tuple<double, double, string, double, string>(item.Value, -100, String.Empty, -100, String.Empty));
                    }
                }
            }

            foreach (var item in averageDic)
            {
                if (item.Value > 1)
                {
                    if (summaryDic.ContainsKey(item.Key))
                    {
                        var value = summaryDic[item.Key];
                        summaryDic[item.Key] = new Tuple<double, double, string, double, string>(value.Item1, item.Value, value.Item3, value.Item4, value.Item5);
                    }
                    else
                    {
                        summaryDic.Add(item.Key, new Tuple<double, double, string, double, string>(-100, item.Value, String.Empty, -100, String.Empty));
                    }
                }
            }

            var max = countDic.Values.Max();

            foreach (var item in countDic)
            {
                if (summaryDic.ContainsKey(item.Key))
                {
                    var frequency = item.Value / max;
                    var value = summaryDic[item.Key];

                    summaryDic[item.Key] = new Tuple<double, double, string, double, string>(value.Item1, value.Item2, frequency.ToString("0.##%"), value.Item1 * frequency, value.Item5);
                }
            }

            // try fill the owner information
            foreach (var item in new Dictionary<string, Tuple<double, double, string, double, string>>(summaryDic))
            {
                if (string.IsNullOrWhiteSpace(item.Value.Item5))
                {
                    var nodePrefix = item.Key.Split('.')[0];
                    if (ownerDic.ContainsKey(nodePrefix))
                    {
                        summaryDic[item.Key] = new Tuple<double, double, string, double, string>(item.Value.Item1, item.Value.Item2, item.Value.Item3, item.Value.Item4, ownerDic[nodePrefix]);
                    }
                }
            }

            // try to fill CP percentage information
            var cpPercentageDic = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(@"D:\BusanMigration\nodeincppercentage.txt"))
            {
                cpPercentageDic.Add(line.Split(',')[0], line.Split(',')[1]);
            }

            File.AppendAllLines($"D:\\BusanMigration\\LatencyRegression_FinalResult_{suffix}.csv",
                summaryDic.Where(o => o.Value.Item1 > -99 && o.Value.Item2 > -99).
                Select(o => {
                    var cpPercentage = cpPercentageDic.ContainsKey(o.Key) ? (double.Parse(cpPercentageDic[o.Key]) / 100).ToString("0.##%") : "0.0%";
                    var cpLatencyInBusan = cpDic.ContainsKey(o.Key) ? cpDic[o.Key].ToString("0.##") : "0";
                    return $"{o.Key},{cpLatencyInBusan},{cpPercentage},{o.Value.Item1.ToString("0.##")},{o.Value.Item2.ToString("0.##")},{o.Value.Item3},{o.Value.Item4.ToString("0.##")},{o.Value.Item5}";
                }));
        }

        private static string GenerateSingleValeFile(string account, string nameSpace, string metric, SamplingType samplingType, List<DimensionFilter> dimensionFilters, string suffix)
        {
            MetricReader reader;

            try
            {
                reader = new MetricReader(ConnectionInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get MetricReader failed with exception: {ex}");
                throw ex;
            }

            var mdmCounter = new MetricIdentifier(account, nameSpace, metric);

            var value = reader.GetTimeSeriesAsync(
                mdmCounter,
                dimensionFilters,
                DateTime.Now.AddHours(StartHours).ToUniversalTime(),
                DateTime.Now.AddHours(EndHours).ToUniversalTime(),
                new[]
                {
                    samplingType
                });

            var result = new List<string>();
            foreach (var nodeResult in value.Result.Results)
            {
                var valueList = nodeResult.GetTimeSeriesValues(samplingType).Where(o => !o.Equals(double.NaN));
                var average = valueList.Any() ? valueList.Average() :0;
                result.Add($"{nodeResult.DimensionList.Where(dimensionFilter => dimensionFilter.Key == NodeKey).First().Value},{average}");
            }

            var fileName = $"D:\\BusanMigration\\LatencyRegression_{metric.Replace("*", "")}_{samplingType}_{suffix}.csv";
            File.WriteAllLines(fileName, result);
            return fileName;
        }

        private static string GenerateDiffFile(string metric, SamplingType samplingType, string controlAccount, string controlNameSpace, List<DimensionFilter> controlDimensionFilters,
            string treatmentAccount, string treatmentNameSpace, List<DimensionFilter> treatmentDimensionFilters, string resultFileSuffix)
        {
            MetricReader reader;

            try
            {
                reader = new MetricReader(ConnectionInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get MetricReader failed with exception: {ex}");
                throw ex;
            }

            var controlMdmCounter = new MetricIdentifier(controlAccount, controlNameSpace, metric);

            var controlValue = reader.GetTimeSeriesAsync(
                controlMdmCounter,
                controlDimensionFilters,
                DateTime.Now.AddHours(StartHours).ToUniversalTime(),
                DateTime.Now.AddHours(EndHours).ToUniversalTime(),
                new[]
                {
                    samplingType
                });

            var treatmentMdmCounter = new MetricIdentifier(treatmentAccount, treatmentNameSpace, metric);

            var treatmentValue = reader.GetTimeSeriesAsync(
                treatmentMdmCounter,
                treatmentDimensionFilters,
                DateTime.Now.AddHours(StartHours).ToUniversalTime(),
                DateTime.Now.AddHours(EndHours).ToUniversalTime(),
                new[]
                {
                    samplingType
                });

            Dictionary<string, double> nodeInHK = new Dictionary<string, double>();
            Dictionary<string, double> nodeInPU = new Dictionary<string, double>();

            foreach (var nodeResult in controlValue.Result.Results)
            {
                var nodeName = nodeResult.DimensionList.Where(dimensionFilter => dimensionFilter.Key == NodeKey).First().Value;
                var valueList = nodeResult.GetTimeSeriesValues(samplingType).Where(o => !o.Equals(double.NaN));
                if(valueList.Any())
                {
                    var nodeLatency = valueList.Average();
                    nodeInHK.Add(nodeName, nodeLatency);
                }
            }

            foreach (var nodeResult in treatmentValue.Result.Results)
            {
                var nodeName = nodeResult.DimensionList.Where(dimensionFilter => dimensionFilter.Key == NodeKey).First().Value;
                var valueList = nodeResult.GetTimeSeriesValues(samplingType).Where(o => !o.Equals(double.NaN));
                if (valueList.Any())
                {
                    var nodeLatency = valueList.Average();
                    nodeInPU.Add(nodeName, nodeLatency);
                }
            }

            var result = new List<string>();

            foreach(var nodeItem in nodeInPU)
            {
                if(nodeInHK.ContainsKey(nodeItem.Key) && (nodeInPU[nodeItem.Key] - nodeInHK[nodeItem.Key] > 1))
                {
                    result.Add($"{nodeItem.Key},{nodeInPU[nodeItem.Key] - nodeInHK[nodeItem.Key]}");
                }
            }

            var fileName = $"D:\\BusanMigration\\LatencyRegression_{metric}_{samplingType}_{resultFileSuffix}.csv";
            File.WriteAllLines(fileName, result);
            return fileName;
        }
    }
}
