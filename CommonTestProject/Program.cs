namespace CommonTestProject
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Timer = System.Threading.Timer;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.Services.Common;
    using System.Linq;
    using System.Reflection;
    using BenchmarkDotNet.Jobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.Services.Common.CommandLine;
    using BenchmarkDotNet.Configs;
    using Microsoft.TeamFoundation.WorkItemTracking.Client;

    class Program
    {
        private const string DefaultEnvironmentVariableList = "";
        private const char EnvironmentVariableSplitter = '`';
        private const char EnvironmentVariableNameValueSplitter = ':';
        private const char EnvironmentVariableValueSplitter = ';';
        const int MaximumAuditWorkflowTimeStamp = 100;
        static void Main(string[] args)
        {
            //TestStringSplitter();
            TestGCConfig(int.Parse(args[0]), int.Parse(args[1]));
            //TestSplitter();
            //TestPluginConfig();
            //TestConcurrentQueue();

            //TestCPUSlice();
            //TestQueue();
            //TestThreadPoolPriority();
            //TestFunc1();
            //TestFunc2();

            //ExtractQuerySendingResponseFromBltFile(); 

            //GetSummaryReportForTraffic();

            //Console.WriteLine(System.Environment.ProcessorCount);
            //TestTimer();

            //TestDictionaryCapacity();
            //TestDictionaryCopy();
            Console.WriteLine("!!!");
            Console.ReadLine();
        }

        private static void TestStringSplitter()
        {
            Dictionary<string, List<string>> EnvironmentVarialeConfigDic = new Dictionary<string, List<string>>();
            var config = "DOTNET_GCHeapAffinitizeRanges:0:0-31,1:0-31;0:32-63,1:32-63";
            var secs = config.Split(EnvironmentVariableSplitter);
            foreach (var variable in secs)
            {
                var index = variable.IndexOf(EnvironmentVariableNameValueSplitter);
                var name = variable.Substring(0, index);
                if (index < variable.Length - 1)
                {
                    var valueList = variable.Substring(index + 1).Split(EnvironmentVariableValueSplitter);
                    EnvironmentVarialeConfigDic.Add(name, valueList.ToList());
                }
                else
                {
                    EnvironmentVarialeConfigDic.Add(name, new List<string>());
                }
            }
        }

        private static void TestGCConfig(int nodeId, int threadCount)
        {
            Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCCpuGroup", "0");
            Environment.SetEnvironmentVariable("DOTNET_Thread_UseAllCpuGroups", "0");
            //Environment.SetEnvironmentVariable("DOTNET_GCCpuGroup", "1"); 
            //Environment.SetEnvironmentVariable("DOTNET_GCHeapAffinitizeRanges", "0:32-63,1:32-63");
            /*var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = @"CommonTestProjectNetCore.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = $"{threadCount}",
            };*/
            var p = new Process();
            var processStartTime = DateTime.UtcNow;
            p.StartInfo = new ProcessStartInfo
            {
                FileName = @"cmd.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = $"/C \"start /Node {nodeId} CommonTestProjectNetCore.exe {threadCount}\"",
            };

            p.Start();
            Console.WriteLine(p.StartTime.ToUniversalTime());

            while ((DateTime.UtcNow - processStartTime) < TimeSpan.FromMinutes(1))
            {
                var list = Process.GetProcessesByName("CommonTestProjectNetCore");
                foreach (var item in list)
                {
                    if (item.StartTime.ToUniversalTime() > processStartTime)
                    {
                        int group = nodeId / 4;
                        long mask = 0x0FFFFFFFF;
                        item.ProcessorAffinity = (System.IntPtr)(mask << (group*32));
                        Console.WriteLine("!!!" + item.Id);
                        break;
                    }
                }
            }
        }

        private static void TestSplitter()
        {
            Console.WriteLine(Path.DirectorySeparatorChar);
        }

        private static void TestPluginConfig()
        {
            var dic = new Dictionary<string, List<string>>();
            foreach (var item in File.ReadAllLines(@"D:\Data\SharedProd\exp.resource.hardlink.manifest"))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var secs = item.Split(',');
                    var key = secs[1].Substring(4);
                    var value = secs[0];
                    if (dic.ContainsKey(key))
                    {
                        dic[key].Add(value);
                    }
                    else
                    {
                        dic.Add(key, new List<string> { value });
                    }
                }
            }

            ConcurrentBag<string> list = new ConcurrentBag<string>();
            ConcurrentBag<string> uniquelist = new ConcurrentBag<string>();
            Parallel.ForEach(Directory.GetFiles(@"D:\Data\SharedProd\RCache\Ini"),
                o =>
                {
                    var content = File.ReadAllText(o);
                    if (!content.Contains("&") && !content.Contains("$"))
                    {
                        var path = Path.GetFileName(o);
                        foreach (var file in dic[path])
                        {
                            list.Add($"{file}\t{path}\t{content.Length}");
                        }

                        uniquelist.Add($"{path}\t{content.Length}");
                    }
                });

            File.WriteAllLines(@"D:\Data\SharedProd\result.txt", list);
            File.WriteAllLines(@"D:\Data\SharedProd\uniqueresult.txt", uniquelist);
        }

        private static void TestConcurrentQueue()
        {
            var concurrentQueue = new ConcurrentQueue<int>();
            for(int i =0; i<100;i++)
            {
                Task.Factory.StartNew(() =>
                {
                    while(true)
                    {
                        if (concurrentQueue.Count < MaximumAuditWorkflowTimeStamp)
                        {
                            concurrentQueue.Enqueue(i);
                        }
                        else
                        {
                            concurrentQueue.TryDequeue(out var _);
                            concurrentQueue.Enqueue(i);
                        }

                        Console.WriteLine($"{DateTime.Now}: {concurrentQueue.Count}");
                    }
                });
            }

        }

        private static void TestCPUSlice()
        {
            var start = DateTime.Now;
            for(int i=0;i<100;i++)
            {
                Thread.Sleep(1);
            }
            Console.WriteLine((DateTime.Now - start).TotalMilliseconds);
        }

        private static async Task TestTaskReturn()
        {
            await RunTask();
        }

        private static async Task RunTask()
        {
            var result = await Task.Run<object>(() => { return new object(); });
            Console.WriteLine(result);
        }

        private static void TestQueue()
        {
            var queue = new Queue<int>();
            for(int i = 0; i < 10; i++)
            {
                queue.Enqueue(i);
            }

            Console.WriteLine(queue.Peek());
        }

        private static Assembly LoadHandlerAssembly_UnderLock(Assembly thisAssembly, string embeddedAssemblyName)
        {
            Assembly embeddedAssembly;
            using (Stream manifestResourceStream = thisAssembly.GetManifestResourceStream(embeddedAssemblyName))
            {
                if (manifestResourceStream == null)
                {
                    throw new InvalidDataException($"Could not load the manifest resource {embeddedAssemblyName} " +
                        $"from [{thisAssembly.FullName}]. Here are the types in the manifest:\n" +
                        $"{string.Join(", ", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
                }

                if (manifestResourceStream.Length == 0)
                {
                    throw new InvalidDataException($"Could not load get a bytestream from the manifest resource " +
                        $"stream {embeddedAssemblyName}. The length of the stream was 0.");
                }

                byte[] assemblyData = new byte[manifestResourceStream.Length];

                manifestResourceStream.Read(assemblyData, 0, assemblyData.Length);
                embeddedAssembly = Assembly.Load(assemblyData);
                File.WriteAllLines(@"D:\new.txt", assemblyData.Select(o=>o.ToString()));
                if (embeddedAssembly == null)
                {
                    // The .NET docs for Assembly.Load(byte[]) do not mention that it could return null.
                    // However, other similar overloads sometimes return null so better safe than sorry.
                    throw new InvalidDataException($"Loading embedded assembly {embeddedAssemblyName} failed; " +
                        $"Assembly.Load(byte[]) returned null (stream length: {assemblyData.Length})");
                }
            }

            return embeddedAssembly;
        }

        private static void TestDictionaryCopy()
        {
            string baseKey = "bbbbbb";
            string baseValue = "nnnnnnn";

            string[] stringKeys = new string[10000000];
            for (int i = 0; i < stringKeys.Length; i++)
            {
                stringKeys[i] = baseKey + i;
            }

            string[] stringValues = new string[10000000];
            for (int i = 0; i < stringValues.Length; i++)
            {
                stringValues[i] = baseValue + i;
            }

            var dic = new Dictionary<string, string>(10000000, StringComparer.OrdinalIgnoreCase);
            var dic2 = new Dictionary<string, string>(10000000, StringComparer.OrdinalIgnoreCase);

            for (uint i = 0; i < 10000000; i++)
            {
                dic.Add(stringKeys[i], stringValues[i]);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 10; i++)
            {
                foreach (var item in dic)
                {
                    dic2.Add(item.Key, item.Value);
                }
                dic2.Clear();
            }
            

            watch.Stop();
            Console.WriteLine(watch.Elapsed.TotalMilliseconds);

            Stopwatch watch2 = new Stopwatch();
            watch2.Start();


            for (int i = 0; i < 10; i++)
            {
                dic.Copy(dic2);
                dic2.Clear();
            }

            watch2.Stop();
            Console.WriteLine(watch2.Elapsed.TotalMilliseconds);

            Console.ReadLine();
        }

        private static void TestDictionaryCapacity()
        {
            string baseKey = "bbbbbb";
            string baseValue = "nnnnnnn";

            string[] stringKeys = new string[10000000];
            for (int i = 0; i < stringKeys.Length; i++)
            {
                stringKeys[i] = baseKey + i;
            }

            string[] stringValues = new string[10000000];
            for (int i = 0; i < stringValues.Length; i++)
            {
                stringValues[i] = baseValue + i;
            }

            var dic = new Dictionary<string, string>(10000000, StringComparer.OrdinalIgnoreCase);

            for (uint i = 0; i < 10000000; i++)
            {
                dic.Add(stringKeys[i], stringValues[i]);
            }

            dic.Clear();

            for (uint i = 0; i < 10000000; i++)
            {
                dic.Add(stringKeys[i], stringValues[i]);
            }

            dic.Clear();

            for (uint i = 0; i < 10000000; i++)
            {
                dic.Add(stringKeys[i], stringValues[i]);
            }

            dic.Clear();

            for (uint i = 0; i < 10000000; i++)
            {
                dic.Add(stringKeys[i], stringValues[i]);
            }

            dic.Clear();

            for (uint i = 0; i < 10000000; i++)
            {
                dic.Add(stringKeys[i], stringValues[i]);
            }

            Console.ReadLine();
        }

        private static void GetSummaryReportForTraffic()
        {
            var result = new Dictionary<string, Tuple<HashSet<string>, HashSet<string>>>();
            foreach(var line in File.ReadAllLines(@"D:\BLT\querylist.txt"))
            {
                var sec = line.Split('\t');
                if(sec.Length == 3)
                {
                    if (result.ContainsKey(sec[2]))
                    {
                        if (result[sec[2]].Item1.Contains(sec[1]))
                        {
                            result[sec[2]].Item2.Add(sec[0]);
                        }
                        else
                        {
                            result[sec[2]].Item1.Add(sec[1]);
                            result[sec[2]].Item2.Add(sec[0]);
                        }    
                    }
                    else
                    {
                        result.Add(sec[2], new Tuple<HashSet<string>, HashSet<string>>(new HashSet<string>() { sec[1] }, new HashSet<string>() { sec[0] }));
                    }
                }
            }

            File.WriteAllLines(@"D:\BLT\result.txt", result.Select(o=>$"{o.Key}\t{string.Join(",", o.Value.Item1)}\t{string.Join(",", o.Value.Item2)}"));
        } 

        private static void ExtractQuerySendingResponseFromBltFile()
        {
            List<string> result = new List<string>(20000);
            foreach(var line in File.ReadLines(@"D:\Blt\6.txt"))
            {
                if(line.Contains("QueryResponseSendComplete"))
                {
                    result.Add(line);
                }
            }

            File.WriteAllLines(@"D:\Blt\7.txt", result);
        }

        static void Func1(List<string> input)
        {
            input[0] = "a";
            input[1] = "a";
            input[2] = "a";
            input[3] = "a";
            input[4] = "a";
        }

        private static void TestFunc2()
        {
            /*string value = "USB.Main:6,AutoSuggest.Qsonhs.Suggestions:3,Xap.BingFirstPageResults:2,Halsey.Proactive.Workflow:2,Halsey.Notebook.TestHookWorkflow:2,CU.DialogEngine.Next.WinPhoneCoreWorkflow:2,Assistant.BingFirstPageResults:2,Retail.EntityCategoryPage:2,EntityApi.Main:2,Xap.BingOtherPageResultsEnpage:2,Xap.FirstPageResultsWithAdsForBots:2,CortanaAssist.TopLevelWorkflow:2,Xap.YahooFirstPageResultsWithAds:2,Xap.CarouselListingResults:2,Widget.Insights.BfprWorkflowV2:2,Recommendations.Windows.NominatedApps:2,Halsey.ProfileV2.Service.Workflow:2,AutoSuggest.SearchCharm.Windows81S14.Suggestions:2,Halsey.ProfileV2.UserFeature.Workflow:2,Xap.YahooImageResultsPlus:2,Xap.YahooFirstPageResults:2,Recommendations.WinPhone.NominatedApps:2,AssistantRegional.BingFirstPageResults:2,Xap.BingOtherPageImageResults:2,AutoSuggest.PageZero.Suggestions:2,EventsCatalog.EventsCatalogWorkflow:2,Xap.YahooWebOnlyWithAds:2,Widget.Insights.BfprRegionalWorkflowV2:2,Xap.BingOtherPageResults:2,Xap.BingImageResults:2,Xap.BingMapsFirstPage:2,MapsAutoSuggest.Workflow:2,Xap.BingVideoResults:2,Local.LocationUnderstanding:2,Xap.Service.CardReplace.Workflow:2,Halsey.Profile.SnRWrapper.InterestWorkflow:2,URLPreview.BingVideoResults:2,Xap.YahooAdsSyndicationResults:2,Xap.BingFirstPageResultsEnpage:2,Microsoft.Bing.XapPlugin.Bingbox.NoQueryCoreWorkflow:2,Xap.BingOtherPageResultsForBots:2,Xap.NewsVerticalFirstPage:2,Halsey.ProfileV2.Experience.Workflow:2,RelativityAPI.CoreWorkflow:2,AutoSuggest.OpenSearch.Suggestions:2,HomepageModulesWithQuiz.Proactive.Workflow:2,Xap.BingFirstPageResultsWithWebOnlyInterleaving:2,Halsey.Profile.SnRWrapper.ResetProfileWorkflow:2,WinSearchCharm.BingFirstPageResults:2,Xap.YahooSpeller:2,Xap.NewsVertical:2,AutoSuggest.Vnext.Standard.Suggestions:2,Halsey.Profile.SnRWrapper.OOBEWorkflow:2,Xap.Service.OAuth.AuthCompleteWorkflow:2,Multimedia.ImageInsights:2,Xap.YahooAdsResults:2,AutoSuggest.Vnext.QSA.Suggestions:2,Ads.PaidSearchForNative.PaidSearchForNativeMainV2:2,Halsey.Proactive.TripPlanner.TaskLineWorkflow:2,Multimedia.WatchCanvas:2,Ads.ClickResolution.ResolveAndWriteClickWorkflow:2,UniversalSearchBox.TextOnly.Suggestions:2,Xap.YahooOtherPageResultsWithAds:2,SparkV2.Reactive.Warmup.Workflow:2,Halsey.Spark.Workflow:2,AutoSuggest.Legacy.Suggestions:2,Local.QueryUnderstanding:2,Maps.BingFirstPageResults:2,Local.MicroPOI:2,Halsey.Profile.SnRWrapper.OptInWorkflow:2,UniversalSearchBox.WindowsPhone.Suggestions:2,Xap.Service.FreqSearches.Library.RankRepeat:2,Xap.BingOtherPageVideoResults:2,Multimedia.ImageLandingPage:2,SerpApi.AdsOnlySyndicationApi.Workflow:2,Xap.YahooWebOnly:2,Multimedia.ImageInsightsBatch:2,UniversalSearchBox.IE12.Suggestions:2,AutoSuggest.Anaheim.Suggestions:2,UniversalSearchBox.Windows10M2.Suggestions:2,AutoSuggest.English.Suggestions:2,Xap.OmaClickTracking:2";

            string result = string.Empty;
            foreach(var item in value.Split(','))
            {
                result += item.Split(':')[0] + ":" + int.Parse(item.Split(':')[1])*2 + ",";
            }*/


            /*File.WriteAllText(@"D:\osresult2.txt", "");
            object lockObj = new object();
            Parallel.ForEach(File.ReadAllLines(@"D:\2.txt"), line =>
            {
                var parts = line.Split(',');
                foreach (var part in parts)
                {
                    if(part.StartsWith("D:") && int.TryParse(part.Substring(2), out var value) && value>1)
                    {
                        lock(lockObj)
                        {
                            File.AppendAllText(@"D:\osresult2.txt", line); 
                            File.AppendAllText(@"D:\osresult2.txt", "\r\n");
                        }

                        break;
                    }
                }
            });
            File.WriteAllText(@"D:\osresult3.txt", "");
            object lockObj = new object();
            foreach(var line in File.ReadAllLines(@"D:\3.txt"))
            {
                    if (line.Contains("GC/"))
                    {
                            File.AppendAllText(@"D:\osresult3.txt", line);
                            File.AppendAllText(@"D:\osresult3.txt", "\r\n");
                    }
            }*/
            /*Console.WriteLine(Environment.Version.ToString());
            string[] listfiles = { };

            var list = new List<string>();
            foreach (var file in Directory.EnumerateFiles(@"D:\test1\NCS"))
            {
                list = list.Union(File.ReadAllLines(file)).ToList();
            }

            list.RemoveAll(o => string.IsNullOrWhiteSpace(o));
            list = list.Select(o => o.Split('\t')[0]).Distinct().ToList();
            const string format =
            @"<TestFailureDetails xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/bing/spatialdata/activity"">
    <AddonId>NCS</AddonId>
    <Details>Signoff by liagao [Reason = 'Legacy failure']</Details>
    <ExperimentName>bilbo</ExperimentName>
    <ExperimentVersion xmlns:d2p1=""http://schemas.datacontract.org/2004/07/System"">
        <d2p1:_Build>-1</d2p1:_Build>
        <d2p1:_Major>20</d2p1:_Major>
        <d2p1:_Minor>30000</d2p1:_Minor>
        <d2p1:_Revision>-1</d2p1:_Revision>
    </ExperimentVersion>
    <ExpireDateUtc>9999-12-31T23:59:59.9999999</ExpireDateUtc>
    <IsActive>true</IsActive>
    <IsIsolateFg>true</IsIsolateFg>
    <ModifiedUtc>2018-04-17T16:38:10.3016691Z</ModifiedUtc>
    <Owner>liagao</Owner>
    <Status>StaleTest</Status>
    <TestName>{0}</TestName>
</TestFailureDetails>";
            List<string> output =new List<string>();
            foreach(var testName in list)
            {
                output.Add(string.Format(format, testName));
            }

            File.WriteAllLines(@"D:\test1\output.txt", output);
            Console.WriteLine("LegacyCount: " + list.Count);
            /*var vipmachineList = File.ReadAllLines("D:\\vipmachines.txt");
            var machines = File.ReadAllLines("D:\\machines.txt");

            var newList = new List<string>();
            int count = 0;
            foreach(var machine in machines)
            {
                bool contain = false;
                foreach(var vip in vipmachineList)
                {
                    if(vip.Contains(machine))
                    {
                        contain = true;
                        break;
                    }
                }

                if(!contain)
                {
                    string sub = count <= 9 ? "0" : "1";
                    newList.Add($"<VipMachineDetail vip_dip=\"CO4PERFVIP1{sub}_{count % 10}\" vip=\"CO4PERFVIP1{sub}\" dip=\"{machine}\" environment =\"xap-prod-co4\" />");
                    count++;
                }
            }

            File.WriteAllLines("D:\\result.txt", newList);
            var submitFunctionalGateRunStatus = new FunctionalGateStatus() { };
            var str = new JavaScriptSerializer().Serialize(submitFunctionalGateRunStatus);
            Console.WriteLine();
            MemoryStream ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms))
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(FunctionalGateStatus));
                //DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(FunctionalGateStatus));
                ser.WriteObject(writer, submitFunctionalGateRunStatus);
                writer.Flush();
                ms.Position = 0;
                //var str2 = new StreamReader(ms).ReadToEnd();
                var obj = (FunctionalGateStatus)ser.ReadObject(ms);
                Console.WriteLine(obj.AnalysisTimes);
            }*/


            /*Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));
            Console.WriteLine("BFPR_20.123_1678965".GetHashCode().ToString("X8"));*/
            /*Dictionary<string, int> dic = new Dictionary<string, int>();
            bool success = true;
            var errors = new Collection<string>();
            var defaultValue = LoadRequestContextPoolSetting("test", @"0,AutoSuggest.Qsonhs.Suggestions:6,USB.Main:6,UniversalSearchBox.IE12.Suggestions:2,AutoSuggest.OpenSearch.Suggestions:2,AutoSuggest.English.Suggestions:2,UniversalSearchBox.Windows10M2.Suggestions:2,Xap.BingOtherPageResultsRegional:2,Xap.BingFirstPageResultsRegional:3,UniversalSearchBox.TextOnly.Suggestions:1,Xap.BingImageResultsRegional:1,Xap.Service.FreqSearches.Library.RankRepeat:1,Xap.BingFirstPageResults:3,Ads.IPv6Mapping.MainWorkflow:1,Halsey.Spark.Workflow:1,Halsey.ProfileV2.ConfigStore.MetadataReadWorkflow:1,Microsoft.Bing.Cloud.Graph.Workflows.SyncTTL:1,AutoSuggest.Vnext.Standard.Suggestions:1,Multimedia.ImageInsights:1,MsnJVEnglish.BingImageResults:1,Halsey.ConnectedServices.ReadListWorkflow:1,Xap.BingImageResults:1,Xap.Service.TokenRetrieve.ExtHttpAccessWorkflow:1,Xap.NotificationTracking:1,MsnJVEnglish.MMAutoSuggest:1,ExtendedActions.DataAccess.GetUserConsentedSkillMetadataWorkflow:1,Xap.BingOtherPageImageResults:1,MsnJVEnglish.ImageInsights:1,Xap.Service.OAuth.CreateSilentAuthLinkSSOTLWorkflow:1,AutoSuggest.BingDict.Suggestions:1,Xap.BingOtherPageResults:1,Xap.BingFirstPageResultsWithWebOnlyInterleaving:1,MsnJVBingKnows.BingFirstPageResultsRegionalWithQueryAlteration:1,Xap.MsnJVCounting:1,DreamMap.CoverStory:1,Halsey.ProfileV2.Service.Workflow:1,Multimedia.Favorite.MainWorkflow:1,Halsey.Proactive.WorkflowZhCN:1,AutoSuggest.SearchCharm.Windows81S14.Suggestions:1,Halsey.Proactive.Workflow:1,Multimedia.ImageLandingPage:1,Xap.BingVideoResults:1,Xap.CarouselListingResults:1,Multimedia.WatchCanvas:1,Xap.HomepageModules:1,UniversalSearchBox.WindowsPhone.Suggestions:1,Cricket.AjaxWorkflow:1,WindowsPhone8.BingFirstPageResults:1,Xap.HistoryHandler:1,AutoSuggest.Vnext.QSA.Suggestions:1,Xap.BingFirstPageResultsEnpage:1,MsnJVEnglish.BingOtherPageImageResults:1,AssistantRegional.BingFirstPageResults:1,Halsey.ProfileV2.UserFeature.Workflow:1,Xap.Service.SearchHistory.Library.ResolveSearchHistoryImpression:1,Xap.Service.Quiz.Workflow.HPQuizContentApiWrapper:1,Multimedia.ImageInsightsBatch:1,Widget.Insights.BfprWorkflowV2:1,MsnJVEnglish.HomepageModules:1,Xap.BingVideoDetails:1,Xap.BingVideoResultsRegionalZhCn:1,Xap.BingOtherPageResultsNoQuerySuggestions:1,Assistant.BingFirstPageResults:1,AutoSuggest.SearchCharm.Windows81.Suggestions:1,Xap.MsnJVDataAnswerBing123:1,Xap.CachedContentResults:1,Halsey.LiveTile.WorkflowZhCN:1,Microsoft.Bing.Cloud.Graph.Workflows.CollectionsServe:1,Halsey.ProfileV2.Experience.Workflow:1,Xap.BingOtherPageVideoResults:1,Rap.BingFirstPageResults:1,MsnJVAcademic.BingFirstPageResultsRegional:1,Multimedia.ImageLandingPage.PreferencesWriteWorkflow:1,Halsey.LiveTile.Workflow:1,Dialog.API.WorkflowV2:1,MsnJVEnglish.BingVideoResults:1,WinSearchCharm.BingFirstPageResults:1,Xap.News.TrendingTopicsAPI:1,Xap.BingOtherPageResultsEnpage:1,CaptionsPlugins.Workflow_WikipediaGoBigAsync:1,HomepageModulesWithQuiz.Proactive.Workflow:1,Lists.AjaxWorkflow:1,Widget.Insights.BfprRegionalWorkflowV2:1,Multimedia.Favorite.SavesWorkflow:1,SparkV2.Notebook.Workflow:1,PersonalDataPlatform.GeneralWorkflow:1,WinSearchThreshold.SearchMyStuff:1,SparkV2.L2TopLevelWorkflow:1,PWILO.API.Clear.ClearActivitiesWorkflow:1,WinSearchCharm.BingOtherPageResults:1,Cortana.SkillsKit.GetConnectedServiceListWorkflow:1,Xap.Service.OAuth.AuthCompleteWorkflow:1,Halsey.ProfileV2.Experience.SnRWrapper.Workflow:1,Xap.TaskpaneFetch:1,Xap.CnHomePageModules:1,XiaoIce.ChatWorkflow:1,Multimedia.VideoLandingPage:1,Rap.BingOtherPageResults:1,SportsLite.AjaxUpdateWorkflow:1,UniversalSearchBox.Api.Suggestions:1,CaptionsPlugins.Workflow_RichCardAsync:1", dic, ref success, errors);
            */

            //TestTaskException();

            //TestObject obj = new TestObject(){getTime = ()=> DateTime.Now};


            /*Dictionary<string,bool> dipDynamicMap = new Dictionary<string, bool>();
            Dictionary<string, bool> dipSingleMap = new Dictionary<string, bool>();
            List<string> emptyMachineList = File.ReadAllLines(@"D:\3.txt").Where(o => o.Split(new[] {'"'})[1].Length == 15).Select(o=> o.Split(new[] { '"' })[1]).ToList();

            foreach (var line in File.ReadAllLines(@"D:\1.txt"))
            {
                string[] s = line.Split(new[] { "\"" }, StringSplitOptions.RemoveEmptyEntries);

                if (s[3].Contains("DynamicFuncVip"))
                {
                    if (dipDynamicMap.ContainsKey(s[5]))
                    {
                        dipDynamicMap[s[5]] = true;
                    }
                    else
                    {
                        dipDynamicMap.Add(s[5], true);
                    }
                }
                else
                {
                    if (dipSingleMap.ContainsKey(s[5]))
                    {
                        dipSingleMap[s[5]] = true;
                    }
                    else
                    {
                        dipSingleMap.Add(s[5], true);
                    }
                }
            }

            File.WriteAllLines(@"D:\6.txt",emptyMachineList.Where(machine=> dipDynamicMap.ContainsKey(machine)).OrderBy(o=>o));
            List<string> result = new List<string>();
            HashSet<string> hash = new HashSet<string>(File.ReadAllLines(@"D:\NewScript\1.txt"));
            foreach (var line in File.ReadAllLines(@"D:\1.txt"))
            {
                string[] s = line.Split(new[] { "\"" }, StringSplitOptions.RemoveEmptyEntries);
                if (hash.Contains(s[5]) && s[1].Contains("DynamicFuncVip"))
                {
                    result.Add(line.Replace(s[5], "Invalid"));
                }
            }

            File.WriteAllLines(@"D:\8.txt", result);*/
            /*List<string> dip = new List<string>();
            foreach (var line in File.ReadAllLines(@"D:\2.txt"))
            {
                if (line.Length < 150)
                {
                    dip.Add(line);
                }
                
            }
            File.WriteAllLines(@"D:\9.txt",dip);

            Console.WriteLine("Done");*/
            /*List<string> dip = new List<string>();
            foreach (var line in File.ReadAllLines(@"D:\1.txt"))
            {
                string[] s = line.Split(new []{"\""}, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine("Add item:" + s[5]);
                dip.Add(s[5]);
            }

            foreach (var item in dip.FindAll(o=>dip.Where(o1=>o==o1).Count() > 4))
            {
                Console.WriteLine(item);
            }*/
            /*List<string> ss= new List<string>(){"1"};
            List<string> ss2 = new List<string>() { "1" };
            Console.WriteLine("33" + string.Join(",", ss.Except(ss2)));*/
            //Console.WriteLine(DateTime.Now.Ticks);
            //TestHttp();

            /*var tasks = new List<Task>();
            var sw = Stopwatch.StartNew();
            foreach (var kvp in new List<string>(){"1", "2", "3", "4"})
            {
                tasks.Add(Task.Factory.StartNew(
                                                () =>
                                                {
                                                    if (kvp == "5")

                                                    {
                                                        throw new ArgumentException("Test argument should no be '1'");
                                                    }
                                                }, TaskCreationOptions.LongRunning));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Finished!");*/
            //TestWhere();
            //TestParallel();
            /*foreach (var file in Directory.GetFiles(@"C:\Users\liagao\Downloads\1\Queries_1_0\"))
            {
                if (file.Contains("Copy"))
                {
                    File.Delete(file);
                }
            }*/
            /*using (var stream = new FileStream("D:\\2.txt", FileMode.Create, FileAccess.ReadWrite))
            {
                StreamWriter ss = new StreamWriter(stream);
                ss.Write("123");
                ss.Flush();
            }

            Console.WriteLine(Path.Combine($"https://123.cockpit.ap.gbl/cockpit/", "file?123123"));*/
            //TestNodeCount();
            //TestProcess();
            //Regex regex = new Regex(@"for folder FILECLOUD~PRODTEST@1\.4 {FILECLOUD~PRODTEST\[1\.4\]{duration: .*?; FsUtils-Exists");
            //Console.WriteLine(regex.IsMatch("for folder FILECLOUD~PRODTEST@1.4 {FILECLOUD~PRODTEST[1.4]{duration: *1/1034s; FsUtils-Exists: 3/0.000127s; "));
            //File.WriteAllLines("D:\\3.txt", File.ReadAllLines("D:\\2.txt").Select(o => double.Parse(o)).OrderBy(o => o).Select(o=>o.ToString()));
            //TestRegex();
            //TestStopWatch();

            //Console.WriteLine(DateTime.Parse("2017/10/17 12:00:00-07"));
            //TestDateTimeUTC();
            //TestStringContact("123", null, null);
            //GetNodeList();
            //Console.WriteLine(GenerateUnflattenedWorkflowFilesZip(@"\\stcasia\root\Backup\XapBeijing\Tools", "testwf.zip", "testwf"));
            /*TestClass a= new TestClass();
            typeof(TestClass).InvokeMember("Print", 
                                                    BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, a,
                                           new object[] {10});

            Console.WriteLine("  123   123   123  ".Split(new string []{" "}, StringSplitOptions.RemoveEmptyEntries).Length);*/
            //Console.WriteLine(Path.GetFileNameWithoutExtension("https://cosmos09.osdinfra.net/cosmos/searchXAP.Prod/local/DataPlatform/xap-dpppe/ExperimentDeploymentMetrics/Frodo_90.987_Xap2-Int-Bn1.txt"));
            //TestMsLevelTimer();
            //TestBoolOrderBy();
            //TestSpecialCharacter();
            //TestSerialize();
            /*Console.WriteLine(IsValidUrl("http://www.example.com"));
            Console.WriteLine(IsValidUrl("javascript:alert('xss')"));
            Console.WriteLine(IsValidUrl("/111/22/33"));
            Console.WriteLine(IsValidUrl("D:/11/22/33"));

            List<int> ss = new List<int> {1,2,3,4};
            Console.WriteLine(ss.Take(2).Count());*/
            //Console.WriteLine(isMatchNodePrefix("A1.1234.123", "A;B"));
            //StackOverflowSearchEngine.GetSearchResult("123");
            //StackOverflowSearchEngine.UpdateXapTagQuestions();
            //TestToStringFormat();
            //TestEncode();
            //TestList();
            //TestSplit();
            //TestGetFullPath();
            //TestDateTime();
            //TestQpsTimer(2);
            //TestMemoryStream();
            //TestTask();
            //TestStreamWriter();
            //TestNode();
            //TestRemoveEvenItems();
            //Console.WriteLine(new HashSet<string>(GenerateStringArray()));

            // Test ConcurrentDictionary
            //TestConcurrentDictionary();
            //TestExcel();

            //TestEnum();

            //TestDeleteDictionaryKey();

            //TestListAny();
            //Console.WriteLine(Path.GetFileName(Path.GetDirectoryName("D:\\data\\id\\123\\1.zip")));
            //TestDictionary();

            //TestDictionary2();
            //Console.WriteLine(Directory.Exists(@"d:\src\xap\private\xap\XapDataPlatform\PerfResultComparator.Library.Test\obj\amd64\031eec27-726e-4974-93df-8bdd6925dec8\Experiments\bilbo_2024544.481300493\"));
            //TestDirectory();

            /*Task<double> d = Task<double>.Factory.StartNew(() => 1.0);
            Console.WriteLine(d.Result);
            string[] array =
            {
                "qsQASContainsLocationScore",
                "qsQASAdultScore",
                "qsQASAppIntentScore",
                "qsQASAutosScore",
                "qsQASNightlifeScore",
                "qsQASRestaurantScore",
                "qsQASBookScore",
                "qsQASBusScore",
                "qsQASCelebritiesScore",
                "qsQASClothesAndShoesScore",
                "qsQASCommerceScore",
                "qsQASConsumerElectronicsScore",
                "qsQASDictionaryScore",
                "qsQASDownloadScore",
                "qsQASEducationScore",
                "qsQASEventsScore",
                "qsQASFinanceScore",
                "qsQASFlightScore",
                "qsQASFlightStatusScore",
                "qsQASGalleriesScore",
                "qsQASHealthScore",
                "qsQASHotelScore",
                "qsQASHowToScore",
                "qsQASImageScore",
                "qsQASJobsScore",
                "qsQASListScore",
                "qsQASLocalScore",
                "qsQASMapsScore",
                "qsQASMovieScore",
                "qsQASMovieShowtimesScore",
                "qsQASMovieTheaterScore",
                "qsQASMovieTitleScore",
                "qsQASMusicScore",
                "qsQASNameScore",
                "qsQASNavigationalScore",
                "qsQASNameNonCelebScore",
                "qsQASNamePlusScore",
                "qsQASNutritionScore",
                "qsQASOnlineGamesScore",
                "qsQASQandAScore",
                "qsQASQuestionPatternScore",
                "qsQASRadioStationsScore",
                "qsQASRealEstateScore",
                "qsQASRecipesScore",
                "qsQASSeasonalScore",
                "qsQASSportsScore",
                "qsQASTechScore",
                "qsQASTechHelpScore",
                "qsQASTechDownloadScore",
                "qsQASThingsTodoScore",
                "qsQASTravelScore",
                "qsQASTravelGuideScore",
                "qsQASTvShowsScore",
                "qsQASUniversityScore",
                "qsQASUrlQueryScore",
                "qsQASVideoExcludesAdultScore",
                "qsQASVideoMetaScore",
                "qsQASVideoGamesScore",
                "qsQASWeatherScore",
                "qsQASWikipediaReferenceScore"
            };

            string result = "SELECT ";
            foreach (var s in array)
            {
                result += $"SUM({s}) AS {s}, '{s}' AS {s}1,\r\n";
            }

            File.WriteAllText("D:\\1.txt", result);

            var date = DateTime.Now;
            date.AddDays(-2);
            Console.WriteLine(date); */
            //var list = new List<string> { "1", "2", "3", "1"};
            //Dictionary<string, int> ss = list.ToDictionary(o => o, o=> list.Count(o1=>string.Equals(o1, o)), StringComparer.Ordinal);
            //var s = ss.First(o => o.Value == ss.Values.Max());
            //Console.WriteLine(s);
            //ThrowExceptionStatement();
            //TestSendEmail();

            /*var list1 = Directory.EnumerateDirectories(args[0], "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
            var list2 = Directory.EnumerateDirectories(args[1], "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName); ;

            foreach (var item in list2.Except(list1))
            {
                Console.WriteLine(item);
            }*/

            //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(),DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt"), "111");

            /*string test = "\t<123123>>>>value</123123123>";
            Console.WriteLine(test.Split(new char[] { '<', '>'})[5]);

            var result = new HashSet<string>();
            foreach(var line in File.ReadAllLines(@"D:\BusanMigration\NetTrace - Copy.csv"))
            {
                var index = line.IndexOf("QueryName");
                if (index > 0)
                {
                    var sub = line.Substring(index + 12);
                    result.Add(sub.Substring(0, sub.IndexOf("\"")));
                }
            }
            Console.WriteLine(string.Join(",", result));*/
        }

        private static void TestFunc1()
        {
            long a1 = 6000;
            Console.WriteLine(a1 * (1 << 20));

            List<string> a = new string[20].ToList();
            Func1(a);
            //Func1(a);
            Console.WriteLine(a.Count);
            Console.WriteLine(a[1]);
            Console.WriteLine(a[5]);
            Console.WriteLine(a[3]);
            Console.ReadLine();
            var lines = File.ReadAllLines(@"C:\Users\liagao\Downloads\AccessedFolderInfo_User_0210_0410 (1).csv").Skip(1);

            var result = new Dictionary<string, HashSet<string>>();

            foreach (var line in lines)
            {
                var columns = line.Split(',');
                if (!string.Equals(columns[1], "SearchXAP", StringComparison.OrdinalIgnoreCase) && columns[4] == "200")
                {
                    var path = columns[6];
                    var parts = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 6)
                    {
                        path = $"/{parts[0]}/{parts[1]}/{parts[2]}/{parts[3]}/{parts[4]}/{parts[5]}/";
                    }

                    string owner = columns[2];
                    if (owner.Length == 15 && (owner.StartsWith("CO4") || owner.StartsWith("BN2B") || owner.StartsWith("CH1B") || owner.StartsWith("DU02") || owner.StartsWith("HK01") || owner.StartsWith("PUSE")))
                    {
                        owner = owner.Substring(0, 4) + "*";
                    }

                    path = columns[1].ToUpper() + "\t" + path;

                    if (result.ContainsKey(path))
                    {
                        result[path].Add(owner);
                    }
                    else
                    {
                        result.Add(path, new HashSet<string>() { owner });
                    }
                }
            }

            File.WriteAllLines(@"C:\Users\liagao\Downloads\AccessedFolderInfo_User_0210_0410_result.txt", result.Select(o => o.Key + "\t" + string.Join(",", o.Value)));
        }

        public static bool isMatchNodePrefix(string nodeName, string nodePrefix)
        {
            return string.IsNullOrWhiteSpace(nodePrefix) || nodePrefix.Split(';').Any(o => string.Equals(nodeName.Split('.').First(), o, StringComparison.Ordinal));
        }
        static bool IsValidUrl(string urlString)
        {
            Uri uri;
            return Uri.TryCreate(urlString, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                 || uri.Scheme == Uri.UriSchemeHttps
                 || uri.Scheme == Uri.UriSchemeFtp
                 || uri.Scheme == Uri.UriSchemeFile
                    /*...*/);
        }

        public static byte[] CovertObjectToBytes(object o)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return ms.ToArray();
        }

        public static object CovertByteArrayToObject(byte[] byteArray)
        {
            MemoryStream ms = new MemoryStream(byteArray);
            BinaryFormatter bf = new BinaryFormatter();
            ms.Position = 0;
            return bf.Deserialize(ms);
        }

        internal static string GenerateUnflattenedWorkflowFilesZip(string directory, string zipfileName, string zipFolderName)
        {
            var path = Path.Combine(directory, zipfileName);
            if (File.Exists(path))
            {
                return path;
            }

            // zip wf folder
            string wfFolder = Path.Combine(directory, zipFolderName);
            System.IO.Compression.ZipFile.CreateFromDirectory(wfFolder, path);
            return path;
        }

        private static void CopyDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, bool overwrite, Func<FileInfo, bool> ignoreFile, ref int count)
        {
            if (sourceDirectory == null
                || destinationDirectory == null
                || sourceDirectory.FullName.Equals(destinationDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (!destinationDirectory.Exists)
            {
                destinationDirectory.Create();
            }
            if (sourceDirectory.Exists)
            {
                // copy files
                foreach (var file in sourceDirectory.GetFiles())
                {
                    if (ignoreFile != null && ignoreFile(file))
                    {
                        continue;
                    }
                    var destinationFileName = Path.Combine(destinationDirectory.FullName, file.Name);
                    /*if (overwrite || !File.Exists(destinationFileName))
                    {
                        file.CopyTo(destinationFileName, overwrite);
                        ++count;
                    }*/
                    try
                    {
                        file.CopyTo(destinationFileName, overwrite);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(destinationFileName + e);
                    }
                }

                // copy subfolders
                foreach (var subDir in sourceDirectory.GetDirectories())
                {
                    var subTargetDir = destinationDirectory.CreateSubdirectory(subDir.Name);
                    CopyDirectory(subDir, subTargetDir, overwrite, ignoreFile, ref count);
                }
            }
        }
        internal static int LoadRequestContextPoolSetting(
            string configName,
            string requestContextObjectsToCreateInPoolConfig,
            Dictionary<string, int> requestContextObjectsToCreateInPoolDictionary,
            ref bool success,
            ICollection<string> errors)
        {
            requestContextObjectsToCreateInPoolDictionary.Clear();

            string[] workflowValuePairs = requestContextObjectsToCreateInPoolConfig.Trim().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            int defaultRequestContextObjectsToCreateInPool;
            if (!int.TryParse(workflowValuePairs[0].Trim(), out defaultRequestContextObjectsToCreateInPool))
            {
                defaultRequestContextObjectsToCreateInPool = 0;
                errors?.Add($"Parsing default{configName} failed while loading {configName} config. The illegal default{configName} is [{workflowValuePairs[0].Trim()}]");
                success = false;
            }

            for (var i = 1; i < workflowValuePairs.Length; i++)
            {
                var workflowValueArray = workflowValuePairs[i].Trim().Split(':');

                if (workflowValueArray.Length != 2)
                {
                    errors?.Add($"Parsing workflowValuePair failed while loading {configName} config. The illegal workflowValuePair is [{workflowValuePairs[i].Trim()}]");
                    success = false;
                    continue;
                }

                int requestContextObjectsNumber;
                if (!int.TryParse(workflowValueArray[1].Trim(), out requestContextObjectsNumber))
                {
                    errors?.Add($"Parsing workflowValuePair failed while loading {configName} config. The workflowValuePair [{workflowValuePairs[i].Trim()}] has illegal value. Entry ignored.");
                    success = false;
                    continue;
                }

                requestContextObjectsToCreateInPoolDictionary.Add(workflowValueArray[0].Trim(), requestContextObjectsNumber);
            }

            return defaultRequestContextObjectsToCreateInPool;
        }

        private static void TestTaskException()
        {
            /*try
            {
                var task = new Task(() => { throw new Exception(); });
                task.Start();
                task.GetAwaiter().GetResult();

                if (task.IsFaulted) // this code won't be executed due to exception in task
                {
                    Console.WriteLine("Isfault!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // exception in task will be caught here
            }*/

            var t1 = new Task(() => throw new Exception());
            var t2 = new Task(() => { Thread.Sleep(3000); });

            try
            {

                //var complexTask = Task.WhenAny(t1, t2);
                //var exceptionHandler = complexTask.ContinueWith(t => Console.WriteLine("Exception caught: {0}", t.Exception), TaskContinuationOptions.OnlyOnFaulted);

                t1.Start();
                t2.Start();

                Task.WaitAny(new Task[] { t1, t2 });
                Task.WaitAll(new Task[] { t1, t2 }, 3000);
            }
            catch(Exception)
            {
                Console.WriteLine("!!!");
            }

            if (t1.IsCanceled)
            {
                Console.WriteLine("IsCanceled!");
            }

            if (t1.IsFaulted)
            {
                Console.WriteLine("IsFaulted!");
            }

            if (t1.Exception != null)
            {
                Console.WriteLine(t1.Exception);
            }

            Thread.Sleep(3000);
            
            Console.WriteLine("Exit!");

            Console.Read();
        }

        public static void CheckStatus(Object stateInfo)
        {
            Thread.Sleep(10000);
            throw new Exception("123");
        }

        private static void TestHttp()
        {
            Console.Write(DateTime.Now.Ticks);
        }

        private static void TestWhere()
        {
            List<TestObject> list = new List<TestObject>() { new TestObject() { id = "1" }, new TestObject() { id = "2" }, new TestObject() { id = "3" }, new TestObject() { id = "4" } };

            var newList = list.Where(o => o.id == "5").ToList();
            Console.WriteLine(newList.Count);
        }

        private static void TestParallel()
        {
            List<string> s= new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                s.Add(i.ToString());
            }

            Console.WriteLine("start");
            Parallel.ForEach(s, 
                             new ParallelOptions { MaxDegreeOfParallelism = 100 },
                             o =>
                                {
                                    Thread.Sleep(1000);
                                    Console.WriteLine(o);
                                });

            Console.WriteLine("end");
        }

        private static void TestNodeCount()
        {
            var lines = File.ReadAllLines(@"C:\Users\liagao\Downloads\exp.resource.hardlink.manifest").Select(o=>o.Substring(0, o.IndexOf("\\")));
            Console.WriteLine(lines.Distinct().Count());
        }

        private static void TestProcess()
        {
            Console.WriteLine("Start running Lens.exe...");
            // run Len.exe
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.BeginOutputReadLine();
            p.StandardInput.AutoFlush = true;
            p.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };
            p.StandardInput.WriteLine($"echo haha");
            
            p.StandardInput.WriteLine($"exit");
            p.WaitForExit();
            p.Close();
            Console.WriteLine("Process closed...");
        }

        private static void TestRegex()
        {
            List<double> durationList = new List<double>();
            List<string> durationStringList = new List<string>();
            var len1 = "{FILECLOUD~PROD[90.1260]{duration: *1/".Length;

            foreach (string line in File.ReadAllLines("result.txt"))
            {
                var index = line.IndexOf("{FILECLOUD~PROD[90.1260]{duration: *1/");
                if (index > 0)
                {
                    var start = index + len1;
                    var end = line.IndexOf("s; FsUtils-Exists:");
                    string value = line.Substring(start, end - start);
                    durationStringList.Add(value);
                    durationList.Add(double.Parse(value));
                    Console.WriteLine(value);
                }
                /*Regex regex = new Regex(@".*?{FILECLOUD~PROD\[90.1260\]{duration: \*1/(\d+)s; FsUtils-Exists");
                var matchs = regex.Matches(line);
                foreach (Match match in matchs)
                {
                    string value = match.Groups[1].Value;
                    durationStringList.Add(value);
                    durationList.Add(int.Parse(value));
                    Console.WriteLine(value);
                }*/
            }

            File.WriteAllLines(@"D:\2.txt", durationStringList);

            Console.WriteLine(durationList.Average());
        }

        private static void TestStopWatch()
        {
            int count = 0;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            //CopyDirectory(new DirectoryInfo(args[0]), new DirectoryInfo(args[1]), args.Length == 3, null, ref count);
            stopWatch.Stop();
            Console.WriteLine($"FileCount: {count} ElapsedTimeInSecs: {stopWatch.Elapsed.TotalSeconds}");
        }

        private static void TestDateTimeUTC()
        {
            Console.WriteLine(DateTime.UtcNow);
        }

        private static void TestStringContact(string s, string o, string o1)
        {
            Console.WriteLine(Path.Combine("https://cosmos09.osdinfra.net/cosmos/searchXAP.Prod/local/DataPlatform/xap-dpppe/", "metrics", "11111", "Prod_11_zap.txt"));
        }

        private static void GetNodeList()
        {
            var document = XDocument.Load("oldnodes.txt");
            var oldnodes = document.Root.Descendants(XName.Get("Key", "http://schemas.microsoft.com/2003/10/Serialization/Arrays"));
            File.WriteAllLines("D:\\oldnodenames.txt", oldnodes.Select(o=>o.Value).OrderBy(o=>o));

            document = XDocument.Load("newnodes.txt");
            var newnodes = document.Root.Descendants(XName.Get("Key", "http://schemas.microsoft.com/2003/10/Serialization/Arrays"));
            File.WriteAllLines("D:\\newnodenames.txt", newnodes.Select(o => o.Value).OrderBy(o => o));

            File.WriteAllLines("D:\\diffnodenames.txt", oldnodes.Select(o => o.Value).Except(newnodes.Select(o => o.Value)));
            File.WriteAllLines("D:\\diffnodenames2.txt", newnodes.Select(o => o.Value).Except(oldnodes.Select(o => o.Value)));
        }

        private static void TestMsLevelTimer()
        {
            for (int i1 = 2; i1 < 100; i1+=2)
            {
                int round = 1000;
                int timeout = i1;
                double total = 0;
                Parallel.For(0, round, o =>
                                       {
                                           Stopwatch watch = new Stopwatch();
                                           watch.Start();
                                           Thread.Sleep(timeout);
                                           watch.Stop();
                                           var diff = watch.ElapsedMilliseconds - timeout;
                                           total += diff;
                                       });
                Console.WriteLine($"Latency: {i1}  Deviation: {total / round}");
            }
        }

        private static void TestBoolOrderBy()
        {
            var list = new List<bool>() {false,true,false,true};
            var list2 = list.OrderByDescending(o=>o);
            Console.WriteLine(list2.First());
        }

        private static void TestSpecialCharacter()
        {
            var string1 = File.ReadAllText(@"D:\setting.txt", Encoding.GetEncoding("ANSI"));
            File.WriteAllText(@"D:\setting1.txt", string1, Encoding.GetEncoding("ANSI"));
        }

        private static void TestSerialize()
        {
            var objectArray = new TestObject[]
                               {
                                   new TestObject() {id="1", name = "1", value = "1", version = "1"},
                                   new TestObject() {id="2", name = "2", value = "2", version = "2"},
                                   new TestObject() {id="3", name = "3", value = "3", version = "3"},
                                   new TestObject() {id="4", name = "4", value = "4", version = "4"},
                               };

            File.WriteAllBytes("D://3/2/1.txt", CovertObjectToBytes(objectArray));
            var obj = (TestObject[])CovertByteArrayToObject(File.ReadAllBytes("D://1.txt"));

            Console.WriteLine(obj.Length);
        }

        private static void TestSendEmail(string s, string s1)
        {
            Console.WriteLine("123123123123123" +@s);
        }

        private static void TestSendEmail()
        {
            using (var message = new MailMessage())
            {
                var toLists = new List<string>();
                toLists.Add("xapperfgate@microsoft.com");
                
                using (var htmlBody = AlternateView.CreateAlternateViewFromString("test", new ContentType("text/html")))
                {
                    htmlBody.TransferEncoding = TransferEncoding.SevenBit;

                    message.To.Add(toLists[0]);
                    message.To.Add("yutzha@microsoft.com");
                    message.AlternateViews.Add(htmlBody);
                    message.From = new MailAddress("liagao@microsoft.com");
                    message.Subject = "test subject";
                    message.IsBodyHtml = true;
                    using (var newClient = new SmtpClient("mail.messaging.microsoft.com"))
                    {
                        newClient.UseDefaultCredentials = true;
                        newClient.Send(message);
                        Console.WriteLine("Done!");
                    }
                }
            }
        }

        private static void ThrowExceptionStatement()
        {
            Console.WriteLine("Done2");
            Console.WriteLine("Done1");
            Console.WriteLine("Done3");
            throw new Exception("11");
        }

        private static void TestDirectory2()
        {
            foreach (var file in Directory.GetFiles("D:\\1\\", "qq--*.wfbond"))
            {
                File.Copy(file, "D:\\2\\132\\", true);
            }
        }

        private static string TestDirectory()
        {
            string experimentDirectory = "D:\\Experiments";
            string firstExperimentDirectory = Directory.GetDirectories(experimentDirectory).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstExperimentDirectory))
            {
                return null;
            }

            string firstExperimentVersionDirectory = Directory.GetDirectories(firstExperimentDirectory).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstExperimentVersionDirectory))
            {
                return null;
            }

            string wfBondFileName = Path.GetFileNameWithoutExtension(Directory.GetFiles(firstExperimentVersionDirectory).FirstOrDefault());
            if (string.IsNullOrWhiteSpace(wfBondFileName))
            {
                return null;
            }

            string[] sections = wfBondFileName.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length != 2)
            {
                return null;
            }

            return sections[0];
        }

        private static void TestFinally()
        {
            
            Console.WriteLine("123");
            throw new Exception("111");

        }

        private static void TestDictionary2()
        {
            SortedDictionary<string, string> dic = new SortedDictionary<string, string>();
            dic.Add("bilbo", "123");
            dic.Add("asw", "123");

            Console.WriteLine(dic.First().Key);

            SortedDictionary<string, string> dic2 = new SortedDictionary<string, string>();
            dic2.Add("asw", "123");
            dic2.Add("bilbo", "123");

            Console.WriteLine(dic2.First().Key);
        }

        private static void TestDictionary()
        {
            var dic = new Dictionary<string, string>();
            dic["1"] = "2";
            dic["2"] = "3";
            dic["1"] = "3";
            dic["1"] = "4";

            KeyValuePair<string, string> ss = new KeyValuePair<string, string>(null, null);

            Console.WriteLine(ss.Key??"111");
        }

        private static void TestListAny()
        {
            List<string> list = new List<string>() {};

            Console.WriteLine(list.All(o => o != "1"));
        }

        private static void TestDeleteDictionaryKey()
        {
            var d = new Dictionary<string, string>();
            d.Add("1", "2");
            Console.WriteLine((d.Count));
            foreach (var k in d.Keys.ToList())
            {
                d.Remove(k);
            }
        }

        [Flags]
        enum Flags
        {
            Value0 = 0x0000,
            Value1 = 0x0001,
            Value2 = 0x0002,
            Value3 = 0x0004,
        }

        private static void TestEnum()
        {
            Flags s = Flags.Value0 | Flags.Value1 | Flags.Value2 | Flags.Value3;
            if ((s & Flags.Value1) !=0 )
            {
                Console.WriteLine(@"!");
            }
        }

        private static void TestExcel()
        {
            ExcelHelper helper = new ExcelHelper();
            helper.Create();

            helper.Open("D:\\1.xlsx");
            var sheet = helper.GetSheet("GoodSheet");
            helper.SetCellValue(sheet, 1, 2, "123");
            helper.SaveAs("D:\\1.xlsx");
            helper.Close();
        }

        private static void TestConcurrentDictionary()
        {
            ConcurrentDictionary<string, ConcurrentDictionary<int, string>> TotalAnalysisResult =
                new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
            TotalAnalysisResult.TryAdd("machine1", new ConcurrentDictionary<int, string>(
                                       new[]
                                       {
                                           new KeyValuePair<int, string>(1, ""),
                                           new KeyValuePair<int, string>(2, ""),
                                           new KeyValuePair<int, string>(3, ""),
                                           new KeyValuePair<int, string>(4, "")
                                       }));
            TotalAnalysisResult.TryAdd("machine2", new ConcurrentDictionary<int, string>(
                                       new[]
                                       {
                                           new KeyValuePair<int, string>(1, ""),
                                           new KeyValuePair<int, string>(2, ""),
                                           new KeyValuePair<int, string>(3, ""),
                                       }));
            TotalAnalysisResult.TryAdd("machine3", new ConcurrentDictionary<int, string>(
                           new[]
                           {
                                           new KeyValuePair<int, string>(1, ""),
                                           new KeyValuePair<int, string>(2, ""),
                                           new KeyValuePair<int, string>(3, ""),
                                           new KeyValuePair<int, string>(4, "")
                           }));

            if (TotalAnalysisResult.Values.Select(o => o.Count).Distinct().Count() > 1)
            {
                int maxValue = TotalAnalysisResult.Values.Select(o => o.Keys.Max()).Max();
                foreach (var item in TotalAnalysisResult.Values)
                {
                    string result;
                    item.TryRemove(maxValue, out result);
                }
            }
        }

        private static string[] GenerateStringArray()
        {
            return null;
        }

        enum SS
        {
            value1,
            value2
        }

        private static void TestRemoveEvenItems()
        {
            string ss = @"4362534	e8cf6f63-3a1e-4816-a32b-a4cb6c7abbf3	3920758	4ac0f5a7-75aa-4a50-8607-1edf1ceda5ae	3737005	e71fa46f-e601-494e-bc73-619cfc826606	3848876	137aa0fb-4e53-4dc6-8301-c08ab3b897b0	3674489	ede0fca5-c017-46be-8a18-aea42acb0bb1	3726828	dfca91f4-cd31-462f-befe-e1d89f5f2ca0	3748952	9d6ad8fb-2583-400d-8366-317635f1b858	3632467	4cc9f2a5-6d88-440e-a646-b3af15da44d2	5607947	5c46745c-1f4b-4b23-b24c-e5d0489c4cd2	6688264	b308bf01-c334-4cb4-ab97-e309dd020852	4518049	388f4871-7a24-4554-a23c-561fe378d095	3933393	689d0215-0f0b-4d1a-b693-93dee4e0a37c	3573806	14a6a0c6-8a32-41c0-97b9-c84b507a0aff	3699818	09b74202-7f77-4563-87a3-51e73a209920	3898831	f5768138-e924-4b36-aeaa-10a25e774f67	3554333	fb6cd5d9-6c38-4b0e-bee8-47fd3452d2d7	3783886	58ee062f-90a4-41a6-97e4-82149d93af63	3525108	87a6f1a2-628c-43c8-a24f-3d2c2bfed95f	3618634	08b06733-3e4f-4be9-b397-4e83d34721c2	3693175	01aa2738-859c-47d4-b38b-6f8468ee849c	5113620	c0252cee-5440-46c1-b84c-d8597f41e1b4	3698205	e5f1cbba-5faa-451a-8932-dc994fbca325	3836871	81cc1a12-6868-4906-af49-381b62dc399a	3819606	82acaf33-0eac-4d87-a7f5-a1e3cc592024	3539865	c8180229-189d-4914-8096-24969ecb381b	4118949	8d2bc082-9b69-4980-8b42-6bfa7b4bc144	3831387	324727b7-bb6d-417e-93b0-058017ceb18e	3822153	433f5848-aef5-405f-b67f-c7ada5415a91	3566964	4e5455dd-6018-4280-82e3-7fd7a9a3e341	3979423	3e8667ff-9a51-41d3-b378-e609e862da9e	3774188	40bd0a88-e2c6-4a94-b97b-8c3b1611d05d	3729771	efd4a009-56ef-445a-8919-f599aa5eb3b0	5016258	24fa1ada-4664-4452-b19a-720412d9c31f	4262312	b8068d2a-5341-4ba7-ac28-514c132331c9	4535401	8fc39bc0-7e97-453f-9fe4-95d9e0e60da0	3501783	2d941f87-0227-4fed-b331-6ee920c9526f	3923094	4cd3194c-f4fb-4510-a031-c249948a9c14	3497678	ca6a73e9-49d4-4318-82c7-b5ca523361f2	4236035	4f195a61-33f7-4349-925a-f17c07a4730e	3544040	8721b4e6-c091-42ed-ac6d-2fbedbd0a948	3733901	393646e3-adb8-4ace-94dd-5fe9fa8475bf	3528779	d4ea7a89-2a99-454b-8183-257b04dfe6f0	3660313	f5c0aa29-d116-4dda-a780-7816da679001	3742245	594d8db0-ee7e-40e7-a96e-f6ed45b063a8	3549739	e037499c-ff58-4d42-980e-24f3606b1ebf	3830932	977cdb14-0b3e-4acf-8839-1436356b7cb1	3727723	3a0fabc8-d96e-4cf2-8be0-656a50f186d1	3574579	5090ae24-45e3-49f1-af6b-4b52492cc16e	3696655	f1cc6d13-c539-4586-9543-b451a0a578d9	4608676	7ec69d9b-e42a-433b-80e3-7ffe258b8d73	3733291	88c9111f-c956-45c1-b3a4-aecdd8dd1791	3500634	91b791a5-b58a-4324-8639-80539d0648ff	3418947	ff735beb-acf3-4792-9ba0-87a4594cf4bd	3973519	232f1352-39eb-494a-a89a-9924f6a7bd56	4058861	87063f46-f573-43b0-83bc-127733ddf58a	3716599	247de4e6-c1c1-41fb-b5d0-e8b3da4d4c3c	3811082	e18700a2-c3e2-4781-84b1-28fb29cf2876	3699920	8b3744f9-ec3f-42d0-b1a7-95fd4230c690	3779203	f76f0ebb-961e-4b43-9e4d-94988d87acd0	3908826	80dacaa5-d282-484c-b334-b9902e1fd61f	3885965	ad5d5868-b78b-4f55-a7d3-dd392c495d4e	3511793	5e4af845-161a-435c-8e5e-9ed18d0e5dc4	4905443	c91e9b12-85d5-4303-ba77-59e14feec7dd	3524468	adf7775e-6030-46dd-aeae-021a02562a07	4087553	e2251483-dd3a-4f15-9e35-942bbf52a58a	3594576	4916521b-a24c-4170-902f-1101a346d1db	3963669	2e2a17f2-656e-44e8-8168-48519869d0fd	3545061	30f2a897-76ad-49df-adb8-6a1967270e4b	3670427	5d38a709-5655-4888-885d-3f6fafa12f8e	3530006	115e5c56-85f8-498c-851d-5c35828de92f	5345162	d12e484c-d30e-4283-b4a7-51f22387b2a7	3683018	5eda7838-7f84-4570-9f29-2ba3fda5f557	3308285	185ea7f6-fb31-41b3-9ddb-f45b26e0a063	3682593	49ea718c-c424-4914-90d3-6d9fd39b21a8	3475764	414e885a-a601-47d6-850b-899f04a2f66b	3698870	2529b32f-0fa6-4978-9782-4e6d6f118875	3975551	3b41d921-1f0b-4efc-a015-d41695242ac7	3503670	82d6031c-8143-4ef1-b9eb-48e0d6624c2a	4829650	84792b52-e30a-49cf-8a74-5065fa98665b	3452205	df03db5f-23fc-4ce9-b047-8fae221169c4	3718783	21f4ec71-a617-4972-b310-76badaba715e	6591919	a98990c4-687c-49f4-9da9-d35110e2e569	3116961	5282accd-d6ab-4e67-9f17-e7dbe4526649	4008686	f5ae8235-41c1-48ce-a10b-f4def7ef9614	3944469	87ba6787-8a24-489d-9423-97cade76cfe8	3586892	ca684ba7-b6c4-4d79-a19a-71fd66ece496	3848108	f60eb5f7-aea3-4134-ac97-7cf2b4f8d0c0	3438137	085a0521-70d0-4798-a772-8949817b8543	3557476	badcd948-9e9e-49ea-ac33-a13deeaf5687	3502892	dbb1b235-071a-41e4-82d6-6809486949e5	9196520	24a4f122-0b1d-456f-87a0-df7db5d378d8	4233493	04f5733a-48c6-4c04-bf12-f6e98e14d6db	3619406	6b5226b6-4250-4eb1-947d-fee8d9caad24	3784501	d0f583b1-0bf6-4058-be14-008791857dbe	3895365	6f3747e7-03c0-4be7-825f-e5dd47b041eb	3586379	7eff91de-79b7-41b7-b141-3dd54771f0eb	3613116	138a2352-5b3b-4506-986d-6115c8c9da54	3876268	460681f0-79cc-4463-b66d-2888772e0762	3618957	e723315b-8632-4cb2-9b32-8445025e78b2	3767599	dfd8485b-c77c-4920-8987-cac2d57b9c34	3709340	1cca4c7a-695e-479c-88c6-77b41736d94a	5154195	d7cdbb2c-0df4-400f-a125-e0c434d2609f	3677656	7b0a8b13-6442-4242-a2ee-39df43f96ea3	4126598	ba02bf28-caed-4b00-8281-6064587758bd	3475589	db6f5754-7f9b-4a23-b29f-41bbea5d53ee	3902946	3b801afd-5fcf-47a7-ab4f-4c0c7ed667fb	3488918	9ce0b266-3cc8-454f-a09b-1faaffbb6e0c	3482583	9c22528a-4b3f-4d4b-a009-4ff5cdb9bf79	3891460	6eec5a09-bff7-4b02-91d0-4af24ad5cd9b	3656624	6b8e4a08-00d6-441e-a317-3c2bb8c7c531	3873741	d32ec955-0e5c-4f4a-bb2c-f5b0e9cf7dcc	3450240	a33a07e1-3a99-48e3-9304-f5c99b34a614	5393582	965bd3c4-9df9-48ed-8c8e-e7c64e5d3d00	3463985	4b16f994-ab14-489c-8f93-797205318649	3687217	085c2a3f-7675-4f8c-b701-9e7d90d36fef	3660167	aedc323d-b9b8-4c11-975c-ba15882416da	3599840	575b642e-9025-4f8a-a0be-7532f6d808aa	3734659	d555dcc9-9178-4883-9356-5bc330e13282	3729718	fd92c83d-4cd6-4085-923f-af38c31a91a8	3897575	1e065242-3452-4b27-96a0-12ed40cb247d	3659004	628e8748-8920-4727-a5b8-905f16949f1e	3864986	0154b5cd-ffa5-428c-a7b7-5f81a70394ea	4267416	ef7bc2cf-5412-44a2-9023-8d4d548a66d9	4047067	a16ce5c7-a74a-49fc-b143-34a4d0ecea91	4816037	c614dbb3-abb6-4207-8189-65c917c8e615	3697032	176b32c6-73fe-4411-a021-ed263926b085	3962095	93307125-54b5-4d1c-b65c-1e6e9efbe1cf	4221689	a12fa667-dd43-4d97-9750-33e7c6029751	3741560	6136a56f-4211-48bf-a24a-1ae45ea678e6	3678375	9ac0d161-fecc-4928-860c-5d6a9678f9c4	3563850	7b7beb28-d238-4e78-9c7a-04f71ef61c18	3869493	5b0417ed-262d-4f00-993e-b67feff9054e	3805490	92154083-7e53-4b97-8c3e-bcfaa7dfb76f	3606874	911f6b97-aeeb-46bc-97a3-7f2befe91d08	3023268	47486314-b6d7-48c9-aad5-44d97363d49d	3889528	e6b6318a-b0e6-4d59-9a62-8e0b7f7899c2	5683554	b54bcc7a-1fa6-48e2-b1d8-8087ac6e3baf	3575015	e7875373-8490-4242-a993-178be1e12c51	4120713	f229c08f-f84a-4821-955a-cb8d6ef13598	3777580	b74a9664-8237-4e4c-a66f-bbd6fe7d6dd6	3611043	d6b27321-9223-4c10-bb2b-91e4ef5ab6d6	3684275	bf540413-ebe1-4180-8afe-44f7cf76c704	4105444	f1fa5d15-bcb3-4a7b-9306-87765be924b6	3607900	c66658a9-2c72-4285-a761-e7e3ef0624e6	3841104	0c9d143d-d21f-4158-af41-4474369b5974	3641505	a5765e8a-5e26-4498-a741-ea8d3eea06b3	4057122	0b9e2ce6-f2af-4fb5-936f-4eacc1ad156a	4859080	af5fff66-a601-42bc-935b-33faeadad01f	3854086	6def8403-cdf0-4d02-9c82-34f79534544c	3682534	5accdbd6-9822-4e21-a59a-429befac7a84	3857776	81781526-ab21-4e23-b362-7f8dec20c37d	3582786	166b4344-98b4-42fa-91f7-c335a3726565	3545447	ce8161d8-df78-4fb6-880a-c6b4451736dd	3906558	a4175430-7206-42ae-9f54-228b9e093a2a	3442586	07dc5f61-60bc-4e38-bb5e-3951515c86a2	4073579	f9ae8f87-dccc-4e8b-aabb-99f4b5ad6c4c	3778235	c8cb437c-b665-49e8-91e2-c25a87f21c31	3615218	42cc1542-82ba-4879-a363-17281cc9026c	3508499	7beb4280-4ae5-4625-ada3-c1c3f946d028	4292358	9b155d28-45da-4803-a394-379f602e573d	3677964	3c9da9a7-d8c6-4e0e-b658-82e815df0046	3725919	c89fd804-8119-4338-862c-7ab36d85073e	3647684	abe8b593-a478-41f5-acce-1782307eaa36	3713470	ea032758-e61a-47be-80b5-a1876214255e	3643348	1698d230-2f64-4fde-992d-e60f4a0ede92	3471991	da1221ec-e25d-4d13-88e5-c0a18a9b78a9	3543253	28f57d77-7de9-4d4a-b043-4c95211be414	3667103	758913fd-b9d8-460f-8bda-3aca225368d1	3488316	fa83442f-575f-4340-a606-93ca96c7d1ba	3680115	8d0df151-3df0-4758-9b48-9194e5513730	3540554	b34d6e4b-a8fd-4f79-9f03-32c64f52e0d5	4898752	de5c9ee8-d9b5-4bf0-8bc7-3a810cbd0725	3706759	2ba9c7c7-1798-4461-bcdf-dfe8ba59ca08	3512068	1e08bbd0-c5e0-4c3a-a22d-490686eea004	3479508	b39951ad-d6fb-4868-a842-bfb8c3f702fc	3520562	edd45a1a-108d-44ea-9068-1e3ec3b6b1fb	3779579	40dfb48a-aa61-4685-a9fa-2fff11b1ad7a	3920587	7d9f3976-7dea-44df-8424-76dddeb7fd35	3842927	11e0391d-b066-41b9-a5bf-f3d69538806a	4398295	2a3ebd6f-f7a6-435d-8b3f-46d5c10e3882	4029344	e39dbb49-6f07-4337-9df9-e3dfe31fe793	3667440	f5a3c90f-9757-4748-a064-ffc6b5e83c69	6215812	a3258ee7-882b-4704-a563-a5ee33be33a7	3864312	3dc647ec-73b1-4355-9747-1154680f2b7b	3608907	0bf91b13-3f15-4dff-90ee-33388c1b0077	3885819	006ae098-c58a-49be-b4bd-fe70e9c30c9b	3552843	ce544f0c-821b-4a4e-93b6-d400df488900	3630340	e01c28db-4263-4576-9dfe-e3e9dbc203ac	3684935	8078bfa8-61ef-439e-b470-05a4884f91f4	3491596	b654547d-c921-48ee-b076-4bc26848974d	3205951	08089739-0ecc-403e-8871-f8dfd451f319	3890545	a7cd6007-09ee-49dd-a5bb-8bb7efa23dad	3486768	70070d10-01d1-41d2-98c9-cc24e7677c7f	3906994	8743b2d7-7640-4202-abf9-9035b4f4c1ac	3861574	fb77d262-c240-44f1-817c-e641567f4b92	3478707	51607487-3849-491f-ae14-81e2431fb7b5	3729967	20ba2758-b1ae-42c2-8114-174560c68fcd	3897540	673a08fc-f929-4ed7-ad29-d5d8b5dae6ab	3667020	c2f2263c-5cea-4708-adda-0fe189f27920	3799986	47699422-423d-47f7-997c-a5af0ad07538	3756806	0d65771d-4467-4c14-b984-50584cd101c2	4085808	d2e8f1dd-f8d4-493e-be2f-261529e05743	3666135	0e4edb02-70d6-42d4-aecf-b50ac176450f	4046622	bbd97380-8ebf-4fc0-ab80-7b91a90e25b9	4708327	1615a4bf-92bf-43d8-b373-a8645947f943	3929024	a9695caf-ebb5-4225-96ca-30910bbaf67e	3780049	f00b7875-0938-450e-9d1b-51e66ba97c1a	3623639	160a999f-8382-4a54-a9f9-0f37a341afa5	3780152	5086aa57-7926-4f5c-95ea-c8fdba163a9f	3915220	de2189c1-41be-4021-820e-e3c77824cd52	3649566	51e946f7-a692-4a4f-9f36-665bc2fa7dcd	6614256	3cff7b9d-b63d-409b-bfe2-40f890f1ef90	3641197	e9cf4a21-e705-4794-8c4d-73f8a91f8242	3494627	9eef97fa-abcd-41ee-8bb9-0fcb4e60050d	3673477	1d9cc5d9-de81-465d-890f-d8d8816592db	3608824	52e19d20-bff6-4def-9a12-6bfc45d66efa	3406791	655cc0e5-229a-4b99-a41a-f432d83a9a5f	3593774	7aee8485-d2ef-45d7-80c1-2e53aa49e6df	4261799	84c9b6c2-ccfe-4a48-ac52-94f318b9d2b5	3953859	47d7134e-b5c8-4922-8661-7d21403d0641	3810315	16388a52-2802-4dcf-bb59-ead654964ca9	3657561	454a11e1-0bbc-4e5e-ba1c-8be115f4eeab	3636587	29773edd-a4ef-474f-8db1-66376b651d68	6009687	ca60805e-aa60-4f0a-8356-abd551a39aa8	3716843	6af338c9-fdda-4be8-8004-38a63b506cf9	3694813	6a0a9139-a74d-4e3c-9762-762853e01edf	3936096	b423d486-32c2-4ee6-a567-b90ddd09271c	3999987	aeb0a2e2-8aee-4d3c-93d3-91642160807d	3742011	e3e2226b-5a89-4c62-93cf-d41ebfbf0751	3700757	01923442-b37f-474e-b5c3-c08d6f162584	3916045	c2ea6220-65d4-4321-aee8-08db955cfbcb	3644080	10516f3a-4882-4316-998e-feb926e561cb	3563317	cd6a91c6-937b-42d1-b095-7d4142e94092	3846539	a6a15269-0782-49f2-992b-3feb31419255	3710147	23abc370-a321-4bea-88a2-538f0a515b4b	3567829	a559612b-5c69-434d-b4fb-3bef061b44f2	4491361	5d1dd011-85e5-483f-a557-7d6ba3858cb1	3516936	1bc45e5c-ba72-4175-914d-c803b6f313da	3664434	0e8acb8d-f8d6-4099-a096-e5e51242ca30	3631392	9e0c3e56-2250-4256-a278-7dc343fe7869	3724531	a959c69a-23a3-4a91-a15e-45018b361a08	3680711	de5e48c0-c883-4621-adf5-af9950501380	4492324	c151ebb5-c80b-461b-b8da-3ae3b64c237b	3665441	f3797600-3804-49b2-86b2-ddec50c81111	3728779	8b9239f5-1817-4279-bde4-4cd5633d7473	4466887	3a400ff5-3d19-43b5-8dd4-b20efd206b6e	3711114	1c9c8d96-5aeb-4eaf-a1f9-ff5800011494	5903076	08b860fe-7774-42f7-892a-f71f405638f8	3588535	d113982f-7b86-42c8-9810-8ec802dbc2dd	3749851	4ab3573e-3b15-4de1-9d8a-41e3d66359bb	3753850	0c1650c7-fe86-4ec7-b419-b778d6452bc1	3683580	29cd4948-01bb-4c92-a7e3-11e60b4e8460	3690340	72edfc83-288f-4a31-a059-a3b9bde574f3	3780864	e7cebf45-62de-42aa-90cc-204d8ecaa1d6	3613340	61a7dc07-7211-48b1-bccf-27d81275737c	3596194	7afc3685-778c-43a0-b52c-6fcaa62c650b	3613903	283d3710-2dd4-43d6-bf5c-fd6acf254dee	3568024	3b1e5764-8186-4d1d-a1ac-ff1778ab0c5f	3530098	aa9158e0-95dc-41cb-a57f-d23ca8064606	4984666	25fd4904-6165-4995-b37b-eb479e8920e8	3684665	e54bb557-820f-4b13-b8e8-51339ed9e3b0	3760561	022da528-9acd-4c47-8390-ddf47e94d814	3844638	5b1dc20d-3962-4a5a-a8f4-a39dad1f11b5	2988212	8627b089-7b68-45dd-b218-069638fcec08	3739689	97e3df12-a7e4-49e5-b7cb-23a829a87438	3577923	97015458-61ef-45e2-ab67-5d5854c10ae6	3690838	358f2937-a8bc-4977-9e11-236376d33a4f	3781720	365f107f-b0dc-46fb-bc63-e0eca925c587	3769212	74980197-d464-4733-b641-a918a3f6cb3b	3715928	cc1e6f5c-54e6-4c8c-9eed-b83f72564306	5393406	7bb10abd-b24e-4449-bcaa-c142a662859a	3936800	f5ed21ca-122b-4278-81bd-7a52483b60dc	3631543	1760eef8-68b0-4260-845b-3d83454ec9d0	3565278	68058491-b19e-4667-b28b-80bc5e0f4cc5	3124464	1f71f1f9-7b4c-4008-8bb0-14045c888bd2	3743648	e5955ed3-c475-40bb-8f0e-ba19748d0da9	3579247	f4990653-eb58-43a0-b77e-85f15d3fdc4f	3425883	87a0559d-c1f1-4f10-bf78-be43a03c576d	3604503	c7ce6e57-19b9-4dff-b736-80d036ddd1e7	3563170	60b62532-6b69-4696-9060-831bf747197d	4059066	723de32d-029c-4bc9-9ead-e35cd15b6cc5	3511046	d2e7845c-5853-4c4c-8a20-de7de028c24b	4790650	9d2fe592-67c7-4a0b-919e-19389a0939b9	3419559	0aefe586-6b22-4e63-bede-2543b011a3e6	3937596	f9c78ece-12c9-407a-93a0-44635a972e08	3847859	3a0a9dd1-80f6-4409-9a53-27a112ae5b6d	3562046	0866220d-a039-44c7-8221-b72df93f295a	3688727	5c43685d-e9cf-407b-9043-08f939b9b0aa	3846999	6c24f518-fd1c-4189-a063-41883db56518	3964505	b34b5111-672f-470e-98c3-17532bd5e758	3823087	3b5d63bd-6fbc-4bd2-ae45-8ea49b20f455	3710581	30551a0e-8a5b-406d-94ad-2d5cf2ff1077	3700077	31ddc59d-c902-46fc-8d8c-ce3aeacae14c	3368425	997fd11e-a984-470d-ac51-05b016b09680	5359195	aa3ca522-9f4e-48c2-8ec2-37c6d67112c7	4243445	9aca237a-71f6-4a5d-a880-46f18374c472	3556225	d179a076-1c75-4bf6-83bc-f7a657a909af	3639003	1768c26a-93eb-4943-b51d-7a089c9dbed9	3593965	d2441ab3-4403-4a33-9a21-c9b2780642bf	3773337	133b4fff-9f1a-4972-9773-2f011c48ee05	3746864	666553f7-f678-44db-9942-43d1545a7f27	3570371	2a9412e5-f17c-4ead-ae2b-c81e42a87697	3707145	af89fd0a-a353-4401-8406-ceb0bf1e96da	3655269	bc7b99b7-89fa-432b-83cf-b69cfddd6096	3556841	243f72f5-73dd-4103-9301-8396cac1f064	3545017	53238861-26dc-4f9d-985b-8b289f2fe5d9	3757129	54607811-f399-4666-8f0f-a34d3a5dcaa5	3706900	79a4e998-5423-4c60-88b5-f226cfdac6f8	3059503	661daaef-2dae-4637-add3-b14ad6363968	3817681	543b929c-4cde-4512-b960-aaefa89799b6	3606092	1db8ce68-8996-40ae-a481-9619137c58bb	3662753	477c3527-4ef9-43fa-aef2-cc967c84f09d	3450914	e2cca98f-108a-47b9-b5cf-d07e1d64bace	3747470	f1a507f5-993e-4e8f-a910-4751446a984a	3600188	ba05cf95-168d-456c-b9bc-e33350a1cd41	3884083	617cff9f-5624-4639-8075-b598be9ed69d	5015519	575db34e-5029-4b83-976c-030eefb96595	3630884	57782743-e062-4edb-a38d-ed59057a9f7c	3786511	a3c01b05-ec06-4d6c-ac1b-435fbb5420f7	3580400	b444a4d9-34b2-44d5-855a-f8fac9b3433f	3347926	7508b481-62a0-455d-9a6e-9d5f6d9837ce	3738066	9e42a224-ce26-4803-b9ed-324e3be82100	3583539	46ad7f80-51b2-438e-85e3-6ec13b258793	3577273	3b65ef8d-5794-4beb-904b-dd8afa8d4758	3618600	f46cbc41-356f-4167-90e4-52ab04ec740d	3994023	3e25395f-50ce-49cd-807f-074008402587	3504536	9ae6305f-db31-47f3-968e-b394c197acfe	4137235	1e18c3e4-8348-473b-8a93-3f0a7954b66b	3651775	4816cc35-e338-4b52-8e10-dda6b3a20c53	5050248	33eb1a83-386a-48af-b305-a3ef4b6038b1	3935783	43dda268-8bed-4922-87ce-8d5a50424f30	4137455	8f808c5f-cc26-4454-9326-a66000c0ee5e	3600485	72124d14-294a-429c-9db2-62fe55ab120a	3965194	c3b9b70b-664f-47a4-af9d-a190a792aad7	3582488	a4546321-1147-4735-9688-210e67d63d7f	3731428	63aca96c-9d28-4b82-9dbb-19197f2198c6	3593246	05c35361-5360-4612-992a-5459b151c74d	3612969	a5d1c393-9dd5-48d5-88bc-b6558cdb46bc	3738106	6ec99584-0e15-4a39-9e60-d3e5abc6fe6d	3459986	db28c021-c75a-442d-9a55-299db63794d8	3675114	98c46f85-6904-442b-ae43-0b5c9aa937aa	4798842	de0c7d08-14ed-49a2-8bc5-b4a74599717d	3711050	ff492389-ae43-4b39-9c3a-fa60907c648d	3572512	0917da6a-7134-467b-9639-f9a8700f4054	3577766	83b8be73-bb00-4efc-8ed1-8bf6bbb0fb62	3571710	cca410c5-73bf-4bab-b21f-674be5eaf488	3440820	aef9a0de-5153-465d-b9f5-20b3911e20f3	3723485	7b6a3f64-7749-4f83-9b95-a42c446ebf1b	3863618	c76eb10c-4453-4690-93a3-cec615097cf3	3543697	b8b7f943-8a1d-4e79-b71a-93decc68ec1a	3619162	662e6a22-c880-4544-91b2-febfc3f3469f	3461903	175977b6-d2ac-49a2-b4cd-53ab55f13233	4731550	88d6b1de-39db-4bfd-8108-9f50066cdb65	3701583	239aee72-064f-443f-91a0-bd8cb76e30a5	3590368	1cd0043c-a3e3-4696-8939-47ed6b7fd683	4038723	532ed95e-80ad-4480-898c-5893f4a5080d	3476811	62602445-569d-4467-b166-732ac85923b3	3701959	624fbd12-52fc-40c8-8997-0b804f83ad06	3617657	524bcd40-f334-429f-a9ff-facbfcce727c	3973904	0aad61b3-b768-44fd-ab04-f8dddd74a979	3539161	522aff56-aeb9-4f74-b5cc-52ea921c6bf0	3861648	93ca58ca-eb47-4cb1-a445-082d94fd43be	3927938	6b4635a7-db0a-4742-bb67-ceb82339e39d	4092857	86323ee7-06b5-4e66-bdf4-4f75cf6076ab	3593325	49589e96-7110-4ddf-b40e-1173af5b0a5b	3550193	c0acea01-f93b-4adc-9e7b-facde9fe5695	3868516	fb842d19-dac4-45af-acfb-3c4f65166ce3	3847532	cac78286-903d-4b76-855e-8e2ff9a1e5bb	3555233	8c06ea10-f247-47c7-9d0c-cd2f180a3205	3600334	3c9b15c3-c3a4-41da-94e3-90cf78eb9ece	3447151	53ee636f-340e-4f97-abb8-4a85d2c51bc8	3705381	d11228e2-c35b-4aa8-95b6-c3b0f1e0234f	3534332	64614130-80d5-43e2-bae5-c52aaa9da72b	4070407	fdbb8423-68a4-4025-918e-165da6084917	3839247	cc6b1d08-ba2b-4915-9024-bbefb8acf64e	3450997	575c4076-9f13-4168-9fd6-7dc68f17670e	3676884	56304cfd-e636-4495-9d57-a07219972f1f	3528750	5ec1e4ba-3429-4530-99b4-947214edd6a5	3103172	8aa4da4c-dfb1-43ab-9ac4-eb69ad072b31	3651535	42135b4b-7db3-4e16-8db2-b9ef87695134	4126140	f925efcd-74af-4faf-bc5b-a7bcbedf52af	3689187	113d58c6-f42a-44d3-91e7-f9540c645394	2953493	3c079ae6-1b5e-45b2-830a-82c1f185c37d	4408388	37e48057-35cb-42de-b640-34f5339c924e	3823898	589344b8-ff74-4170-ba27-b21a774aed6d	3572775	96db0fb9-569a-4efc-91b4-d057a30f18e8	5790873	a62af201-00cb-4b13-a7f1-8f533eb84d37	3512869	5ba835fb-080d-452e-88a4-4209fd5f62fa	3538526	74c47c22-31c0-4700-a5d5-fb9bb5b9fd59	3635160	82288518-0e09-4dd0-bc8a-505c47c19db6	3605501	53535bdf-75fb-42f6-b2dc-7b5446379d3b	3629574	6a481d88-826c-45f6-be4a-f2c517b786e9	3501284	1e5440b5-28d1-4284-a7b0-ec2ae5777c9c	3553469	2c0b8365-c810-477b-96e9-bdafddead6a1	3509144	0080e6fe-81ab-4808-b187-6387a95f92cc	3870270	0532263b-a65d-4acf-98f1-dbf0e754b6c3	3983392	f73b2c7c-6980-4065-b6ae-5f8747a0c541	3741077	979fe184-89b8-4e94-a7ff-4b878cbb25ce	4914628	365c30b8-b5a7-43c3-a15a-95510e9b9d54	4123920	81c9b111-9de0-47d2-9196-f0d2fe22c2a8	4726241	c187ba6d-4702-4a37-b682-6ae641347e03	5150798	9bc3bdc4-789c-4655-86d8-abe25c4531f7	3679866	0f6ecdb5-1659-4459-9f70-a3dc23346b01	3687271	0d7fb074-8245-45f8-971b-f342775723b9	4263123	4becf2f2-9631-4d53-ac50-57db3328105a	3770659	aeadc1e8-c845-4b37-a917-e6b0bc81f62f	3928843	b6153dab-a5ce-4d24-b63f-22f4e3303fbe	3841631	8a60367b-a4f2-4bb2-9a7f-a2a1c0689e3e	3769505	218d97e2-05c1-4d85-94ba-7c7ac8ca5199	3671932	13f4782b-962b-4071-98c3-b097eb812590	3254986	25690d88-cf3d-4c94-907b-35a319ecf49f	6638858	5f24c829-a9ae-4290-93fb-5c88d865463e	3594058	f4a51478-e3cd-45af-947f-ddb9d07d5088	3611044	5d570554-7b1e-493c-90af-794cf6096af2	3909775	17417f97-2f60-4b08-8da8-af1f31d70981	5204570	dbc4ee81-54b5-4dc1-9509-89c72548b040	3820169	2e18cf53-7b6e-4316-a8d1-603c4fd62403	3737201	a0f99063-7786-44aa-b25a-21520b1e18ee	3377981	3ca640f6-ea1a-4854-b5d4-522c521ca6e5	3516833	95740ea4-17dd-484f-91eb-ada4b43208a6	3841104	eee6fd6b-5a9a-4a57-9417-3b33d013a2e2	3410637	af91226a-2026-4791-8865-22497751ce14	7256971	f2338c11-fd84-444f-a804-562b56e1b155	3932318	f40778b2-5e4f-4754-813d-b5a2685b4fbf	3857918	21047d00-8f8a-40f8-bdc7-b75a377a6749	3812495	f218beea-d10d-4e6d-b5bf-073ca99de270	3841710	5016a0fb-fd74-4d05-a4a5-2d917818c9a7	3643900	7f2dfef6-08e6-4e17-b670-02527238821c	3542696	c226b2e8-47e9-45c2-94ef-ef2ef06a8f02	3329962	28ebc170-c135-42b2-9fbd-2d099ef18aac	3496098	f03cfd57-0078-414c-b79f-3c785f7adb29	3853807	79191507-fb3d-4458-bad8-31fd5e165d49	3821513	a281c923-9bc9-4ea4-9441-e2ead5b54d99	3594766	caa99a24-bf50-459e-a256-00da25e3b62d	5634234	fd821f10-1b03-440d-a680-4be8a9ad4d0a	3494535	89ef9b34-01a6-427e-8599-8f92aaf401ec	3551786	109743ab-e8f4-4abf-97bb-f0962da19d3b	3844438	5ecda41a-eaaf-442b-85d3-324585d2316b	3440249	68231496-ce5f-4da8-b71d-44551f1fd270	3751029	97f7939e-61c7-434a-8b6c-8d583522c25b	3648328	66ce9495-29a2-4570-8207-f5bc2e0f1ad6	3670211	39f123d6-2767-45ea-8121-9acaa1dbff23	3769393	2812c9ac-c632-4cd5-8798-a88938dc599d	3603672	ab77b96f-6f58-41e3-9c73-62ae68e29a0a	5906243	7f4449c5-b2ca-44c5-9b5b-f9caca26045e	3644658	9cedde62-17f9-425e-a48f-2f89435336ce	3382844	820241e8-d86a-499b-8844-235b5f047298	3505963	589ff823-94f1-434f-a2c3-6595c9f66943	3701910	545c7146-201a-46d1-98b5-b636cd44223e	3705033	3ee67096-0cfd-4688-b58c-60e0af44aa69	3720440	6d73eb46-e5e7-4fb4-9f4d-93a7fe631d1a	4132151	552293d4-d423-4810-a70e-9e59cc4db027	3703772	e1ef40e5-d47f-46fe-a5e0-e59513021290	3691064	e513b5bf-9a40-4060-a8a8-c4c0fdc6080b	3707888	b2fde8c5-01a4-4711-a49b-58ebd56b86a2	3712947	fc59e166-2eec-46ff-b254-b44e6b4e18dd	4101606	9cb1faf0-4187-4841-8733-27add6032943	3765938	59f0a48b-d2bc-45d9-80ea-c0faf49c2e0c	3829895	441c5cb3-ebd0-4b1a-875d-015d7d1b48c2	3612104	4bd47da3-5abd-4335-b1ee-464a164132a5	3769173	5ee100b7-3158-4c92-a42c-46adf441533b	3801262	21daac8e-aad4-425e-bebd-3665483bd0f6	3130583	40ce1510-294b-4a87-83bb-014e443e8d27	3494397	c805d5f5-470f-4694-8328-f2c257098995	3608370	e2cf4991-92f6-4d1a-a4a3-902367fbb994	3186970	307154f9-38a8-4ecb-93e6-ae85a7b17832	3214744	b21f8147-d930-4a39-981f-2ff1404f6812	3890551	add78aa5-ab11-4cea-b911-1f5d94af3d62	5811051	de69f3d9-ca22-46ab-a650-ece84ad02495	3480623	2bf5bcb6-7062-4853-a671-a0c2f18f8784	3610579	ffedc2f6-6645-479c-9b7c-829f1ea450f2	3685775	203ae3b9-9e3d-44e5-b80c-49217a8938ec	3395245	a89dad8d-306b-4a44-bb17-fe9cb400d46c	3879787	227f211d-1e42-4c8f-bd26-7d556f0251d0	3604689	dbb5dd11-8737-433a-9da4-a2a5b656beea	3508084	59707fa6-e87e-4b6f-ac22-3e69f41ac464	3996325	98379ff5-553f-4c7c-874e-1fe1c9473605	3787713	c363d669-7580-4076-a8dd-9438e3433cd1	3749342	15a9f2fc-c3ef-48ab-9a36-4a2509a76834	3177204	55cfea98-f785-49dd-a561-1b68d161c1b4	8145310	60973d2f-340f-47c6-883b-11de3cc21243	3490849	59d52176-1cfc-48cd-9cba-a54ffdd18458	3701465	587fe4ed-daa7-4dea-b4ca-3f0176cceca8	3568494	e8ebd622-6495-464b-9877-e2ed52dce2f9	3974446	03b5301f-0681-454c-9556-7f7addad77ae	3640043	de110a97-f152-4f59-a91d-2b5bca4852c4	3600334	60c86b7b-5b77-4765-aa09-0b8880ddc6fb	3907477	bcc0ac3d-32c2-452d-8c0c-5d1c9ac1c8ec	3878726	42f43756-0a76-4d58-bd57-4b34c833ba95	3588084	ceeb4c8a-8e24-4017-847f-ac2f5ca1d9a6	5132326	250d7da4-e098-4c55-95d5-310af94d267f	3965126	8489b102-0aa1-43c9-8e25-b407e59ab83f	3833557	e6cd5db7-c0d9-41ff-b3d5-865cfa914379	3789863	c1cf5ec9-d3b7-4479-afc8-9494870b964c	3771353	fca7e1b5-07ce-4f3a-9028-718f17c06176	4165639	8a62e060-f441-4d7f-ba7b-ea8bf321cee6	3730509	e14b28de-fc5f-4c92-ad00-4aa9cf0161ab	3731643	72ed1b29-43d5-4db9-b8fc-9c8fe3bfcef2	3593788	f1e22dcd-4e04-4c4a-b3a2-b9790345e29b	5066017	c36f0348-073d-4113-bede-d423d9e53174	3401722	74c4cde4-a2fc-4fb6-8e1a-bcecf6b270fe	5106582	eb2fb44a-7a08-4f0f-bddd-2a5ceca05b4a	3722909	cb96b477-5363-4835-a5fc-857b62d30b88	3433626	bdcf8b17-5485-46b5-b287-b143a493ebbe	3729732	84995abd-57c9-4fd0-8a25-5273c2555c65	3876170	3437a31d-1bfb-4d0e-b87c-2a49061c7675	3716681	90a9b99a-6281-49bc-abc2-b123c6aee7d2	3643514	1921aea4-5238-4c99-8d06-451fc8ba3bd8	3623194	6edd9a9b-6fd5-45d2-8321-1f0962c9a85a	3907125	3dcb9dcb-a259-45cb-828a-d84afb68895f	3697951	b9f0b574-df09-4027-92e1-aca3dabcda2a	3070295	69ab7749-9c3a-4108-bb41-e03924183d38	4955187	00d2ae38-e5c5-4276-b506-296d5c157c90	3581241	abb2ba83-78ff-4e36-b998-86666c3eee44	3640249	c5013ea1-9d14-416e-8aec-e06ccbeea025	3856628	2dca16dc-ac08-4545-b782-3a95e08efa47	4418203	5941478d-6294-492b-87c1-16456bde4045	4044051	bafebfd1-d9d5-4e16-b1eb-ba01f8afca1b	3703616	1d99b6a0-28b0-437f-89fd-c115cf500a35	3036852	d36b4d8b-3115-428e-8580-0eebf19a633a	3299857	6a6ed6ba-29ff-4b21-843a-409c06dd57d0	3661594	6627711b-a91b-49dc-aac4-dde65febe7dd	3604884	480be649-7d1b-4643-9807-1e9ebab77d4b	3685672	fd5ed32e-6d90-4f4c-8984-90c574daa405	4781690	bf736543-ab77-481a-91ed-0a95af1fe9ae	4074757	a47ea639-3297-4b40-b491-9fa68517ed24	3808844	53285cfc-ede8-462e-9259-7ea186486e57	3883218	58b013ed-80d1-49d6-a3a2-d49b4a94d1b7	3878731	b7d1b9a3-d777-4a11-a5ab-7522da103be1	3551220	95166f50-2a49-4424-8f50-a310631d61f4	4058549	a39c552d-ac96-4fa0-8537-03dd5744333c	3480965	2289191d-cb99-464c-b06d-fe4faa73c91f	3653563	e7e5d023-92c7-46db-b14c-e45dd7752c3b	4247682	c5ede305-01ec-4c8d-99bd-085a4a66585b	3433009	b1da0c9a-58b7-4528-8bd6-f7fe5f42f447	5609795	0f87e39a-9fdf-4c88-a30c-93d1caef9a63	3584727	db77a1f5-9ee7-4bba-8002-31c5674793f1	3718422	3d42d008-3902-4ce2-9bcc-26684cd1e407	3517430	f257fa81-c60e-425c-b671-c0924592bc24	3678903	19f4421b-8b5e-497d-9883-17299bf5a3f8	3918734	77f1d3e5-8530-436d-9438-539d4bc46c5e	3608653	6dd7ec94-1829-414c-ab86-de3ec1e5508a	3644687	6a863281-4dd4-494b-9f34-b5f6c61aab05	3679572	2ec4b2e1-eb92-46f1-b367-be129616ea30	3629104	fe0b0c72-c959-4562-8685-cdc147c521b6	3602412	720f36ae-e710-4c75-bcbd-196dbc430e27	3711515	4157c156-4f00-4910-a2ce-862685100ff4	4705296	1ce68e60-4d3d-48ff-afb2-661bc4f01b15	3606537	b39d3d30-7719-461a-bbcc-3c96bfbc6838	3530944	47a3ab8d-8dda-4285-b1b5-cb728794329f	3515244	abeeca50-ec92-43c2-b604-20bbf3b34102	3619694	575a318e-6993-4e57-b874-c836ffe4605f	4109628	c516b7bd-d509-4a54-86bd-5ba4f5fc058e	3977268	b8aa1e86-716a-4eff-9caa-611a052f31a5	3776833	2573ef03-adb6-4819-80b3-0d5489b0e7eb	3706431	097eb671-0e94-4308-8074-c327e41afe55	3733804	70a8f98e-5200-4dfa-95f5-7a277a783191	3675359	aba6de3b-2feb-4648-8f9d-b7fdc8d75734	3560155	de7d1867-4349-41ea-a45c-da0c27ef503e	5449280	fa48d41f-5833-448f-b3d8-e9ab7370b72f	3401302	987cd937-bf27-4ea0-bb0a-4b362ae643c9	4430413	5815bcd8-a333-47f4-a4d3-c382916d1064	3221695	5ce161fa-0dd7-465a-bbe5-12c420853d8b	4140905	d6514ab1-7760-43c7-9e4a-5a2125e04a84	3532607	069736cd-e6c0-41b9-90ed-d547f124ac8b	3627262	faa8c43d-3766-457c-9b0c-6bfc47e64b8e	3972516	6333cc76-6248-430b-a5b7-de1b5532fd89	4170600	9c6c1b5f-a7f2-41f2-867e-11dba4bba97f	3551846	0f28bb95-28f2-4458-9ad7-c00fffc8e553	3788113	f426ad60-d8da-4f4a-81c3-396393b002f0	4934849	17d476fb-2690-4ef3-9bab-61d4bcbddfd0	3665319	2f9705c6-9c1e-4d06-b7b0-0807653ca9ad	3661526	d4bfe49d-a12b-4ed7-9c9d-0d1680788b49	3864326	f8cee9b3-fc2d-4ed1-ae47-4cd53c8d70fe	4027882	3ff677c9-6112-40cd-ab79-13b2a1beaff0	3473859	7708106a-e41b-4f48-828b-310bd04ce0f2	4010818	6381e9a0-ef75-4f26-9b91-9f9dffc60a42	3665724	62fb4fb0-a6af-43cc-a7f8-1ed9c03ba507	3728985	10493183-a0f4-451f-aadf-d1ff7da40f38	3643949	3415ca81-cfb1-4e18-ab59-693395dd77b5	3648578	9100729f-3ed3-4b14-a9e0-5e6457630632	3606693	9790f854-fba0-4d4e-87b7-f00a16543d30	3649198	8f92070f-05e9-45a8-81e5-9152341e3702	3506016	b80058c2-196b-434f-ac76-d0b74bab1c08	3498714	9bcb0a28-c661-4474-a15e-602bb9a78c54	3670158	18fa465d-8849-429d-948c-829bef91143d	3581564	cef34b1a-cde8-4ed9-b0e2-7171381cbc0b	3677534	8cd34461-f673-4d31-9eb7-547a8a4a5e10	3516344	7c52b94c-7d2e-4318-9c85-f0e81c7a5b20	3290580	a8ca5a01-52c0-48fd-9d18-a27a62d07651	3480047	0ceceda0-4953-4c29-bf77-cbd83a416ea2	3388535	aaed536e-048a-43af-8d1f-05f1bc422202	3763229	76dfba62-10cc-451d-8fcd-9115735c0ab7	4870254	64e080c5-a61f-4bfd-8827-e26354c955c3	3595851	afca37d0-e387-4d01-8ada-53f47a7996fa	3480520	92b44aba-c3ea-457a-abd2-4afebb244e5c	3423175	011e7989-7767-40a8-bd0b-60ca0990f1b4	3447258	a070719f-00fe-42d5-a0c5-1a12e4cd9880	3563357	919e65f0-9a25-4a09-8563-516505ddef5c	3737973	0989299e-6ad1-49a2-a56d-a7c1ee561daa	4815411	c993d4ac-e41c-421b-84bf-cdac4b7937ed	4797566	1b5edb10-94ce-478e-88bb-3dc283ee7260	3610085	e9237979-291e-43ec-91e0-18a4ec9165fc	3866125	128d3c29-2772-4143-b7fd-48e464a1348d	3737084	93cfec80-faf3-497e-9f09-314acf393292	3727249	9e20bcb3-1ed0-462a-9c75-81d8303ec5aa	4620906	359c0f5b-acbb-438f-b46f-389f9722b6d6	3726721	27326050-6654-465b-a5d4-4d8500e420a0	3517679	cd011cdc-7f68-43a4-ae64-92279448727c	3358508	45636a67-831c-4569-9e68-945dff79581d	8264811	ac3dc831-60fe-4342-a1ab-e36abd2c7a66	3618444	90e70d82-4f1f-4d31-bcca-90a5a39f8f43	3760824	432051cc-fd1e-43b8-95c4-6aea916a93b1	3977766	b12382dc-ddf2-42a2-b8c3-f2b7d3a175b0	3693630	4dab5ede-160c-4c9a-89a4-9b410fcda69c	3591848	41bdb8eb-d2ff-475d-a8e5-130bdbc73600	3562672	db076c50-129a-46fc-81d1-135ef20ee221	3777414	0d33e88f-85fa-447b-adf7-d77868fbe328	3264557	ceb85b05-b71f-42a8-a927-e7d00b63aa4d	3533496	0690ed60-1f4e-4683-b9ad-bd4d53f34910	3958551	c5ddde96-1a7e-4531-b4fc-e211a400313d	8555130	6ecc8782-f8d8-4ccf-b9f3-d551bb636d79	3688546	429053dc-d933-4498-9ab4-3a6bd86f190e	3166515	0d4b913d-8777-4464-932e-2588bbd7f56e	3555008	52b18d1b-0cd5-4c83-b72f-9b300a65ded4	3687178	8cee00ba-fecc-4f33-8dd6-5d95d3001a75	3358132	7b0ff0a9-a03c-403e-a9a2-0105fb89bcae	3730509	2f9f01d0-6fd1-49c0-9fef-04b62ec0fc22	3314096	ffcf52ec-216d-4ebf-8b58-4da3ef2ec251	3727709	b722d0a5-c42e-4c0c-8603-1bb932a8bba1	3619588	1cc5afde-37fa-4ec4-9a5a-cea7cc9a52c8	3715537	436eafcd-036c-4e9d-b704-d8a2488e4185	3598056	19607d33-2d39-4ddc-9f20-977e48747bee	5299523	863d050d-3151-4bba-b87a-70434815aa6c	3651090	6e83065f-9e43-4ffe-b4f1-a75bbea56cec	3524102	e32bd80c-8a5d-433c-ac70-9d8573bdc8a6	3752901	0a7c2104-cc6d-4adb-9035-31c16126e38f	3792244	15532b7e-9bc0-45dd-a689-685b375a23bd	3823546	923dfa63-34f1-4b06-bba3-457582771c91	3682113	70414c38-ecf0-4ecc-9a00-05100815f66f	3419597	4afe8421-1189-4d1b-95e0-bed68ad91ecd	3662298	6cfa76c6-689d-4df0-ad7b-9f2bab470283	4144025	7e53643d-467a-464d-8e62-d3da342bebe1	3223420	dff34891-a071-4b9f-bff7-a29102d23bed	3713098	9eaa5d9f-4785-4d18-80c9-5b9d1f081186	5906106	60f4fdf1-15a1-4009-b022-7255e76e0db3	3531898	404caccc-3958-4fb2-9748-bbed68eb59b5	3794023	b46f466d-e818-42b6-80f3-54f504268488	7957022	076854b6-213d-4bd1-aae1-7b040dc5085d	3784292	338ba288-12c8-4f45-bff7-e926da1680b3	4084074	0ed40761-8b18-4f90-965c-172f79f6466c	3984296	d3353901-8640-4984-8cb3-50be795bb60c	3972051	0b1ed573-8459-488e-892d-10d2bf35d6a6	3893317	85bdd642-53dc-4280-afec-35ac132f1d34	3586276	7c5b3177-f15a-4c97-a489-847c26c81030	3634261	97ac4f84-b15d-4ea7-b31e-f57f064bd463	3653046	ca0282ed-384a-420e-87ea-eeb0a7bc27e8	4700418	7b10e14f-f470-4cfd-8846-33d7400f9848	3419881	0c9f180d-4014-4cb8-aca3-a139e5579bd1	3814948	c9064738-dfc4-4d68-8393-ef52e22cae68	3155981	780596fb-656a-48f1-8c1c-1392180ab9a8	3686156	fab58e85-eb6d-4272-82c8-855e299ce99c	4395641	9cbcce92-084f-436e-8088-7f45ac86c95e	3808921	197cb643-b564-4022-a64d-4fb5b3e3bccf	3945857	ec1a94ac-a968-4400-b139-63afe47d0bbc	3497311	376c81d2-29df-49e1-8145-c53405c448a0	3617378	109fb8c1-ef8e-49cf-b4cc-cc87e39c64f1	5402707	86fad536-1853-4fa4-9f02-8b7a471a04ee	3576090	d2ea5d4d-2bb0-4465-9f13-38ac1cd034be	3641255	8c2d4552-06f5-44b2-a06d-cb46cfdaf3ef	3967290	66f3473b-c64f-4d96-8c22-d51d5c884cd9	3755565	4fa2374f-e0f7-477b-9410-e3cf34a0b1db	3782791	e2c24ed1-30f4-4ec4-a3a7-9b0ce52ff54e	3838552	1f68738d-1e8b-4ce4-9f65-820b698c9895	3602250	f3c1470e-a31a-43c0-a5db-04c11dd30617	4558595	97316fa3-3d7d-4acc-b041-b11f6f29c59d	3684871	df3276be-0320-4512-be2c-764ae30f9813	3860044	607bb031-3c3a-4a88-a557-82219a964b06	3736106	3c02e4a4-341d-463e-b91d-83d5a056d733	5278392	ceb29b3a-a739-439a-b91b-674d345f462f	3758815	5fdbad87-3494-4239-a6da-8572c9db21f9	4059389	6091fe48-b5be-4635-b6b6-e1fc4d4655c5	3914472	9b01c6ec-cc79-4be3-9cfd-c12357fcf234	3746449	5c189f04-d21f-417a-8c41-4a9123c30752	2932373	7e08c0a0-0a18-4337-bbd6-1c4ea9085fcf	3784487	626a7db9-20a6-4316-8f2b-4bc9df2bcfd1	3647121	dcfd2e47-5bf3-4752-bfe2-d6af623cd0b6	3645772	1ff3f2c5-242e-46a5-8a53-6876b43d696a	3808291	46687921-d374-4506-9626-bd4cff29b01b	3641153	a1b74e25-f0b1-4f37-a4d1-c33d1e1dc640	3656936	174636e5-e62a-4650-b10e-ec59171a1d1c	6110672	0464c498-3b90-4e94-bbc9-82bd8da17802	3772467	32ab7cca-f0c2-45e6-86c9-b28df3d00c3c	3735598	94415fd3-49d4-4b73-8012-c14b321a362a	3831998	991be812-952d-4534-8392-7dcb5a0c0144	3605134	36965be9-821a-4ba3-94c1-249f27c42197	3641139	6bc514b6-b5f6-4311-bd9d-0f0add5862a9	3697550	9d6eb105-f94b-49ea-99ca-a640860af9d4	3554573	bf9252dd-b0d5-4ae9-b48e-667587394d75	3671644	70b96d82-3caf-41b3-97f2-d3279e830976	3832540	4edeb630-c8fc-4058-a634-3f48c1d156c1	3706592	0e006bbc-4772-4081-8428-e17b5689d7b1	4975619	e52be181-7543-4af1-bd0a-0a196808e69e	3592532	de41753c-87c7-48f0-9e2b-23d5fd2f7aef	3687652	e1adb912-0bf8-43d1-94f5-657e9f5a6a7c	3744010	39d75d8c-b0b9-4c9c-ab8a-8b641d3f8c71	3685506	5dca3f93-e517-40d4-8f4a-8a94e85ae943	4577848	5e71a720-a6f8-43fe-ab43-596d3c73465e	4039085	bff4cbf4-b5ae-43b3-99e9-1917b3d73b55	3996492	e93b331d-9b19-4a67-855c-05d947463582	3912101	da4b2612-064f-4f98-94c6-580909e4dc64	3967017	f7e81cb2-f9b9-420d-a6dc-6bde511c37f9	3770038	ac93838a-a073-40a4-9c61-aee5345df477	3625340	bf45d872-7d4b-4316-842d-9a2e1bd8116a	6278299	161ccb34-636c-4108-a29f-6a2f596d6e66	3926271	8fb217f1-8ef4-4e4a-b620-5dc533c94b4c	3828845	79a8742f-5d41-4f9d-92a4-0842c484d7ba	3853744	9115845d-8942-454c-a33a-fc70c0bf3fc4	3990337	bac2370e-2655-4fd7-9a2b-7903c3706083	3718172	f81003c9-9f33-4291-814f-00951c98a4fd	3722904	de2e7be9-a3a0-4c6a-b40d-b6e47d88fd6d	3660617	cdfa1ff2-2a2a-4ba1-a0b4-18f506ae9a3b	3594448	13b51fdc-d5be-455f-aed7-4e0e06c151f0	3689461	5acc4d4f-1cb1-4cd6-bb2f-676061fbe4da	3793764	110e41ab-dc0a-4462-aa06-c81c66abeb3d	4907838	1f702a9d-2046-4862-94ac-bfca17abb7ee	3922454	234b09f9-88cd-4672-aae4-f47d244d45a2	3582722	45ae7564-bae8-4414-b205-05aaf3071383	3916926	520ae6e4-2d3b-45c3-b297-f7bd45927953	3697066	5234e89b-f839-4a67-9f09-2bf19854ce03	3630346	6ebe59f2-c944-4b57-885f-68b1e919e682	3688214	09d785a3-e9f2-46ab-8506-2cff7b0a8511	3264948	2a6bad15-a7a7-4d21-adf9-50aa39043e5d	4104916	c87bd30f-4c4b-48f6-a464-4959e5879c09	3858485	f2d4813f-2c92-43c0-ae1c-f8546945109d	3859917	d57e821b-26d1-4215-97aa-3a19e437695c	6158500	5ed72c7f-65df-41eb-be63-81fa24841ebf	4462615	a3727a40-e7b1-4c89-be5a-52d8cc34c202	3821244	b248d7d2-f1ab-459a-a40c-f225e5c5e114	3572223	719a657c-5e9b-4549-a736-d75139a72420	3794541	80d6a482-9315-49f7-ac82-dc20777b65e6	3700542	09577551-b68b-47ae-b9a4-9a4f34a6c1d1	3468203	65222e97-e248-4d14-aac1-66a1d58364db	3693029	00c0af2c-fdc1-45e4-bc3a-82503125cf28	3619035	83876ca1-d4a6-49d7-bf37-3d5729b167c5	4275016	dbde1f8c-ace1-4096-a0ed-c07fcc0e0676	3675808	75dc152d-090c-4eec-8678-5b860359ee3e	3755130	c966820f-b9cf-4bff-9e27-e25fa2fc2e53	5080670	2af18791-d6ab-4cda-a185-0c93cab14c36	3463051	0418d3ad-96f1-4809-a260-089e270f71a6	3542744	7f343d3e-ddb9-4f89-b96c-2a71a154075e	3926477	03d56a95-e343-46ec-9a46-41ddf37456f8	3870847	aaec13c9-acc1-44f0-a975-cb4ff1ce37e3	3588852	5aaac0b1-abe6-4cbd-aa12-604726c52023	3759040	a7b8146f-dd47-4293-81b7-d3e15c7010d8	3814327	ff43f23d-139f-471b-a3c1-246d6fe4475a	3595138	c9ae3689-557c-46b7-9418-7dfdc1709afb	3612124	787eac58-d67d-45ac-88fd-cb581f8306d4	3853055	9ace7221-5797-4cf6-a163-def7e4513a39	3701778	109abc4b-66ec-4b71-8722-fe1bae9df91a	6150097	e31ba3eb-4c95-460c-995e-ff2d983574ef	3855147	159857e2-9fa3-4cd7-a521-6cad21be8652	3530979	0ab41e90-ca65-4a88-94e1-bec54c970268	3627931	35e27b90-ca72-49cf-ad35-f4e3de688656	4169227	a9e90292-2dff-456f-88fb-1d41c839398d	3531414	a28c6c7f-6d2b-4737-8ee1-eb63db78b57c	3713622	453ed2de-8430-4030-b9d6-1fd6f3106be0	3760654	8ab2581c-cb1b-4a02-b1f0-a6f6c1482ae5	3103279	855be2aa-59c9-4eed-a3bc-322397420891	3799268	6b067505-bbbf-4863-801f-1e561146899c	3644100	d4b58710-7a5a-4372-96a4-155f6f3d7676	3934698	8eba7dca-0ced-4219-90f6-8e41d5a68fcf	3856047	da031aa3-e289-4a14-82a7-6d52d728af6c	3250460	a695ed9e-9769-48eb-98d4-30f407648d31	3833405	bfd88e74-5208-4f0b-b11a-28b292a352b4	3700732	9f0f2d65-78db-49fc-90e3-d9f635f8f158	3867704	d3273dd7-1134-4ed9-8401-1f4cb32dd9a5	3383877	723b410d-1e58-4659-8386-5416a8f5cb35	3779056	85cbfa4a-77ca-4120-97eb-40276c8436e9	3644814	9b98e618-79ba-4605-95db-9fc5a5d010b3	3754465	29423013-f9f3-4260-9e71-384c2c6167f2	3725030	b0c8af35-2003-4c4f-a392-ebf783a16b15	4382634	76e8c60c-ddfb-4c99-b4a5-1cf43e281ac5	3703059	243964e9-a2c0-4807-8aba-c3d15c85c587	3944254	fda88137-e90c-40be-b8c3-151a16b37678	3649013	260d2ebc-dcaf-4840-9d23-c20b08d1e10d	4026294	a10071a9-8747-4f16-bba9-5c3b1ac20784	3452219	52020780-5eb2-4c51-bec2-9353e9705e90	3660666	bf1883cc-fcd1-4d3c-a759-d27c3d2224ed	4270079	b0f40bf3-851f-49ce-8625-e1aa8ab27809	4062385	80fd10c3-2193-48de-b56d-9f8235f94204	3679685	83c1db3b-b410-49f9-9917-6271c0636fa8	3894617	83c3d351-8c24-4320-9ebd-7defc4e48559	3870881	08f777b0-6993-4840-a488-1e408cd141a3	3896875	b6615aca-b4ca-4006-b106-5c4da0070f21	3637311	ae7659ba-c189-470f-8464-adcebf478e24	3920406	7fabc768-bb74-4e98-9bf8-2a054c01754c	3981720	76c38d18-7548-4d62-b9f0-e74db20f6996	3842834	0b82caac-4b8f-4d9a-a022-d6694a28385d	5036938	d7db2b01-86ad-431a-a82f-6f75256853c4	4744889	e2789bf5-7895-409d-b5df-1296ca0b2541	4389707	8d9a22ce-7b58-4fa2-a315-9e484580f610	3719008	f4ecab17-e095-4487-b5d0-92946b79d6ab	3715973	3379473e-41a8-4a18-9493-5d27d2416f02	3702477	2a385a17-2542-475e-8800-9efad8257ff4	3714247	20c87949-43db-4aa5-9459-b8cf22f269b6	3500571	c24684f7-6024-416b-b5f8-0aed085ce0a3	3470055	7ffb9671-48fd-4e39-bba3-8a509b973ac5	3844198	d71d04b3-9207-4079-b7f6-5086cca19339	3741214	75effcfd-27e9-4b2e-b86a-48443a701240	4057771	9f19b09e-240b-4683-9ef5-5cd5e469bc0b	3551156	b50224fd-eb94-4f4a-a45f-e204602d8858	6921624	96399863-ebb8-496d-b938-ab205575d3b8	3572429	8e0b61e5-3779-427d-b3b0-8b1a6793d9e0	3785127	27df1738-f48b-4514-9bb2-1837648f7543	3957227	9321bbba-8404-41cb-9c4f-a1aa1a9140eb	3584008	92d1d0f2-ce04-4cc8-9cc1-c46c6da13b27	3772159	54da4598-151c-4d06-849a-932b1de727d1	3510875	5a8e491b-9c89-4096-a32b-2afc50279831	3680652	f2c037c0-081a-4030-9c21-04cd791c3252	3355937	bd65922b-ff51-4e98-ae3a-d84dfac998b1	3818830	a49520f9-aa22-42ab-8df3-357391989760	3552681	414d972d-3f39-4542-b919-a7416ae5a6c7	5494748	0be5bc3b-4348-4836-aaca-ba14cd6eb4dc	3352466	fcf95fc8-1a3e-4624-9295-6da22ea0f94d	3717170	03e932a8-4f74-4aaf-938b-5967dc91a538	3970439	560dfe5e-b26e-4102-9850-a27a911fca7d	3597132	643a972b-5d1f-4ad8-a883-ceeb90bfc7e9	3652019	0f2e5559-34ce-4ebb-9d07-cf00ee6f35f7	3546512	a48d857d-30ee-46d1-8d31-4f852a9e039c	2863531	6e432f24-6cdb-4d9f-951b-e19157ab3d6f	3604508	0730470e-aff7-4ec5-892b-c275e687eb7d	3831900	29028f7e-89e3-4e03-9be5-aaabe3557178	3570913	b6c27899-17db-4f51-abcf-a4b35bdb36f7	3918426	e66db278-cd6f-4975-9411-3d05cd726646	5363853	ad7b37b4-50f9-4d3a-9cb5-f94b85020dfa	4189238	f4338c92-b369-40cc-adc9-274e0dd83558	3594028	8ee7eb75-f095-461f-8fde-d6435f19babf	3653460	8c26acc9-fcb0-43ed-8f8b-0e9b762e8c7c	3856931	9f940b3b-4e94-4489-9e8a-930f93702683	3584922	6cb79448-0acd-4c87-b5bd-513a73a3156c	3704725	8d3fce46-9405-45f4-92fd-ce55435b1c2e	7591175	117423c6-f007-4659-85de-dd3f3a92b910	4012695	14e124f7-ee44-4898-ba7e-98169097cb6d	3692168	91b6f24c-6d9e-4a50-9103-bf20f4693770	3680389	ce2242c4-024f-4a03-bd85-ac00172f5ac3	5031600	beccdfae-6e68-4487-8dc7-61399c4b15c2	3704950	1461031c-c41a-4aee-8cf0-193b42578075	3746024	5366e549-4578-4ec6-974a-280e582d3b95	3494344	b7830668-1830-4b05-ad41-86e77c9695f8	3666853	ec515a94-025f-4cd4-b211-95028af6c240	3927044	b0f74602-92b1-4841-80c4-e20fd7c8c85b	3487598	8ee6cca5-219e-4d21-963f-ebe1e5b036b9	4048010	2588b968-fff1-4214-af3e-76677e2a0ee1	3634794	f55ef076-3f32-46b3-a5c9-395dbaddda18	4066042	6131625f-cef8-4012-bd39-ce31b0cdefef	4044481	2100b70c-af6e-4ec7-9a46-d839e673d925	4205866	b4760c8b-80ca-475b-be1e-725883566ae5	3699945	5a081793-c917-413b-b676-d94262c8e548	3620858	28ffe0c6-7797-4fcb-948e-2862b0318ee9	3542260	04269219-f655-42a7-ba83-eff5970e7c49	3655860	ec447135-a34a-4aa7-8cf2-f9168f445126	5208407	2727479f-7440-49b1-b9f2-b8eacc8e5c6b	3764896	f8fc3f75-bf95-420e-9e37-9acedc3b07d7	3883986	e3b10c1b-9e3d-4478-8276-91af232fbee9	3837516	b523ca4f-fadb-4882-8f10-78113f515b3e	3552731	fbd36145-fd57-4b74-8194-41a11f2a92bd	3749513	4006baa1-ee3a-43a3-894e-af4011a5aee8	13404484	0a616192-6f41-4985-b6fe-3424fbde6fa4	6224371	c6a4b0f9-8e25-420f-924e-1ea26b2fa640	3634432	b1df1ab3-df90-4024-abdb-2749df8f59ea	11861093	b044970d-0d9f-4862-a852-2daa32aa0bb8	3616009	60d79c94-4af8-4804-9842-8a5d2e916d32	4125377	d25a870b-ed6e-4018-8697-6962f7d104af	3500087	27da93ab-3735-482b-bcdf-2b5c467a955d	11993678	486a350f-7e34-4732-bc86-8265612d2dc3	5307403	85e4bfdc-e59f-41bb-872c-a02521ab5123	3581007	c74dec6a-77c7-4a7b-9958-5be97c13b169	3956152	2c1bca28-7b73-4841-9e6a-a536baca141d	3691518	be03765b-81ec-4f8f-a8c1-21874913bd73	4429719	8329edfe-b215-4819-865f-ddc1a236aa85	3514873	ab12a73f-3d4e-4531-943b-27f90a0765c9	3576955	9ced40df-af09-47ff-b937-56cfeb1775b8	3974022	f5800943-d424-48b0-a656-75c00d5acd88	3784008	66e22d87-30de-46a4-b8ad-1e6f59d7b51d	3654184	3babccd5-89f0-409f-a5c5-230bd84bf25c	4381725	a3e97b06-9492-490c-9a7a-c5f2f8e715bb	4128421	77bf3520-494f-4d3a-a754-2b441b5bb389	3895692	12a1de4d-c35a-4b20-ad60-8f7915325fc1	3631519	992bf611-f173-4fe7-8cb4-87c7c7c5eb4a	3932640	df9a5285-b9ca-4933-b618-430939ac4c4b	3624319	1dd7c82a-d460-4f43-b90f-dbdd1d00c2ca	5443996	0d4d86d0-259c-4a6a-8ea1-415f3ca8d667	3845450	95d5180a-0758-4f84-86de-5753165fd623	3500689	2ce280fa-15c3-4f60-97e4-d01fe799ccb5	3835371	c577ee4a-7b75-426f-abee-9eede023411e	3819078	72fe7e5c-c8a7-4240-9ae7-b132f9ba3940	3035195	cb3ede31-2d8e-4388-9430-37c98de77d3e	3725470	9bf60a84-73b4-4d15-9ac6-c3d73307aba4	3433831	345d2728-65c7-4c77-bcfd-a07856f73fda	3654898	55463005-3592-4a16-9361-5d71d7408b46	3772947	21f85e2a-94ff-463d-8451-1c835cceae24	3629750	4fdf6ebc-0213-4344-8696-b77c5c88a08e	3437004	63e50e92-3e37-4924-9074-54298837e10a	4888447	226d0817-6235-426b-b5c5-a1802ab00d27	3645455	45e06fc8-08cd-476b-9537-27f7f91305a1	4015789	a304515d-9b94-4222-8f49-3d1160d3ef9d	3633278	d22015b5-7c30-4ba8-bf7c-3c6b175c1aaa	3748702	6404b208-91d2-49d9-a6f6-8979b4a4daae	3651290	db89b380-c4b8-46ab-ab41-84a192e2ec4c	3003585	23e04de3-32ee-48c8-b547-cd9178d204e8	3565527	1e175f6b-4ce9-466d-a941-b9651bede382	4008800	829a9903-57d8-4c6c-a1eb-7ee6115da952	3682940	43ebbff0-9f7f-4ef5-9586-77034cd2bf20	3866203	32bd35c8-fd24-42d5-a7ec-f5907ee71ca9	4212167	493c4116-9a0f-4cb9-b133-9080c94ca56e	3683698	5d245fbb-11f3-44dd-919d-37642d5263e7	3846114	d438a350-4f42-4f3d-890b-8a3f418fcd11	3847121	2fe02c90-088c-49b5-bb39-a32126f46e40	3961103	3cd49f6b-ed9c-410e-9724-f43e3351fe87	3671762	80837f99-9d02-475d-9c21-d117297396f1	3833767	3ed754db-06b3-41a6-b7ce-a9e4ac42c576	3900854	fc7e7fc3-de7d-47d3-862c-085e06c7e79d	3485585	fdfb9442-eb27-4cbb-9ef8-211dba5202fb	3542739	04fb9abb-2b3f-4373-8539-4ff197f6c9f1	3682236	29de6c7a-1083-42c3-99f0-a2c64e649895	6397134	a84bd2ce-c2a6-4a03-8de2-4e7c63891cfd	4113836	074e3e0f-15d5-45a6-9af8-11fa1339baab	3702199	aa436d4b-6721-4ccd-b8a0-8b59e1e28e52	3763444	628a0477-de20-4f84-9ffc-3ab63a14c1c2	3696049	6e2a4ae9-7e4d-445e-ae1b-c01f03755927	3482593	6cd606bb-4e03-4a9e-abb2-238bf6cb816b	3840625	4b5665d6-e480-4bde-b95f-baf9e7247759	3793466	a163e0e3-2482-41e4-a16b-d9f7975b7fd4	3938990	184c23b5-52f4-4d94-bd08-46e7bf920db8	3453652	b29d6fe2-db75-4dbe-804a-2aca565c341a	4048137	27485851-4a7d-4588-bf16-0a741daf0a63	3868389	7255621f-b39b-42f6-a77a-9e04c27ce03d	3335838	9962ff91-b065-4944-8e0d-4fe694f2c0d4	5616374	fb531c91-311c-482a-9a39-6a4bd649230a	3898845	c89605cc-9f08-4b91-88e7-d8925f817b95	3548502	dc05f7d1-7881-4633-8368-2a1b811484eb	3599972	476e0fbf-1049-40ac-b808-2fedccba2994	3683610	ef96c0fd-20a1-4dee-90b8-811ed3974df6	3644926	d835a27e-18b8-4ff0-a15f-4631a12abaef	3827887	57b7a3bd-f74e-4047-bac0-eb358eab3740	3501636	cffd6305-d3e8-4bcf-bab6-4b6843714a27	3887500	621318dd-6471-4a8d-8fdf-9db94ddaa8a8	3786677	335afdf1-928f-408a-a106-7c8b6e9d7642	3546606	ac541fde-cac7-4694-ad8e-0faf04c6ec45	3599972	ba3a9339-0a73-437f-90f8-766cdb6fe8d6	3100918	e7b4b090-c8df-470c-9bfd-16b11002aecc	3498450	70b00206-6d2d-4309-bc70-55254b462a56	3852615	1cde14ba-98e9-4913-a320-099485038cba	3942694	af0f3134-9642-4a7b-bc48-72a21877e544	3592014	f65d6164-6a85-4a23-a5f3-3f4f2981f346	3613428	b6892013-b8e5-4862-8c08-e46411bcb05b	3781866	15cd04be-1dff-4250-945d-ccabbc5ab642	4174710	8872842b-0644-418a-8279-1fcbf1a4d7ec	3575757	a762c57c-3629-4802-947b-cf6f53599932	3112395	6e81f2b5-e129-4b00-b0d2-9cfbda069544	3660841	b03835d1-735c-4a2a-9123-642ecd231a59	3958527	69e83069-6a23-4317-ab48-06ac203d9558	5725830	72b54bad-df24-466e-be63-8dc61e8f78d3	3730973	b662f720-1859-4261-a93a-45882bfb064f	3632101	95d7555d-c9f8-434b-898c-a37d625e7830	3546419	3c058c7f-3019-475c-ada9-b3904e1c10de	3519692	f6551684-9fc7-4993-844c-76d8b370923e	3665040	08fec83f-0d83-47fb-bbdb-5a0d02407029	3953971	75335b9a-5d33-4f48-ae31-5ae2564e4a87	3718045	0cc1599c-abb6-4cfc-a8e9-32250e7242db	3536360	26f5822f-ce46-45f9-b6a1-150a38eea81b	3640400	370eeb5c-8a90-44c2-a740-f9765a09e211	3805085	a45d6831-e943-4bce-8cb5-cc989ede8341	6817654	eb9692d5-63c4-4c1a-94b9-d90adcea7b62	5220236	5441cd88-d7d6-4f4c-a71a-fae9ba3eb7e0	3636524	8231b4bb-877f-4a98-8a28-0f861f522ea4	3904803	dbba7d73-2a9f-4407-8d00-2d15e6b172af	3464967	5d1832b4-3968-4716-a1b8-33eb97662dab	3861418	b89b967a-3bf0-4320-aff2-9da35d6645c6	3612905	6f8c895d-17c6-4368-a5b8-69ef5bc8c501	3930309	fcb1abba-040a-4f78-b86a-bff6d5d7c3de	3607544	d92e99e1-1c1a-4ce8-bc21-aa70bd1a01ee	4105145	d6f57ad7-4f11-4648-865a-30d6771b343b	3717258	1eae9d6d-5ca4-4d1e-93f6-2798a4138fef	5645256	d0fc97fe-1a1d-4178-ad1d-24ddcbfedeae	3852893	b04aa9db-adb9-4502-8375-a895ccf4a640	3843973	92266d58-141c-4cd5-8fea-1c32fffd992a	3740632	d79e5d66-d5de-4a9b-a122-2e1fac3bbfe1	3071253	4d7c150c-91ee-47d9-b4d4-71ffa67adc47	3708567	293f3727-f954-493a-a217-a4709f925bf0	3504667	e271fc38-6310-4b35-b163-f9576f0f5d96	3834519	4c307901-5c5f-4de1-bcdc-56bc15fe848e	4064111	0ffdf01f-cf91-4155-b334-cb17dc6fe34f	3587576	79dd019d-41a7-4e52-b0f8-e4f15dcb6336	3706148	2c92c1a5-7bff-4aca-b476-6e24e0525b2f	3450591	83becdc2-34cb-4f45-ad37-e5dc68dca22d	4118148	e268faf1-98cb-4d20-86fa-7e59dc5762d8	3453866	c7755015-4282-4eac-be1b-1a0a4501941b	3505249	486147df-f4b0-460c-875c-cba21092fe7e	4705047	395adb25-ef58-4a28-a324-6c4d41b179ac	4802596	0e53b2e2-d0b9-482c-9894-9584758a8395	3793094	b908b7dd-c93b-486c-9b29-4e150502e8db	4005426	d31d9f38-db15-49c6-99de-747ef7da3478	3505121	f84aa4de-ac58-4d6e-97fc-20a2eaef5e54	3916192	d81af6e9-86d3-4b96-9690-42f4c751f426	3457250	7961d577-3762-4fa6-92eb-c5f5af309358	3530617	8c430b7f-bb87-49a4-82c9-f635f0cc1796	3565888	f1e3b2ca-2ff8-4785-8194-46408198ed51	3618640	3c6f07e8-5982-4100-b073-e3482570fa35	3808619	61e77e21-6124-47b0-9512-a0f11c230354	7236652	9be995f8-1d05-4e36-88fb-b1f0ab7e236b	3869258	892e8ccf-c316-402d-a8d0-8ae079dc6547	4241661	7fdd598d-e8a2-4ed5-8844-0e5c478624ab	3426592	b964ff34-7e5b-4bb6-9181-f92e8d30c57c	4023370	40919d80-972c-410b-a7dd-1701d2042ecd	3514820	0e2ab9c4-8907-4c6b-a046-76dcd7d21668	3690672	192c5a2e-1a00-4beb-a43d-5f41cbdc851d	3503362	cfe190ca-821c-4b24-bfbc-ca6c90dd65c9	4151777	78a168bc-6138-4db4-b042-f63d1882076c	3697032	90d869a5-dcb6-4323-81e6-21ab89fc5e2a	3751669	b39f135e-a44b-400f-94e1-c68277b453f1	4089372	a67230af-b64b-491c-af6f-e28eeea796f2	5980541	9b7e9fcb-ce67-4b09-96d4-8a26b799ee08	3619553	a132ce1d-883d-4f3a-84bd-084c698f17e4	3642526	55e640dc-8587-4345-8af7-ece950de0247	3737411	2908d474-dcfa-4ba0-bb45-deda2f95389b	3800837	3aaf3d40-af29-4f2e-92a4-d04d6e2d9b40	3839593	e530bec2-0482-4160-932e-081f3472da86	3534640	317e3870-5c4e-47e0-8ed5-0fb689975274	3522469	5a7b9ed0-0d20-4608-83de-5e952e84ef29	3753444	359613fc-0a03-4856-bec0-299c94e09689	4288570	bfcb656a-9514-4166-b533-6c47119d99ca	4094129	8a24f20e-657b-402e-880a-4ade4d3f8f53	3615516	2c56b979-282f-4225-948f-ee2b508acf9a	3790582	00bb2954-7b57-4f8b-bdb6-18ba22f53f0e	3649643	1a0ea727-72e1-493e-b468-7d0f127fbd26	3887690	2cb717d4-d128-4cdd-9550-c18829d449bf	3304984	b6f5c4c3-54ca-41c4-b887-95ceada2636d	6397202	822100fe-346b-42a6-aa41-1db386e17a77	3796599	f99087a1-9b5e-443e-85eb-cff6268dfe1c	3719077	68a12e1f-c60c-470c-b7e5-8193968d877b	4038053	b68b8d8f-3e70-4bf3-a596-48c2026f773d	3868168	6b3f8f04-f46f-47dc-a490-89a5aa63d138	3512522	0c31caf1-54c2-4636-9a35-7f2d46151910	6100163	52800f07-a8c5-4452-8bd8-d9908b4de1ce	3547119	6b0d633a-1e8f-4ace-8d67-6e52c36cdbbf	4656192	91031de2-f234-4825-8fda-9971b968f62f	3589439	b5ca3086-dc66-49c1-b5bb-c775ef31da73	1786043	f19b07ad-2b08-4d54-90d1-deef907f6b8f	3777840	e70758b3-c5ee-48c0-85d2-259a433fbea3	3642453	32006ed5-8a1c-4f91-8096-77cdf57684c8	3450685	58b19770-9c05-4304-8d02-69ea6b5a45bc	3891401	c6926329-d13b-4fed-9134-08d848570f8e	3569980	c312aa7d-1ccc-4b6b-9f50-887ba16d99f5	3575659	4176bb6e-1c54-4ba5-b85b-c0043c18a3be	3826040	6985966f-be48-4e6a-912e-672947331bc7	4760653	378f82b4-f522-40d2-9591-7a66f842e3cf	3840527	74edac8b-7200-4678-82a9-bd1198294641	3784042	fe413a30-c35d-4f32-992c-5395878f1927	3663369	c6bbc521-acb5-434b-8441-6a8a202f270e	3734117	48b408c7-94d6-4875-93fd-020120eb1e9f	3758884	3b419b49-0c87-434d-9094-bb46c655c0cb	3844349	5fc05738-6173-43bd-bf43-9b367c8acd75	3659801	8db3f773-4bd3-4915-8592-507b8d468114	3688781	a96bf1f3-9681-48f1-a1a0-16e61d90a9f8	4047766	d5a46652-90e3-43b4-acdf-8aacf11feda4	3583211	cfbaeace-5331-43b9-8207-9e2d869b254e	4346341	6952d438-d327-4d45-9243-0056ca5fa78c	4031797	3dbace0b-ddec-4383-8474-a30e80ca03f2	3656927	1130eff1-fad3-4e02-9e04-ad28a2377e63	4103049	9d940530-7890-47f9-8128-c822f925fdc8	3674919	d397e2e8-99c4-44bd-94c1-03a18592bc43	3724107	4f3d9bc7-b79a-45b7-96ca-e9a8a99dc25c	3670036	02828f36-5fb2-4f78-a53d-4c12a3f43af6	3602553	48478f1f-0e84-40c6-9f5d-29799bf594ec	3699456	93482329-31f0-40d0-88b8-21ec6cdb18c1	3617535	24d33b31-0d90-446c-be4d-d48555b50c08	3699359	512fb0f8-fc8d-44fe-8692-4e8079e98a02	5234607	9cefc899-927c-4202-b13e-f982fe0192a5	3091573	24cd9f3e-9a0d-49da-8de6-2efaba5447b4	3224271	635ff031-a181-4f43-84f4-803ec9e4e76e	3578655	6cb6377c-b933-40e5-94db-506b43405f42	3859762	804149f6-55d4-4930-8995-1a2ac016bd2f	3421435	74a431db-942d-4a5d-8105-c4cf5397ac44	3909296	a3055c68-9e91-4698-a691-e98953f0dd64	3595192	634e9a1d-3827-49ab-ae34-8bcee40fc671	3660323	05448366-6751-436c-86f7-4189e0a83507	4422715	220fb05e-a30c-414b-bb5a-96213dcfd2ce	3413858	928a6abe-7fe2-4a14-8aa4-6b2c928097c2	3582429	5d9eb678-bcd0-44f1-a06b-72249cbfd827	4485134	f31631c1-3250-4956-b873-a07c36bf8159	3675535	d15fcbf9-c82e-4866-beed-9e01ed7281d7	3741233	88f82350-3bf4-4b6d-9d79-1bc34dcfb62f	3607074	80456012-fd3e-48ae-80f6-b4dcf160f2e4	3641998	be8d1eab-e24d-4da3-b473-d1f5e0e5f984	3603237	d7ef22c0-9a31-4733-87c0-1d799216b47e	3586237	a8492550-290d-4727-8584-45eb33d12651	3764784	646bd3b3-e022-4aa7-a6ae-e0be9d7bd712	3567805	2ed944aa-0173-4c86-b672-d6c797fc1c53	3844853	1ef0cbcb-e40c-4ff1-89df-c1f8cd550899	4004219	cc5a6be7-7bdf-487e-93c8-c4f10a2f9c36	3663036	47ff9a4a-4307-4bf9-a27d-3b86b61b3387	6482654	bdbd2d5e-cf48-4f6f-ba43-ae15a0c38734	3793764	a2ad69ab-f333-4dc4-bb87-6cb15f7310a8	3748717	0ecdfbf0-e198-40ca-a0f2-463ddddcc4e9	3575019	2e682fb7-5616-4e8f-8097-161e68b4533f	4643953	45bb318d-7ca6-4de3-b52b-44f72f734fd4	3685970	3b3c03a4-344d-483d-9e17-2683cce60ca8	3850327	66ba5e45-6925-47ca-8f59-96586664b4a3	3452704	d22045db-2637-4e21-9bb9-dc0161662c31	3739362	086bb0f7-06aa-4e3a-8865-aacfe60211aa	3692662	16979f04-6158-4918-88b0-b137ca59695b	3774095	701cac76-fd6f-4631-8e0c-1bfe14a643d3	3637316	8c7179c3-9db0-489c-8237-c39c34849ba1	6218314	e8349623-ba0b-4eb5-8282-606dfe62370b	4022525	7de0931b-51c8-45a7-bc0a-5156cac8f90f	3234579	8099d015-9cd4-4c3e-bc12-7ef3dc556af8	3507209	fca46804-aa43-4bce-a3e5-d4e3a7f3bb1a	3477627	044908bb-c5c6-4974-bbbd-475904ffc08e	3506476	aea1165d-7e3b-4ac0-9485-8705a5fb4ac7	3831983	413d9194-ec10-4bd7-8916-152289a4993e	3779120	cd8b3a21-4150-4cb4-bea2-d8e8045e7d8a	3556411	d69b4ea9-c9e8-4fc4-a5bd-b9def69ee228	3864141	bed96c0f-4807-42ba-9ef2-46a4a525d807	3907995	9b805870-24f0-4802-8f92-bdcd22b956bc	4987487	16e9ef5f-b7cb-4b04-9e12-a20fe657e179	3868007	c53a2121-8d01-40ef-a581-f049d6fcf392	3630209	9893c60d-48d0-4f74-967e-4341ce71c99c	3699212	a1199cfd-51de-4bfc-8de2-88144750ae83	3785171	19f66505-cd49-4d09-bcfa-3298493c8db1	3443181	7af6080b-f658-41d8-a840-7ca173488d24	3685476	44f7d73f-109b-4b91-b13c-4cbe58bb3d6e	3674024	60a198be-ea72-4924-b35f-c47545a097ad	3709955	697b3b2c-8897-4580-8c16-97a3d63a8114	3874772	0566b8dd-bd58-4e66-bb9b-c121ab8ef19b	3594800	73606af9-50ab-4f6c-b958-48db8e7772e4	4853577	36da2ebb-457d-4f67-bb5f-8344f8fde2a9	3779657	7e884c34-1f12-4468-89f1-1cacde00196a	3663975	5f0c4014-72a2-4d41-9710-59366882e5b8	3972018	7074e922-94bb-4b5d-afe4-317fb4766d4e	3834612	a60c7d5d-fdf6-463e-9a32-8579396c60ff	3748394	e17eca3b-76e3-4248-9d26-eb86336d7255	3773660	f219a5ad-003b-4bc5-971e-333585a58c1d	3599528	d84a5744-4de8-4b2c-8230-093a0d38ed54	3956445	752e9557-592b-44f3-8693-19f2a40b3ad0	3839398	4bf1bd8c-b6b3-41fc-be87-fc225ff17878	3815652	f5074855-4446-4301-b7b4-765c912b24f5	3571974	eacd98ce-066d-4a89-8044-828d302a5be6	5428295	af7daaf8-b58c-4117-b7d5-636383ecb588	3576255	77274043-5f3c-41d1-942b-8c4cbd96fcf5	3659043	bfc982bc-e707-429e-83d2-02b5761053cb	3784863	d1b717be-88b6-4178-b1e4-0b0ea7036267	3604127	5cc0d9cb-9a08-4016-84b3-c90c757e1f72	3498078	d542819d-6e9d-4e62-9a6e-82d3c82cf968	3681464	3e9b4d55-c891-40fa-9a5b-18df303a71f8	3601346	fbbfc24c-3629-4f78-b35a-1a38552f043c	4137107	76766047-a460-44f9-8891-5ab622849027	4045904	660263a5-0f1b-4d75-908e-b194ca6e9c36	3125852	317a2fe2-fe74-4e1f-b2ee-f893fc5a46cc	3779721	ebcde418-f95d-473d-9f15-0df504012a2e	6252280	c655609e-3ab6-419d-8caa-68056de07bb7	3582952	cbf6132b-f386-49f3-836b-2e981a7fc782	3669537	08487441-6cbd-49d2-b149-6252b1aa6622	3251511	84725ac1-c940-4e1f-9018-042a04b76850	3765184	df68ca64-759a-4c75-b018-7a28fca08e77	3630502	605b51c9-47b9-43d1-87ee-b5eeb2ed2b65	3669811	4edb7df1-ce1c-4651-b985-b1020071e823	3667382	120a9d36-cbec-463e-b91d-3679ff8b8c12	3671336	82937ecb-9768-4d21-a455-3725a10880da	3288155	e864bb20-cf25-4533-b073-1aa7d2393805	3846481	8f88fc92-1c69-44e9-97ad-cbbb4c9ca388	5049295	cbfaaa7a-55ed-40d1-a0fd-9fc13af87a01	3581158	1f6bac49-9ea5-474b-850b-e7f13d6d126c	3604576	f6aef2be-652a-4dfa-aff6-0aa296e8e1ef	3562589	bafa4147-28bb-482f-b59d-a1ec4440b185	3889479	6838fd67-aecb-478c-a902-aab8e3a33a72	3582312	ccb2f8e6-6f46-4ed2-ba4d-88d702ee5d1e	3820418	503f8e51-36bf-4341-b364-22751a32340d	3520347	57b3fdcb-78d3-41a9-9744-11ce5f6bd548	3787732	1d2d2b83-39e2-4fa2-92f1-74a98e5dcbfb	3095097	9ceeaabe-0b34-4738-beae-76f56b327713	3730739	6e3d8e70-ce7d-4aa3-b935-93ba746428d4	3758586	21a1e6c2-8cf5-42e9-b827-608df46d2967	8792811	09a06473-e157-4add-b011-2b1630a748e9	3636441	3a0230e6-72cd-4730-acd3-0dadd6e6dc11	3740109	65246285-9830-4589-9b2d-4f5cd96ff625	3719048	e2e61347-bfbb-489c-9e2c-155a171ef2f2	3699667	8a479376-f473-46c8-babe-d09de7e30cac	3880975	2ec41d66-f2f0-41e2-9424-8e4ac3249cdb	3731164	6d44d348-682a-4ea1-b8ab-9b69d718aa30	3578881	35f113a5-3cf8-4a39-baf3-9b880c49dbeb	3774940	22d8b19b-1439-4e72-b97a-9db8ce86a807	3551914	5cef52c4-0731-4208-b66b-7faa957ba583	4010637	aecce6fb-fa79-4e59-ae81-a8640d5e25d8	3725807	20abc9e2-9939-4f5d-81b6-dddd5571de1a	5118087	c1aeb995-5db0-433b-892a-2c615a916466	3640146	85862ad6-d42f-4a4c-9c0d-3f5d4d5f1b14	3731712	da5e7d13-97a3-4582-9900-811a14fddba8	3503866	d6badc39-5fd7-4554-bbfd-3380a83974fc	5577793	55801e09-aac5-4f9d-9e58-e4548c96fbc7	5382227	e49ffe1b-eebf-4081-aa8a-edf97baa1aa3	3939361	cf5961b1-4cff-4403-9822-7bf61d51d0d6	3710283	941e4aae-5125-495a-b7bc-1408e6fedf3d	3579052	d7277b8e-b537-41f1-bd0d-f6eaaf251ecb	3675447	a4f89137-0bad-4c59-95af-85910ef1ebc0	3820682	ea04dc54-0e09-454a-b5d2-876a51c54523	3631861	e362dfd3-581c-487b-bb1f-d82b705acbe9	3774163	187c97cd-20ed-4438-a69d-818c766d9858	3633826	e16268b9-d17a-4bcd-ab8e-268278ba969f	7621661	32d5da0a-8517-433b-85fb-6dcb7ada62c3	3541835	0788cf26-4deb-401b-b5be-78d60742fa23	3535975	2a168de5-461b-40d9-88a8-6b7223930de9	3634959	d424ee75-3059-409a-9aca-ad75bb980536	4080041	6924a430-3f44-4ced-87d9-e43e28b54fd1	3025263	5fca37fa-c589-4533-ae4c-9f5b2d042fc1	3592166	284c1085-78bf-462b-a44f-419753112344	4008418	f3a51944-24d3-4a41-ba1c-2260e01008d3	3529073	b55a4520-7fc3-4ceb-8412-b7e1a28c9dfb	4119286	b38a066e-3e46-4946-bc42-5fcd651eaf33	3886498	76272ae1-6eec-4749-9bab-c128c7cb72b0	3737992	ef2ec1ef-1f50-440a-8821-b7a3bbba64df	6124988	0f587693-124f-4912-9f44-0a140814fc93	3569100	41f8eea1-c60a-4e51-912b-bda20ea073dc	3607216	4e128313-5588-40e2-baec-b1ba23937ac8	3653104	22744799-ad21-4993-b50e-0d218db4e4ae	4118856	67a7d4c4-e7d6-4cb7-8ed7-97daa8e9690c	3795714	3b6e81d6-a6fd-473d-b316-6624ad3da53b	3786515	1d6a3d52-9a86-433a-810f-0af015935042	3723940	74506215-d8c2-49bb-9508-683134ce897b	3617661	bf443d01-fac1-4762-8ec3-70a3c96a4b68	4024221	77ae5c0a-11d8-4b89-95bb-9dd3d6047adf	12061694	b1334d34-a81f-48e7-9a2a-5ea8232a8a69	3796507	0059088d-4334-4678-83ca-5239c876b5b4	3670241	2d100f03-ff23-4e6b-9f8b-d2adce16d165	3713979	bfcfdfbd-b4d1-47d5-86aa-ecd391deb728	3617911	9b2959e4-c898-4f6f-ab73-759223ca2c4f	3604684	f25f9e19-4007-4e94-b770-c439430effa6	3762921	f07679c8-0d3b-491d-9f8a-0129f4852f45	3460622	c1c7cec8-c795-4d46-aa5a-6337d220e501	3408614	6bbe4b54-e926-4964-9e09-9b5f0c4d0ca1	3603975	306437f7-571a-47fc-8bb4-0c8289fa376c	3511212	08a3cc05-4880-4e62-8e0c-9c469215227c	5911415	07e59fc2-c824-447d-bd0d-4b6b265b2253	3573318	7740f2a3-5501-42f2-9f83-6b2b93c1bcba	3717116	889785da-39d0-42cf-84e3-fb9fcfe261d6	3599869	eacc7040-542f-4f56-8cbc-6a2024349416	3734288	fd4798c6-7e44-4fbc-9170-4441f651a2c7	4124272	883814d3-9e08-471f-87f3-2feddd941353	3666854	8aa343d3-3397-42ce-868d-f3cd883accfa	3636353	466b3d17-b4a0-4e4c-8d01-e50f8700f61f	3598784	9355ed3d-6bfa-462d-b90c-dd55d86bfe0a	3740207	1d5ce0ac-caae-4d22-b87d-be7707dfada9	3517640	ca1f2f63-79ca-497c-806c-40e63c9ee288	4174007	c03f77ae-85ca-4b52-b0bd-43b4348c543b	5125865	fc7c1c33-10d3-433d-8b1e-02dffb504658	4319775	4e218c34-188e-447e-82f7-0d67f08dcca0	3191359	2eec83f8-5f68-4ddd-91fa-28f137fe5400	3890115	423a5424-f612-417f-8e33-b2f749149340	4049095	67b82ce0-799c-4361-9325-69395d4d0cf4	3629260	b0ecc567-da92-470a-acb7-7f68308e6fb5	3607108	56c15d35-da8f-45e1-b1c3-7980d05da7a6	3684929	aca46259-570f-4803-9a24-255c8958e9df	3804161	6685b45c-f439-4361-af7a-99ad8d3f3a3e	3666340	4294c6ad-ef47-421c-8b0f-c3932b7aa485	3663125	33b6edcc-408d-45c5-b633-46bb49f19466	6134559	b0e0f2dd-0ee0-42c8-b87d-6b3826ef626b	3839813	2e50298e-42c2-469f-9b57-8e4c1dcfd963	3627511	36a56671-bb6c-4012-b260-25ac84f48937	3773587	004d7a13-b358-4c70-8f71-cbb8294a062b	4034642	0d125a72-f695-4379-87ca-638879aa2ef8	3886117	81916700-9eef-4b21-acd6-1e59e5acbb96	3863574	f86ade00-64f8-4269-91d6-eeb342b3593c	3477592	20ae7c31-c5ea-4482-90d1-1dd01ddc5ce2	3559334	e2a01def-5715-43f7-bc84-d9b4a9b829b2	3792435	6a334fc9-4777-4e0a-b7ef-98654cb5ad5e	3711598	a6915f40-bb30-4bf1-888d-3bb69f322002	3634886	8aa8587b-f350-49ad-b4fd-fdb9f6c5b51b	5161356	63c8ce8d-22ba-496f-93b9-f3b7933084e5	3455357	a748f0eb-aac0-4cef-8ceb-df9f740cb5e4	3877807	7800f1a4-ed61-4dbc-8dc7-d78763460ae5	3810999	7566066c-8b50-4cb6-90a4-7377486799ba	3542177	9934efef-2a1a-45ae-b92e-8f5eeac52d6a	3752090	26f6f9c9-645d-4965-b39e-d97b101c0b90	3878692	f7fcb4b4-b502-470a-a299-c7e76fa1e5e3	3586276	6b0f66ad-66e2-4f66-b822-af4001163e9b	3561288	bcd34090-f58a-45e3-852a-e2f2777c6359	3658936	acd0c2a3-26a1-457e-aee7-6ac12ab401d3	3997205	05b85560-ae7a-4e43-b7b9-00b3fbcb60b9	3683732	2087c597-adf1-4d1d-88d7-9cc001602445	5703330	401ccbb9-5535-40e3-98b1-3feb5a2ddd2e	3588094	e47c9b52-fe87-4997-aa79-ac7a38307c88	3490033	26de3b89-d282-4769-90da-b3c35d2154ce	4217582	c0812a65-2cb9-4923-a79f-4a5f331a4b2f	3732611	65816c37-93fd-49f0-8220-41e1ed4dff12	3577707	042dac62-8f1b-4a14-a980-e512cc616ab5	3797723	d8658d8a-f82e-4acb-b463-9ca21f2307a4	3750516	6f68b89b-99ad-4514-b5a6-8260bcb0aab4	3833875	b7316c48-c13e-4f23-af5d-ae12abc8ec9f	3729498	23439f24-6f1e-4b56-b4df-42c517a722fd	3454594	6f2aa625-3cec-403a-8c3c-dd4da9222ff2	3674264	a4a6affa-6dc9-46d1-b18c-4dbbd0cbea87	5093995	df4be40b-5fc9-42b2-afb8-1bb13b1564f8	3759617	54eab148-f7d0-4c01-b3f9-abe648d75365	3489143	60373eb4-f133-4456-a406-23abfd73d8ff	3865241	44d4f12d-94c8-4b3a-b698-eb29412175cf	3727020	51c9d661-54e5-48f6-b422-9d5a692d4935	3465070	d0de2840-e1d0-44fd-a268-19f786e31776	3556675	fb42d2ce-0589-4e87-978c-d818e5940979	3544728	2cf42e32-0465-4efd-b3b3-36c61b55afd4	4107951	c283794c-7f9a-4953-a23b-1ecc12fc99d7	3377957	6da96868-58c1-4951-bb26-bccb8119b7aa	3894710	424e4931-bf43-41fd-a385-58d260251f40	5500984	d314c240-c119-48b0-8e03-d07c83f62958	4285340	803ae98e-aa9e-4800-b274-c9027fa133c6	3885232	84a111c1-c69c-49bb-900f-20edfd2c035a	3321300	474e8aee-0eab-43b5-8421-c5dfbd6fc30c	3699950	ca7e29c6-6357-48a8-8096-f36cd0e4c353	3288645	7860b05c-3f32-4c34-a1ab-b3796d0a510a	3528628	93fc6616-e8cf-469f-8296-f0e8853b1b4e	3787947	0acc2b21-fa27-4886-9f6f-0621ed3f001d	3666199	e62d0fb3-1fc6-488c-81e2-1a488525bb75	3675427	23246f1a-a5d6-4bc0-91e9-af176b9e0eea	3913666	a096962b-2031-408b-b4da-e7203c0ee3ee	3561274	d3b2ded8-79c2-418a-87ba-f3c40b662ff2	3910215	04c3321a-b026-4e05-a619-7b67643aa7dc	3808443	89994b8d-d185-4016-bd16-235501edf7ce	4108591	ee4ed49e-0980-48cc-ad4c-972693943fc7	3875926	53250f10-9b48-485a-9f1c-593931e2124b	3640175	54637676-efb4-4f1f-a9ca-8b6e1c245363	4029915	b9cfb945-7f02-40a8-a0fe-21b5857416ea	3638768	113e6e58-7d8b-4545-8683-ffc7d4115696	3913558	a7566514-ab3c-450b-8afb-0ff0a36a1894	4242579	235a29ca-d69e-492c-9a9d-559c28307cea	3878428	d5d9942e-b697-4310-9794-ec071a6c5962	3710180	9796f0cb-fb49-4121-82ea-2032528b9fe8	3484206	16a0d4cb-460e-4c0f-adb9-52289ea42917	5738660	fe154ca7-54d4-403f-a4cb-9cbbef8ee889	3742245	c72dc3bf-237e-4ecb-a113-24c09c71810a	3836964	15bb0dd4-7689-44e4-b2b9-99485642f72a	4056525	37d557de-4350-49a7-93f1-bb6a7de52959	3705195	e2d2b8ef-cb3a-4c7c-9fb6-f8d456beea5c	3761244	8791c924-810c-4a2c-8b82-1cf91fbd23d8	3820970	91c1c54f-78b8-465c-9600-b339e08f6a14	3794390	f9892270-a711-49a9-b94c-05ec58687b98	4056647	7aab2de0-c113-45fa-b41d-2be057515fd4	3845454	b0574449-f78f-4052-a8ab-788deb9c77a3	3673780	19cceaa1-53e8-45a2-833e-6da4e9c78ae5	5242256	02cd0c35-d769-4dfd-9477-8aacdb2c3564	3944430	39e71b6b-17d3-48d8-b633-acefe835ce20	3398345	b1a6bdff-7492-4a68-9287-d94bf907abe0	3787996	228438c6-aa42-4bb8-bb1d-09a3e1d2e474	4027129	a3c86f19-47cb-4536-a063-59d68d94d2b7	3470305	63af5656-bf4a-491c-b175-311a2ce9d081	3649956	6006ced7-c7ae-4418-9a30-44163840c4bc	3707365	d6674af2-6d30-4a88-830f-7f73b6a04936	3274283	9ae11be5-24d1-4aff-9582-99c2b8252428	3693498	1afd6437-da40-4fc0-8b57-b632c09c03e8	3618380	39c4e577-1334-4604-a4b2-89a96a3453c3	5263064	a9b88e2a-129a-42c7-9b1a-54cda30ae2ad	3705175	769005fb-0d2d-40dd-887e-5a6ee8b2e510	3525895	6691389f-1e67-4246-b641-055c51537ab3	3649179	cc250520-b0ab-4187-bc97-2ce6900a2d01	3698112	da122da8-7076-44ed-ba8b-e81531f288aa	3628024	8fe4fd06-f566-4492-ab7b-b384c8642767	3474430	f98102ea-9db5-438e-83b6-b2a7d8d29eb0	3752017	303d52f9-93ee-4a01-bf42-9bc002294750	3602802	cfaee5fe-13ee-4628-a6f9-639fd13f5e0a	3787786	050c2515-5bc7-42af-b5bc-96f40d06a56d	4001985	38e40641-bf1a-4841-90b9-bff7333e2c5b	3611918	5cc7694b-8695-494a-ae08-5b2095a1cb41	3573665	2d2016d1-0ee9-4826-b675-07e2877c1487	3339069	76d16600-9320-4463-a610-7e726628ae12	3387005	a8351ac6-80ad-4735-81d2-5c71a3b8ee7d	3635605	3fe76d2a-bf45-4664-831f-c7d0dbbf4e79	3520264	2dbf2005-ad4e-40f2-a5c8-a8e94fc5bbc3	4102779	cc82bd25-9130-480c-b9ef-c7badd9fc14d	3728031	3c09dc65-2b0c-4f3b-b911-46faa9fdb1a2	4017944	e0060b94-f04d-4028-9353-7e41d3c4b9a6	3776466	2c6aabae-26e7-46cc-985e-086a1d9bb215	3677924	b3e63ba0-bd25-4e50-9fa9-5aa188da0390	3219749	db542db7-c31a-4223-ac29-f50914e569b2	3734160	7b61c190-d976-4798-b25f-2e52b8ee3c3b	5567554	97fc0211-e156-4626-8b28-164ac06b7941	3508680	bd10cf5c-c7da-43b1-881a-552863c70a4b	3539646	30b4f717-ca5e-4699-b63c-47bcfa3bf689	3709482	40d8601e-0d17-4e7d-a2c7-206350c578a6	3649179	c56f58b7-4eca-491c-b085-1efd37e40b83	3708171	373d01f8-86f0-4ad1-9965-be78a8448f9a	3890609	38719f74-8b48-40dd-9d37-4cfe2db60027	3539949	7e25aec8-be1f-46a0-b229-d638fb7fdff6	3558409	d03d50b8-88c2-4ce8-a1f9-83664a5cf8bc	4316358	c46b25cf-5ebf-4621-860b-37909d0e2eec	3739816	a96529f9-a3a1-49b4-96f1-e6713773c282	6060624	46bad3c8-e42f-4554-b68f-cd171466de2a	3758781	1e005b34-08ff-460f-9794-df0983ac9f3f	3654341	dcdbcba2-56a3-4ef9-b6cd-5378d6b4efd3	3441725	a233787c-aaa0-4435-af6e-6053f13ff069	3908592	ef6342de-2e29-4606-abb5-acbca741da42	3622286	42f04df8-ec20-43e6-aa5d-34536c9c7ca5	5252653	73af6200-0352-46b1-87a7-8c504d680446	5232387	31bc663a-4e89-4ad5-a17a-61e1668cb7b0	3468360	4fda346c-4dab-4df3-bd9b-60630d3ecb61	3389957	b6d1c7dd-1b60-4e30-8ae8-a2df514e2705	3641993	10ac1ac8-3269-4f19-9443-9e0f22d278b8	3845263	08fa7d47-f088-45f4-b1de-db9479893580	3494910	b8ac7d2a-84f7-410f-9993-556d7441e8f6	3864248	61505593-0399-4c25-8f66-e95ecd858d60	4037848	d216a14f-5a52-4cad-b4e2-b6e7f1235d7e	6580144	9df3bcbb-9ac2-449b-b677-3ab0d19208f1	3668310	5a8d5cec-35fc-4d84-bf5d-f90856df9c02	3708005	9fbbf08a-6bf1-4dce-8313-6262b9228ac7	3111643	e379b6a8-e1cc-4fa5-be6f-603b9a41ec53	3772438	599d56fd-94cc-477f-9fee-e777eb6ff862	3575664	49f0f218-33e9-4553-8ab5-ed5fc5337735	4119569	7f39a0c2-bc0b-4641-b1c1-12edee70f7da	3670456	72ca36ef-3808-4c08-9b6a-6480b95946f5	3533433	8799b73e-6ad6-4ec0-ae55-39e607eebee7	3451428	0762c4d2-c502-449c-9169-c8ba6104982a	9289929	5d6c3c31-f889-4230-963f-e32e25781419	3420668	18aaa398-37c9-4925-9bbd-2c58fadefd52	4659785	4a8ef2e9-05b5-477c-aa8a-ee374667879a	3605867	771d0365-4bd4-469a-a4fa-0e68effbf28f	3829353	09295c31-0230-46f1-a6b9-232212643d0f	4817470	77ff4b2b-c4e8-433c-a9eb-9a9596392fa6	3571240	016e40c9-29ef-4b57-9cf6-9f8df291171b	3861511	b2d2022b-50fb-4565-82d5-1cd3224902dc	3930192	57913f98-5326-423d-b63b-fdbdfbdc6e98	3798383	787ce842-82ff-4cbe-b7a4-7d91b19cb98c	3792586	e4c04202-0847-4b93-809f-96be64928b7c	3886781	27e63b8f-b6c6-4c9c-9478-4f69310f7d6d	3539713	8aae863e-13aa-43ac-87ac-2d3bd961e260	3590979	dc7b0c8f-e722-4505-9464-2dae706598b1	5094229	ce4e436d-6f4f-4c27-b31c-c3daf7cd2996	3924453	6be299d2-efe7-4a19-a8a5-b89f93a287aa	3608330	e35ef426-e46d-435e-afc0-b6f6941d8d4e	3866629	1bbb5fbc-e697-479e-9bb2-388ab2d132ac	3742891	8fa7f531-603a-48b0-bc20-e3e3781e8ce8	3816068	77c6f866-a0e3-4123-8d17-80c7dc438ee2	3828112	16431fee-0bdd-4a4b-a7c7-0d9d4c489c27	3663310	a7a88179-e60b-4d2e-9107-0f5b1e9a0e25	3602084	7eab1654-d246-4a21-a883-6dc9766cc5a6	3650176	0695e6f0-eab3-4d54-9849-ea0df411f13c	3583436	4feec5ad-8d72-4afa-80fd-5d0e3890901b	4392605	f80dff2a-36fe-40a7-8e90-448df8190648	3732558	ac0ef7aa-3e35-4002-8dcc-774df5a3be2a	3561416	7e0e30a6-2635-4fa5-9c18-e3a57f3b71f9	3677363	16b8e319-3d31-4243-9efb-c91e67e80f58	4769851	2c9299c1-e25c-4216-a051-4c5a4bdbfbfd	9179995	28b76d9c-e898-487f-a3ed-89f34a1c0193	3793148	8acd9c64-f248-4a7d-bdd6-6c1be0befae1	3392015	7b6b4af6-249f-463b-b796-2670ae97713c	3592063	0b699782-fe51-4eff-8a3c-688ebc391c72	3592259	541f5258-83a2-4a40-b22c-0025d6cbb000	3841094	afdcaaf1-6d5a-4470-bd61-f99762b4e06d	3650518	6b7f0edc-dcab-4f00-a641-2cb994897b07	5429371	386448c5-48b0-4957-9aa6-8ca548100cbb	3653026	63126f97-d555-4839-9c85-c9201e473d49	3508347	7963a13a-b5cd-43d3-bbb0-44617cde1c99	3744333	a53de23e-79cf-4020-99b5-834059cc892d	3985195	736f0dce-6a2e-40d5-9476-a4150dbaf490	4548961	ad61b787-80dc-48f0-aacf-74912a4cd8a4	3607358	112d1aa6-d787-4036-aebd-8ca77de6eec9	3572814	72df7f5c-d0eb-467d-8781-a30bb1ff8ebc	3513763	27601d42-28c4-4ad2-a21b-08d2d3cbf6b1	3513494	54ac1507-9b3c-4126-b855-d065bed27da3	3733349	cbc382ef-e964-419e-812f-4e6e34e5a482	3915127	ea3a29f8-5f04-4ade-9bc7-4f91b0044ce3	4669424	2de545c7-66b7-4858-8fc1-1def9a21b197	3591907	082985fa-a549-4714-bd2d-50ffe4b3d862	3739176	c0a0a3a3-452a-4ee8-86ab-849461f40154	3467357	f0001792-7f23-4d65-bbc1-111a2fb783bf	3740212	782cdebf-f71e-42a5-858c-fce8fbfdbcf5	3624569	828ee0dc-de33-4ed0-8a1d-21e715273089	3853020	48da9443-074d-40c3-b4e4-7997a6034bcb	3727576	869a9c3c-2003-4796-b37f-00166f39f71f	3690423	c0bf53a5-196d-42e1-9a74-5b804d773374	3603247	a8fd2541-92e9-41b6-9473-1a161befa0aa	3637345	6cc81270-1ad3-414d-9897-463efc677353	4954899	a794f9a1-e9ef-43eb-bd25-87fb7e5e7878	3848035	51085f0e-2e81-4c0d-b8b4-9e96cec9f77d	3964940	9f256be6-59f8-40e4-a9c3-74cd5c722815	4120674	056a2f89-9293-46ae-8c03-916014c02fa8	3808741	54683847-2c29-403d-8e12-cd71dad5d334	3856154	5ee7dbfd-bde9-4e9f-841d-8f6d259d5851	3737876	7a34fe15-226e-4ecd-82ec-5854e48a9715	4001941	df75efae-bba7-4c9c-a37f-0941b32272e7	3449756	4ec16183-4ed0-44a9-8224-e8659bb47bff	3840483	17fe3d99-ccac-47bd-8f0f-b6dc4339a649	3725895	33209d5d-e5cb-4199-840b-8b04ce1f04ef	5458064	a158af42-da56-4e06-889a-59636ad5b782	3566553	a144a2fb-2c9c-4e6d-b7cd-806e657e9d8f	3907380	b74c0f31-61e1-474b-a07c-5a733ac932ef	3604395	d88882fc-de29-4bd9-b00f-326d474647f6	3999058	36e00cb4-c6a5-49c5-ae89-8e7084d5d576	3736077	b83fde54-c28d-4fda-979e-d74c4638bc15	3601727	5eb79d18-60dc-4abb-b6d6-c8865152e494	3607881	06f20801-2a38-4d88-9e78-d67f0f5fe503	4741961	61d9c017-89b9-413d-b06c-04a0f7982f32	3502693	90df11f4-eb3e-428b-9dc6-ee3afb0c4fb2	3562394	0326cd88-10eb-4978-9c5b-99ef2d159b4d	3729917	bc6de9fa-6b9a-4cba-b1bb-c0bb0b6c6b28	5010519	92603273-a0d6-4621-98a2-95411a41c836	4032213	c624179c-4dae-403a-8942-24a2acc53a8b	3935148	8e4d7e38-4987-4689-9513-5aff5d49c895	3760624	4e8fd56d-22e5-4dcc-9dd6-d072a5a83697	4029436	be674c1b-8ed4-4c45-aafd-f5f8c22ebcc2	4028899	958724f4-99f5-4bd6-a11b-6142c437d848	3499990	f2000c6a-9675-49dc-a333-8008afd97a99	3858045	420503e9-b99f-4621-a5a0-2becc466ca1a	3709052	267623ff-8c2a-4a4e-9ce9-44ccd0928b6c	3759803	33c1ceb0-f8eb-429a-822f-cbc11272037d	3709882	c45677fd-4864-461e-9c82-033821f3d170	4711005	829826ef-afaf-4d55-9be3-bbd410c86898	3562223	ce9aa668-014e-49f5-b50d-eff7695eabc6	3995567	59a7f1aa-7efc-4765-9a43-7404a6194d20	3598418	c905d98e-3b3b-4f3f-aa55-b2f6035a9ce9	3469494	f7f83d55-3cae-4e51-a5a9-22b4df220305	3600334	270a12e1-4240-4bd1-a531-6fc29de893a7	3630468	996379e3-366a-4eb3-b286-3cd30ba1e228	3541307	cb62e825-59bc-43bf-be36-ffaa244368ed	3761572	a0a85093-0170-4ef9-861e-83b4ba9eb8ef	3895296	dba871bb-44e3-42a3-b0e6-a575a1a8a298	3292257	256d45af-cb8a-4768-8626-4afa2b19528f	3612647	2b7999b2-ea4d-4897-ade6-714a8bd8d934	3771719	233b8fea-4dfc-4c62-87ec-124fd260c362	3830614	def43f6d-eee5-4a63-8cec-4b7dbdff2d03	3748628	5a130355-9bc6-4dad-b07b-7fa01414ed06	3865250	4c1491e1-1888-48c0-a68c-ccefcdfcd2fa	4049790	4c9666e1-70ac-4c3c-a24d-52ea05e305c1	3657977	a12e7c46-ab69-40ca-bcc1-b323f04115a4	3681874	3e10bef9-d85d-48a1-9547-94872dde2bb7	3699784	97b29a62-8cbb-4e97-8027-686b83d57836	3560654	9e0a50ff-965f-4db5-9a0b-02eed447ff2c	3693185	2679bbad-1d0f-4016-8a8d-71273c7f802e	5587609	1058d490-368c-41e3-9e41-bd542e86c4fc	3864116	f1ffc417-9367-4e1c-b409-47a13be45be0	3667245	da66d2b5-a897-45a7-bc8d-96a334f4c697	3716989	e3476dbf-9bff-4e57-91ed-9a9e9c25f0ea	3662856	5db0e399-e632-4d48-8d4e-f1a56beb8153	3551894	64104a71-5933-4130-8168-f751b883f8db	3549719	60d754ff-9ceb-4e77-87cd-6361565943a9	3256565	daacd2e5-b13c-42c3-aadf-9e91034a6e6d	3683243	9606de25-1416-4029-918b-0095185d959e	3751664	943b54df-aa17-4bb6-ac61-3ca732bbdeba	3730280	4b8f3005-fb57-4f51-8083-a5b29ea329b6	4195920	7363076b-81ee-414a-b119-f66aeb1ec853	3865070	30ceceec-d6ec-416a-9f84-1432d558c579	3741062	78e12ea4-3ac6-4639-b1d7-8e4dcb794361	3611322	ead880cf-00e6-4368-9087-de473f36fe70	3604576	5e87310c-bbda-4729-b504-a7275beb939f	3830258	75d24284-ba24-4c4e-99b9-fe0be5542779	2887359	f145af97-12ef-46f2-844c-15b4c5c98ee8	3721476	228a6184-0eaf-4c60-a9bc-fac7b09c8aab	3159051	fff29cd7-d9e3-4eff-8021-497af6811110	3790958	f396fc4b-79dc-4953-a739-770c0d2e9638	3732059	2024fb25-870d-44b7-95e1-76ec0c99a7f6	3823370	9e9aa652-a78c-4d61-8eed-9a256a44e071	3610794	438de366-8a62-44e7-aa5c-fa2e0020dbfd	5338422	fafcd491-fda8-4142-9612-cf9c7d6743da	3531775	c90022c5-8d97-4543-8c74-f94222b27391	4090477	927afdfb-915c-476b-a960-b5d71c7a1f5b	3461815	74a70f49-e5c1-461f-bf67-46568a91fda0	3612925	f93e222c-201a-4470-a1ff-6e2acc324770	3611674	35d0012c-eb7e-480e-894b-51146dbcfac1	3710674	fa256b03-0774-4b3f-bf31-f4e1d691ce31	3565170	8979a102-456b-4db1-8c01-cdb326d870b8	3705166	72011ecd-8b2e-480e-b6fd-fe23163676bb	3294862	936409d7-5047-4f35-a866-c657a5f58c3d	3458432	9f5288da-2e96-4d19-9773-02880611f47a	5197335	325f8e9d-dda7-4d40-9a7b-c25eab00a7b9	3663946	7f4fb69c-e744-48e1-b8e2-b091ecc2073d	3818043	4174932c-7e8a-4527-ab7d-a6b8bb0eb763	3951253	d5bb6eca-9535-41f3-8509-0db62a06360e	3788480	5bc365ad-a7f8-4b40-ab5f-6acc05b8ce9e	3438113	160c6b07-b0c5-4639-904b-97ff9651c63e	3738521	a1b0fbc3-603f-4b33-80a2-51f4d4693ccf	3733461	d9e04581-3328-448c-bef9-543895792767	4161112	c10fb488-ac1d-4af8-a4a2-846451a746dd	4292779	c7a35a95-2853-4e90-91b7-be880904cd8f	3705928	ad8532ff-9b26-4a21-ba72-2afccfc7471d	3645977	31f5c1c4-b05c-484f-a4cc-1a18b210fa42	5953969	acb2ad36-c9f6-411b-a2e3-bb65b8205686	3181417	b7ad3d2f-03f2-42c3-a042-bb9081edf733	3704163	079dc968-a521-4e7d-a9dc-a1717fb5ac94	3736351	9863e67c-7734-42af-bb7c-165d0d38c62c	3702262	31e36d6d-aecb-4034-951a-e7b9d592a03b	3738579	af1eac2e-f1c7-4073-a87b-0490c97ba0df	3383123	e3d0e21e-cd89-48a3-bb87-1966e565d7b3	4041700	51248e45-ccdc-4688-bbcd-6f12729b42c4	3685174	be1e5ba3-27ed-4d89-b80a-ecd2cd7a0cb3	3556450	9f94b7e0-0349-4349-a716-f2fb2d89e063	3176779	84aea42b-b0d6-4066-be64-9028c1f947cb	3516862	a5150f41-e25b-407b-b30b-be7276d1fdc6	4710312	d9f02a32-a54d-4e2c-8a63-c282e6a96f3c	3683742	939ff78f-d113-48e3-956e-e9861d95ae41	3746126	9ba0ca2c-ae85-472d-afd6-ee78b4b2824a	3692051	723be96a-b92a-4a89-829d-b89cbb941e1a	6455843	49403ad5-8986-4066-8729-7e7d3734042d	4044208	64e2fd8b-f29c-4ea7-8f8d-dc7a194ba61f	4721529	aeed5a8a-cde4-4540-84d9-22973b542ad1	4407494	bac15e46-cf3f-462d-9feb-076a4b04edc7	3858891	e9371c2e-41c9-42ee-9804-67a6957d692a	4167197	8024b006-d08a-4468-978f-f2d22ca6656c	4365585	a21a0bf6-07ab-43e5-9b9d-8c7214a253a4	3647058	4f26e6f5-7166-49a1-af3d-1de42868d2f2	3640014	4d4aebcc-9519-46af-bf73-c5c76e631829	3715420	f58e32b8-591c-401a-adb7-0ee6fcd43511	3572321	89398c78-6cec-4630-b6a8-21c116deb719	3564402	1787d1bc-7774-4f34-9e6e-fdd0171b52fd	3411654	f52ff969-d296-46ea-a166-e914b0d2d7ae	7708950	0fa610e2-ec81-49b9-971a-80019bc31243	3740755	ccce1ab1-84b6-419e-8e1f-7172206d63c6	3645176	a8aad129-7618-4ab4-a8ad-f5f3843cf48d	3993720	5ebaf0af-3d7a-4996-abd4-08188707b8a9	4081111	cc58ad13-ae18-43b8-975f-1b2ead5b4fba	3917419	88d6a731-a8dc-45d3-bfd2-cf8654ad01c2	3213737	678ef4ea-e51b-46e2-9dfb-a13bb62182c4	3726120	13c60cd8-4e68-4285-b186-ee87264e0f44	3641989	e6fce10d-62de-4ea5-84c5-9e5b0a98709f	3436206	00146b64-156a-4dd3-ad99-2f3082c279f9	6216252	7f3a0d91-b047-40e7-93f5-362ed56a6136	3869972	a67fb2db-96b5-4ff5-a2a3-d92f9be1ad55	3770532	acee5ad1-c419-44fc-9c13-7a6923030022	3882001	2311f3c0-051b-46f7-bc16-9c02c97d2814	3615531	7217e085-ab60-4ebc-abdd-3c3756b0155f	3627922	6170e91d-3b26-4f16-b726-f5da0c3462ce	3515273	a3c21f16-d352-4de5-b0df-0b3a2ba47460	3984829	eebc59c8-6369-4dd0-843a-395e8341532e	3541800	9f20eb37-4dbc-490e-a48e-7f13c609907e	3561606	56f31b6f-ee4f-45af-a6dc-e1db4f2c2a87	3427545	8ca21310-ea84-49d7-aa95-9820d9d383e2	3675765	789c2c3a-3fb4-4b47-befa-d401bb248f31	3797234	2b690fc6-0aff-4096-9064-1661983890db	4086683	871637b9-5b0b-44c1-9e81-5b6947459a5b	3682671	233b65e3-051a-4770-88fc-28ba76cd8fd3	3714169	2f0b3710-7626-42c2-8996-c27c8b6329ca	3574486	90854e7a-ff39-4f2e-974d-f08b7e3c1d16	3673432	d5354e45-f0c5-49e5-89ff-9a54e84827a1	4300409	f8a5b862-5306-4342-b0cc-f22318aeec76	3536199	1c2f3fee-c930-4c68-b5b4-b4c7d7b5089a	3541287	9d4f4ed2-11ea-4dc6-91f4-ef15be0febcc	3763210	04e5d256-52ab-4c30-9e32-957f200d4675	3669220	1af9087a-274c-48d8-94bb-0ab8eb47fcfc	3946898	fd346f0f-6eb4-4126-9b96-c9b1bce6c19f	3788036	882aa06f-fa35-4a46-869b-f6f8d4187f83	3734005	0843ef2c-ce88-45c4-a0e1-49ad5e64d0f1	3825243	3378b4d9-567a-49d2-9304-c461a8ae3c7b	3740295	4efc8954-d2f7-4a7b-a66c-57eb56bdacee	3508069	1f03737b-8b93-4b50-af3b-a40d477d921f	3812055	0ece368b-ad04-4cba-b906-b596bc90f7c2	3436480	2c26e2c0-082b-41e1-86d4-64a8786d23ed	3822901	d34fce6e-ff50-4409-b78f-f88af6d79910	3532025	3fcc0d05-6fb4-4c98-a6d3-c9315061f960	3744284	bdea243c-cabb-42ec-a6bd-292540498a14	3796584	8588a7fc-7b7c-4fc0-b64d-35ae853cbc08	3953282	8bb86e12-af23-442a-b8e3-453f6def187e	3692002	a87f6757-fa82-4b91-96ef-2ef87462eceb	4002646	250e2b6e-933f-4992-91f8-a42a3d3b9651	3737705	626c07a5-4b2f-4904-87b5-c9ab9434c84d	3491749	c71745a7-f61e-4cef-b32b-1a861552a48f	3665367	b325abb9-20b1-4d7e-a754-3f51c01917c6	3609797	5b676055-90c2-4250-8005-583f9d77c111	3006938	33b1dbcb-d07a-45d4-adcc-5019febb7548	8957480	c5617d58-5595-4bbd-85e8-4a2b05dcc729	3926110	90fa4745-12a1-4cc1-970a-6f6cde1ceb16	4043406	29f54942-b6af-4ba5-ad9a-3bf54f2b13a3	3686640	28f3d7a1-9b7f-4cbb-9958-9f2af0840999	6201994	6fb6bf05-ee54-4063-8762-b1cb02c1b8fc	3548052	d6cfbc45-3eee-483b-8bc3-1aaaa0e6075b	3597117	c8e6d436-ad41-4006-940b-0f5e44e85ab4	3594454	10af5440-d655-43c0-b94e-c014af2d6712	3623722	8ff585b5-2816-40b2-b25c-19a5de934b25	3695091	3ca23b58-ca05-46d4-96a0-43b2678e62e4	4098581	16494d15-ac26-4816-88fc-faa63d4ab485	3830585	a2e9cec7-dbfa-45c0-8a60-91c119f8a49f	3546068	7a727a6a-4730-480f-8b19-b2e44210f9c8	3062294	a22ee7fb-5cda-49e9-8818-f9e598838751	3637869	deb24641-8c93-4e3a-b80d-35529469f547	3993382	89e5dd18-41c4-4c35-9240-cb89c33784b8	3641950	e55b14c3-8325-4eb9-9fa3-678d2d58604c	3658554	5730ea7f-1a69-4c30-afc5-a66e354d85b6	3807886	d9e7bc64-f92f-4ea7-b403-ea5ea14caa04	3390044	fe35b89d-608f-48aa-9780-362c02dd0dc1	3621181	d0079ee2-50b0-46d5-9f67-91190a3b2aa0	3614025	356ddb23-2939-4f19-b851-1e468bf80bd2	3993750	64dcea16-9a36-43b7-8c59-c447e7459378	3872836	6ad5763c-1ca0-44d4-a764-0bdba83d9b75	3759255	9feb6ab7-537c-4272-8d96-02770c368573	3564853	38fb594d-b8d2-40a0-b0b3-4bc0ba8c9fc4	3207426	7cf998ac-5abd-47be-b2e1-b71f28df915f	5884683	884b66ed-b752-4e03-801d-b1027bd5b559	11988188	81b30668-f3ad-4577-9883-6660c8014009	3520778	6f3d5fe6-51b9-4eea-9ca0-cc6df5f682b5	3784727	b7d9e8a7-25b3-44ec-9345-99db6ad36b6d	3720723	a45f46b1-3554-4bd4-8ef3-8ccdbb26ec00	3601620	8b513860-7794-4e2c-9110-3a6166c1db41	3940769	1a66785b-b92f-457b-9407-1e54052aa579	3938457	22577b08-9227-441d-b397-6f1a5a6534f8	3746649	cbb08eb2-13ab-45a4-92aa-9adc2d00a8cd	4489948	78bdf419-a3cb-4998-a599-64c59e1f635e	3677685	17e22034-f459-4d73-8e31-154fd1b1280d	3576485	dfcdd5f8-5bbc-4604-8f43-80e9d0c9a5fa	5702382	b0578dbe-a36a-414d-b9e4-64c5459d8910	12157424	2dd430a3-d2f7-4995-98b1-03d521f62f12	5159533	2f9127b0-0cbf-44c8-a371-0f74da52ae5b	4221903	43d7853a-79f9-4212-b31d-f29c623a25a0	3533091	57516b26-a309-4a6e-baef-39a435db76ea	3608350	798f2b8a-0f3c-4ca2-9566-d78df58edebf	3718568	a83b752e-3f6e-4acc-acce-b14ccbb0d378	3417251	0541b9fa-debf-4ca0-a329-6c77048a3f0b	3791971	c1bca5eb-cec2-406c-a94b-8579d7ddf022	3025195	d1d0537f-9cc6-4f90-b799-b13a30eff980	3483199	2bc97370-e0e6-4f36-9f2d-2ce2296f02b2	5092153	17d735fd-c718-4e61-98ec-4cf6eca76626	3833919	fe573c4c-35b9-407e-ab8c-65d0deed2b32	3513143	e6058862-cb9c-4155-8134-5eebf4739931	3871873	7878bf02-cd10-4cac-a593-714c1104eff6	3233939	3c495026-d711-4266-8662-da8b02ba3924	3897589	fd5c2766-52f5-4559-915f-317438d830ca	3269054	c914a02d-b0bd-4d2f-873e-efc2a58f2598	3619133	bae93d6b-b949-4965-9373-9a69232f54bc	4237599	16f0f73b-8cf4-4dd6-a68a-69b4bc20122f	3923725	0cc6d500-25c1-4f5f-a180-a22e445d725e	3804557	6bd54cc5-4764-4b50-978d-97b7f472deae	3719394	0f9a7587-888e-4323-95b8-2d043ca1045d	5234954	a513f532-2ea7-4559-afb7-c6ceacc90d6b	3791677	ce7fea66-310b-4a03-b359-e5375fd9c3a6	3613321	dff1b79e-e3b4-4801-adb1-8096e3f64e59	3734444	21bcc3b5-e780-4402-bdda-929afb9b704e	3524556	7cb2cd1c-e448-445e-b97a-eddbbd558a68	3459097	745cc4a5-a55a-4315-8613-ab3e5b23fdbd	3694676	cf49174f-ea80-45f0-8479-fb4f12f23760	3568933	50951f40-61ac-451d-82c5-a3a103895e34	3731971	45a21d01-e1fe-4f03-b7b4-f8339edefa4d	3785635	078dd05c-fcf1-4be9-836b-cac790dc088b	5129584	965185ce-9344-4462-b85f-fe90af27c4e5	4151551	4899dbc2-0e7c-4a69-99d7-b06915c0990e	3928320	9b66b106-5022-4395-80bd-bb6a1ab03482	3629842	273b43d6-9c8f-444a-ade4-47a049f46fa6	3526164	be4bb96e-6e6e-4be9-b53a-1fb6b69c9989	3830692	7ce41c84-fa73-4579-b652-9f3b83a10c89	3234081	eba8b7fe-2fde-4f6f-bf2b-2372e23d3cdc	3198687	83b855ea-066a-4790-819c-54ca3efc5a75	3570044	c9bdb6a2-8773-4b4d-b1f5-7173dcc130d6	4088595	ed0de6d3-bf6f-4df1-9928-682aab543b9d	4263837	5a041697-91e6-4154-a212-1cea640df2bf	3934136	b26a8c06-c11d-4b3b-a5ae-e60ec0e24d69	4034544	f80991d9-3f33-430e-a816-af8cc88eb8bb	3730421	2568f2a6-f550-4f62-9fff-6ce79856797d	3841417	eb7b44bb-df39-44fe-8c0b-ff3754c19a81	3674831	dd91ea2f-1a88-4f4c-8d02-ac4e9cee6bcc	3621494	712a1e16-a423-4328-baea-7f46d1f553e7	3623571	fde30a84-3a51-43b3-8f92-9a6c3d95a7d5	3590528	7b97d2e1-381a-41d7-819f-909edc77bae9	3807700	59de6684-195c-450b-b3bf-4a5260db8c72	3739820	85688de4-1a18-4f68-a985-65bb998cba88	3617559	cd5f1ec6-47eb-4da9-9351-3d08bb4a3a1b	3590055	a13c340b-b06c-4156-87fc-82b9b4425796	6008827	f58ff49d-8644-4eea-8c55-816cc09626ae	3680036	ed6157e1-491e-4880-9948-81fb6614ffce	3749303	147d05fb-1dc6-4966-89ed-3f8d2077496d	3826196	2876c638-1ae5-4e01-8832-82f6dddfb2a6	3463730	d75759dd-ee6d-4692-82a1-67fa32f2e73f	3589473	0b39edee-4c69-4a05-bd81-c4c2382dc0c4	3613614	9543a4ba-9792-4dc9-8168-2911a513a839	3578886	95b1622a-cb62-46b1-8a34-ad7b53a73c96	3521408	13deb64c-8f3a-4302-bf66-bef5e8fd4563	3839471	f398d5af-607a-4b04-a9a6-b461ec51f93e	3783969	ed79f3e3-1447-4e93-82e0-bd7e719c761f	4085935	90bce0df-bcef-4b4d-ba4d-5682abf025b9	3454131	ff31f037-c87d-4be2-b392-924837f0caca	3744592	29d5d679-efff-48fe-910a-d5765aa3c1cc	3555213	4e863309-1e61-4f27-b9d4-280a7c53e234	3600735	500bce83-6216-4b20-b383-70a0806ecee8	3590836	69b172e2-e5ea-45a9-aada-feef6de84f5c	3911392	439a8169-7496-49ce-9faa-07a4b48576ed	3578255	e96b503a-a59c-4882-a433-c58ad380b906	4199522	f4079045-9ad5-47c1-97ee-5ce108745dd5	3636148	22659473-54ee-4190-b012-d640650231aa	3527572	abae1bb9-cf38-471d-b136-6f50bc1f05b8	3675554	d2013258-1d78-45cd-a291-644d78dc140b	4973478	3fa3b9d4-453f-4ec2-be3e-0949fa0eb16b	3779780	ef4beed2-54ab-45f4-9639-39116b7ba52c	3740515	363570f1-66e4-453f-8cad-4b87180156ea	3657884	0f2dfe2c-ddd0-46bc-835e-9d363b4f3180	3505942	b168a426-fa1d-4042-aad6-9d7a43f93ce6	3395084	24f9f9a7-64d5-4a3a-91ab-7a301783cbe7	3458056	736f0aed-ffcb-4470-b432-09c956d416e2	3398178	52870ff8-61c3-4a89-aefe-58d909321ad4	3592288	a5c5766b-db53-4f6e-8f35-19596bbdaacb	3749309	bf2a846e-afdb-47bc-b346-ec82a0b66ec5	3642399	9fe940bb-f452-4429-8e71-1dfa243534ee	3544493	f94e138a-7a83-4a06-b2ef-a40663c92c70	4162036	b0ed56fb-98dd-456d-a01c-d7c992c13f2f	3129880	724f4198-076f-4840-883f-4811b1d37629	3693351	b8ba349a-e8dc-4a43-b338-3ba9c0d4828b	3459204	a4509bc4-b4dc-4256-9cba-5cc5a4a92035	3798095	9290cb02-3eca-4e50-9d98-c5dbcb74fb1f	3923456	d2178076-0a4a-4496-b4e4-844d60e6fd00	4205260	e9eef53d-79a2-4023-88f8-a838ad5ed92d	4829084	694b911a-42eb-4c5e-a3e9-c029780667d0	3903312	89c84151-2d7a-48b0-955d-081f77fac8c7	3725577	e30bfa40-de86-4197-915a-6a6d376b4503	3617251	f6ac99cf-b393-40c3-9c0b-b0b917fd6715	2934655	5ceb0ddf-f55c-4b96-8830-e97926146464	3622041	2de1c906-5d39-43f8-931a-92d6bb45827e	3959680	cafc0d2e-df45-41c6-94c5-720bbc2441c3	3896474	4952e1d1-30f7-4ed2-a89a-cc8ae68416f8	3605691	d40f4523-0b12-4184-96f0-7494ae211e5f	3801497	c3bf909a-5cf8-4749-b95f-47ce45a782b3	7802466	a2b6a0e0-8922-4b5d-bea9-00a227993598	3354988	deb77fba-093f-4f3b-ac6f-ab498296a4d9	3505976	11cb052d-b1e1-44d2-8ecc-14c02e5b23b7	3943178	f553372d-e5f5-459a-a325-29aff424bf09	3719932	f51d95c4-77a1-4787-9d1b-68c7f3b26a98	3662929	631874cc-ef06-4618-a20b-90f0a222be98	3753390	624dad55-9dd2-4c1b-a297-e14817fa2fdf	3708225	a07c02af-7e50-4cec-9dee-86712476e9c2	3684382	e25f7244-b802-4bb7-a108-bcd848b4123b	3368909	bb9bda6e-c375-4a1b-94be-481bc9bd449a	3767648	13706f1d-4cc7-4476-9655-065679b48de4	3267666	32c3bafc-0477-4e91-bc40-fd48b900719f	5353251	8333b11d-73eb-4b09-9f67-d8e8f8e546ab	3953796	e911dac9-2da6-4c9c-8d56-513e66193d80	4082197	3a61a87e-2fa3-4a70-9a40-5be0a54702e7	3767761	afd7e4c6-559e-4260-b2c4-256959b61e88	3842287	70b1f14c-2ed3-47bc-a1f9-6275366956a7	3621386	6269b742-e9b1-4864-9ba0-aa7be628f827	3702883	03a62d14-fba7-4d85-9a42-adcad08f3be4	3752281	3836e5d3-269f-4d9f-84f7-3b2a53ade238	3576041	8042b238-2921-485c-bb48-6e18f44ec43b	3551181	d9f93543-9d1d-4d7b-b62e-91ab7b0d998d	3841285	054f32e6-abda-4914-b6b2-68f15f3a2035	4312726	3dcb9ecd-bd47-4993-8595-813e8c4464f3	3098044	d8a30592-9e6a-4ab3-a8d4-f197ae2a86b6	4031283	cd3fde62-7504-4c05-834c-4113337b1a09	3586154	e501027b-7192-4259-8e4e-5f1570b62f14	3707561	366982ef-7d01-4d87-8ad4-89aaab2a7fe0	3574814	716572b1-10ba-4bb7-9995-6a8a73a231eb	3700512	b89cda5e-ae83-4842-ae35-b98dd7db3a7a	4127244	6e171025-9457-484b-9c50-a9323e95cca5	4198285	c85dce29-df6e-4885-b4a3-2face693e29b	3306227	e8bdc309-f392-4d15-af75-e6cdee93a3c1	4405221	3026bd80-ef3b-4bbe-b56b-164788a383fc	3393765	62bed0bf-6dea-4d3e-8c88-cf0048a2c9df	5784319	8e139290-6186-4cea-800f-c719a1bf1fe3	3679152	c93637a7-034c-4714-b640-97697e44fb88	3958267	e5e286ac-fad9-4c24-8a77-cd54cd00410d	3548796	1d7cec03-06f2-49b0-8188-4d804e019dcb	3800685	d59ddb5d-61f1-4212-ae6d-e853ddc67bf6	3638719	3ae9f432-aaa3-4b1d-8820-fec10f16d59d	3612915	2f093f2f-cbed-43e5-b49e-982a3522e441	3412500	751b4952-7f33-4b6a-b996-161c72651045	3588099	de042ed2-68ea-49c6-b819-ff9b49043eba	3626303	0dc25d6a-73d9-4acb-ab6a-44da93e75897	5103477	cca105ed-ccc9-4633-8572-248e94417217	3945486	0731db91-e2ba-4dba-96c2-8bdc5c3120c5	3928579	3600bbf1-6c5b-4a04-a604-1866c808299f	3492735	6f62f3a0-a979-4483-84d5-f7af48d885b6	3791637	fb9a350a-ba7d-434a-aa22-8f2c3453e15a	3569569	0708e36e-7a21-4d73-a87e-fe79acddb1f3	4154772	96d9a79a-1acb-45c1-88de-2f6d131c0b03	3589307	b37c3ff6-352d-4d3f-8946-f1f0c4bd4e74	3810471	f7ea60b8-e7a4-488a-a6c3-b13443d3656b	4005055	9dd014f3-2198-4c8d-ae80-c0a52556474a	3631851	70aef1b1-7595-462c-a880-a0bb069af8e9	3796350	4f23aaa7-95ff-4470-825e-a152730194f1	5547855	71869893-adc8-4d4f-975b-e57a5207ec59	3567687	2a2ef3ec-b2f0-4be7-90b8-462673cd3d0d	3950736	4f60dbfb-06b6-41bc-96b6-4a2f2354c694	3606806	311af3e6-47ae-4a05-a34b-59cd1ad1d468	4234509	e0e3aa32-b972-4df5-a97d-0e764f6c6692	3455836	b92d894f-4a5c-466c-9f52-109f5ebfee57	3888468	471e1cd9-c56f-4f61-a6f2-0b39011797fd	3603897	533fa756-5788-4307-bafb-23de970fb3b7	4305409	7a46d472-11ee-47d3-95ee-384c48e8706f	3555013	6714045f-4f49-4f6c-8032-b6275ea38de5	3464796	e8460b90-0a33-40f6-80cb-0152517a3d09	4181231	32a48522-3dbd-48f4-b7f4-9eba370a0c13	3865191	b298fcfd-2b29-44f8-bf78-2085cce7a5f0	4068823	3bfdff17-834d-4c21-b504-5601740d3004	3537500	8d0f80cf-eacd-4821-afde-c98bc32efa84	3598565	5dd204d1-ef0e-41ca-826f-b2bf5c5c0581	3636778	1fcef6c4-d4a7-4ed5-a8b6-f5f2f8893b45	3521256	eb347094-10eb-4018-bdf7-b6e0195b4d97	3455778	cfeb0b12-84e0-43e4-ab5c-b9e2cf44cef2	3502560	3911de94-b55c-43b2-9425-02b4a157135c	4016508	c85a2cfc-0cf3-439c-92ad-853d534c00e1	3683267	5aa1184e-6ebc-47f2-a09b-d5dfd57e60f3	3674396	3712f4e0-7b87-45ef-abd3-947c7d6a4db4	3369648	3ae5f785-2a20-402d-8ee7-8eb77907498e	4436743	51a716f8-e04d-4d4e-bcad-d28c66c19f82	4539170	82026042-a4ec-4e0c-adbd-287c29e06def	3728437	c06ccc17-bd81-49f0-b56c-429f12261cf2	3867738	8efad7d2-1835-4447-aac3-985e6cf84e6e	3664009	d17bd1dd-52f3-4320-a2e7-330db2001ad6	4047761	21f39f89-73ea-4a3f-8acf-e97e4bf142d1	3871737	b44dc89c-62bc-4046-bf30-8afe9eb0d351	3610921	836af606-7e86-49ba-810d-9ebf9d67e3b6	4099368	5a8d1b84-e9c9-4e0c-a4fb-c3859b6c5c24	3140442	8c7e5d83-6cc9-4cc0-ba15-0811bce2396f	3483845	958e38fe-03b1-4058-9ccc-1212e99f574f	3626787	cf769b6f-39f2-4e40-972b-73e2a1b9e96d	5000020	9c780e2e-4000-47a6-97ad-37421cfc5a80	3759847	16f39107-d407-4f9e-9d10-eea45f15f99e	3462293	65b5c99c-2209-4b88-b9bc-e3011af2a05a	3824563	ab1a461d-f07b-4297-a314-d68af5c79339	3726350	40ea06e4-5872-44eb-9bc7-75d74ff97026	3932288	5fdb4176-a8ff-4b91-a631-3d6a6246f09f	3799913	ac8e0f4e-a729-4cd1-97a1-104117fab8c5	3457122	1c028b00-c326-47a8-887f-5d08c6da074b	2897907	9f9f11a9-7bd7-4898-8b4a-e1d9f31a60d9	3882935	c772ba49-370e-4e8e-b179-c3bf6665dead	3843597	2c883006-b11d-43be-83a7-62039aeee89d	5110057	27f12d57-7d8d-4f2e-9fcf-7448c2246b74	3814709	f17a2ef3-fd90-4792-80be-32a487776d8b	3954059	ae5a3cdd-34a5-40a7-af22-dbb4476cc8e7	3527669	c38fea96-75ce-49c2-abbf-b166e3c9940d	3811982	d8ce72ca-0764-4d98-a484-3274d5dee776	3541228	8565b3de-fd91-47a3-9919-903db1812053	4012837	ad88c2b1-fc76-43b7-b326-e57476189e5f	3816327	2f9abb26-553e-4a37-a2e1-14e96d647e4b	3427511	ae7811d6-220b-4232-b7bf-ebff8891cd8b	3495433	d7d3fb3a-8bec-4ab0-aa81-f2406e266712	3794659	ba88fd19-d955-4382-be27-b8a0a516eae8	3564095	fae9fa2b-d620-4453-b889-c191f9b1e251	3223234	a7a5487a-835c-4f76-86b2-9e1e8bba8482	3702228	59b55871-1173-4b4d-a746-fcece8dbd305	3711799	39fb0e8b-72bb-434d-94a5-1b7b535c929f	3677666	23df4152-4a7c-4b52-a4a1-cd7fec965e49	3562247	dd16f64c-e56c-4214-b267-067fd2866e8e	3975586	2409c634-871d-47f9-bc12-8ed7fe3d43e8	3868979	0a10b360-a753-4f5c-955d-1d78239b8c1c	3717116	23c17cd3-df4b-4f03-b684-475c70d000e0	3527797	785a6d46-f50a-4d45-a58f-a0949bf1b9e5	3536253	3e828a3c-25f6-46d6-8424-64b920e89c61	3577429	8b6d7e5f-b472-4660-b11e-b43a962aa886	11929671	c44304a1-36a4-4061-abb4-c89a3cb47d70	3616782	321e7327-0d49-4788-abce-c4687a105215	3789712	7d0e30cc-b8b2-44fe-8136-dca8a3622ac3	3956190	a11d6abe-a1a5-4624-9af8-1f6abb9f2864	4036059	7dc7e70d-c5c0-4eb6-af04-c632cf3b23d5	3764866	30f88187-c221-4e13-9e4e-296908c2a48b	3807641	dfb7e79e-39d8-43a8-906b-767d4b83dd06	3674983	98830a9f-1787-478d-90a2-7ca1b3a2610c	3460001	b63f6c41-3fb8-48e3-80f8-636456412926	3726608	ce056f31-54cf-4247-8660-ca4ec2ae66d1	3692950	40f6a755-104f-430d-b43c-c99dc29bea28	3491299	177ff946-7d03-42b9-a116-f238c98146de	3779418	f9cc46e7-9b61-4a24-ae9d-9e350efbe0f3	3455710	fd7e7523-add7-450f-9c2f-02b3f5310fe9	3737084	04703f15-e4ce-4f01-bff8-27eeb6e08349	3715909	b3cebd18-305e-46f0-80a4-7111a13f8973	3522533	d0b97c62-501a-4656-ba58-7048131a0112	3564642	a163aaeb-2cbb-476c-a32b-69f960c23f05	3749411	f325644d-3877-4e37-91c9-c4e1d8f1fa25	3693557	704860a6-c7b9-4f73-b88b-78cacbaba2f5	3922865	ff155c83-4635-4505-8229-7dfefb18ccb7	4345017	efaaf40d-c76a-4240-83b1-1333333a0576	3569105	93029100-e63f-4f1f-a8f0-4b41e50ee128	4881262	49f49401-23ec-4506-95b6-049796381f34	3852860	611a43b9-eb18-4c91-84c6-70c2c1544d73	3582566	40573693-3661-48a1-857e-e131d0462d23	3638235	2568c947-e26a-48cc-a8c6-dbddddfea21b	3966377	d6d04d0f-6e64-471a-9e94-ed58df57a652	3873565	2304bc7d-1bb5-4d8e-b510-8b9f8bef71c7	3619714	0c5772ac-2386-408a-bd8b-08a24edda361	3935876	c5268b08-d26e-496b-b07c-24fb1e411b00	3520235	63f1a5c6-b9df-4f9d-bee5-12b1c0731c70	3556186	a6e8e2c0-4ee6-4fa1-8747-249026a27cb5	4005520	a045cb9f-fb1a-410d-8f4f-c5b838235e50	4804126	59f2940e-7d42-4b39-a4ca-2333a03b85ed	3891445	51133f20-f3e1-4583-8af4-e4ae4536ebe3	3900977	70c38450-b11f-4ee2-b7b6-a099d0790d56	3176109	daf53de4-3318-4583-a401-637866b814bd	3043548	e81f7182-59e9-4eb6-bec3-ef45c13f2566	3846832	5c4bb7ba-2880-4ae9-9c64-d3b7b4f78a21	3502204	e59dbccf-434f-4993-9b1e-296e11882fd2	3722116	6c9981c8-c906-4eee-9306-efbde2bc9242	3628659	dde977fa-2c4b-4e84-949f-6c14f91be243	3691807	9c85660c-8bf5-407e-9211-a6afe76ae8ee	3671746	de96cc9d-4a34-4b0d-b60a-1d25f79f7ff7	6683211	9d67bb4d-4db9-47ea-8fa8-72d30d65b030	3563371	b95cfa6a-c333-4966-a6a8-d2298400418d	3054962	2906cf38-ac8e-42b6-ab4b-8e80eb2e3078	3617265	a5b49648-e7a2-47e0-a460-3ac1b16b4c00	3390006	d55030f4-a72a-4d4b-8744-f561907fcd2c	4064057	3c5502e3-b2f5-471c-b9f4-db37bbe68b64	3747759	da52454a-4bb6-46d1-af3e-add738b5d3fc	3519624	a9443053-633e-4113-9980-e5f9dbdc4910	3678770	df588913-78b5-4740-a6d4-a784d3c28eba	4340216	7363b300-0c2c-492b-ac9b-b4d6afb0281b	3097482	4757b236-866b-41dc-9ed0-1d58d9e75a62	3646915	0e5fa132-d8c3-42e2-8331-929a9c0f96bb	3502610	2074b0b0-2a28-467e-b0c2-349508d3cd5f	5240520	d7dd4d56-6d36-45c6-ab2a-70327cbaeb99	4812616	44eb7b4d-8827-4a3f-88fd-877ed21c8173	3744728	704696ee-722a-4835-9042-ac4305287570	3613830	77db3fe8-4842-474b-9481-3cc02e2c5c88	4761132	c48c7891-d13b-4bbb-8a48-665467220e09	4288292	54eac3c3-8f38-41d7-903f-0f397ce74a8d	5039206	db7622a6-5ddc-4388-9665-e34754635a91	4053431	76cf7c01-6d8a-404d-a0fd-6ebbe46e5694	3623561	c9347ab2-3546-47fc-a3b1-f710b458e3a3	3677895	850f1c7c-6601-4f8b-ad57-94ff6e4c487c	5106092	57926f8e-6756-44e3-b832-4a1dacb3419d	3555951	31942198-e111-40f8-ab8e-c6d812e609e1	3733246	f7a2ec19-70a3-4155-8978-17f35257f284	3598452	aceec74b-e2c9-4b59-967e-beb0af67ee1b	3478600	05a3703b-2af0-4f49-ad16-13227d34c5f0	3803916	b6d39ad4-0cbb-4555-a28b-02273ecd9fb4	7311022	f69951f5-503e-46c6-be08-3507b4571d13	3561670	32e1eaaa-31b7-4125-bf02-246b76049ba0	4030404	1d84ccbd-bfc8-40b7-895f-03eb67477f46	3636784	ca3d4316-17aa-4d6f-b6a3-73108fd7c4f7	3641608	40f13971-4236-494b-b12f-f9c1edd329a5	3478527	cd9fa367-9c1d-4de0-85c0-1a87da33f1c6	3784404	a921a622-db91-4a52-8af7-98b784655511	3777791	ee85b3c2-b5aa-438d-8983-9f0d65aeefe6	3568988	00d0570a-2097-459f-a987-37cf3fd951c7	3954475	564667b0-79e5-414b-88d4-09eb967929b7	3843426	e1e505dc-6c03-43ba-a4cf-ab27b919c6a5	5921660	8ef3f3cb-5593-44b2-81de-9495c8285f94	3627203	f7ccf17e-d126-4bca-ba78-298a95df3823	3978563	e38225b8-2f2e-42d4-90ec-83ee1bb17b95	3502179	049f6921-9575-4c25-9f5e-6149608800a8	3558356	c3f1b924-51a5-4030-be0c-3bcdbd7c5e44	3627657	24a901dd-f9af-44ff-af4a-0628a6d00b1c	3503850	6e3a07d9-3ca1-4bfc-b7cb-50f9609b8e3d	3609787	0558ec34-28c1-4999-b59a-f63007e24f3d	3923383	4f0dd0c8-7457-4ddd-9872-acc2617a5e9e	3666961	f3238063-58a1-48b4-b2fa-c5b8badaee62	3576974	c39bbc5d-0b17-4ee0-a344-9358841d61bd	4110190	15e2ffb4-fc8b-411a-87b9-8e0023926b09	3527724	f1f15e06-2c2f-4bc7-9d2c-1c2322e1a3dc	4577736	cfae760a-858b-4629-bcd5-cb5168591009	3461976	9c7a22a2-4191-4979-90a5-398733c50556	3828386	a8508263-cc5a-488d-91e1-b2c80de1cfad	3761738	5888fb4a-c7d5-49f3-87ed-d7c65b59e7a6	3710557	2ec37b18-1154-4a54-984f-8515a86b9fcb	3637799	292f3b5a-6e00-443e-8ab6-90aa932ef4af	3733183	282f4971-216f-4f77-b9a6-71de6f9956aa	3606918	ec66e2d3-b48b-44c6-827d-ad48419ef440	4203251	042a973f-cef2-43da-b600-0de8a5732dfa	3795402	ba68cd39-a389-4741-841a-02a7b4a75cac	5259125	ba360bd8-6c04-48ba-9d5f-955b27531882	3680262	b707dba4-aaa1-4ffc-ae79-ae1f383a7e4e	3486465	4b2622d7-d2a0-4e49-ae5f-da52a3f94170	3874244	09634bd0-640d-4ba9-ba7d-2ce44337d7ba	3759236	073f6d45-9d4d-47d7-845a-0d8833a91253	3441280	66477aee-52ba-4f25-994c-aefdf5695c6b	3654893	b44c7818-79c6-421b-9e04-b530c995732a	3569902	fe66744e-b9fd-4877-8831-646e199b8bfd	3332176	7c4a1740-5e96-4080-a507-f6cb2601aab9	3462787	72d8464a-b492-406c-889f-6d3a0998e8e1	3413043	b05cc03a-7dc1-48b6-963d-51bdaba8a9d8	3602401	7fc4f45e-7643-4ba1-854b-254802f783c1	5060219	b743a9fe-dc7d-4ecf-90b6-b1af3f48826d	3766523	b2380a36-e6e4-4edf-9fac-3cc265ecd82e	3581451	5bef86c7-9384-42a0-b493-2c3312881677	3642722	2421c85a-5249-4397-95d7-3174627c25ed	3805295	0fc3a299-b77d-43d2-9576-41e7242e7770	3723945	a9799608-ce35-48f3-ae75-b2e85c2a4101	4073237	c2c57b9d-752b-40a4-9b9d-33a49d766be0	10449812	3c38e1ad-c4d0-4396-98ce-8e8bdbd0b77b	3752471	ea002fb0-b13c-4466-9e7f-844223d4bc73	3635659	52b4e989-2fd6-4a73-a5a5-d78ffb2a8aa6	3877025	2e3938ac-3264-420b-9590-249ddeab83b7	5913170	26a85e92-62c4-4c39-8d1f-15a099d12a63	3892070	731033f9-3c5a-46dd-8d38-0c2faa73036d	3601624	9da31ae8-bfa7-403d-b332-6d13a1733d6d	3788280	f5fbdf76-a8cf-4fe0-a867-068d034eea00	3858095	c5b54a13-ab43-499b-a2a5-97cb1a319033	3884876	d450181d-bc44-46bc-8686-2cefd1928cbb	3830610	48386c73-796a-4dfb-9b79-012b8482caf2	3593588	96b0b98c-a786-4373-9f8f-826c37f2d64b	3914853	aaa72be3-bc86-40ce-9eb5-4f6a1ef79d2e	3855934	d77e2e96-c0c6-4600-ab63-54599a78c96f	3663574	bf12be99-e2ae-4da3-8fa1-153ebdd5a850	3847556	a38af382-258d-4606-ad60-b2fb8dd9d4c3	4256481	4d4eac32-dd20-4627-ab4f-02ef3c85f13d	3705009	5615dd6f-d1e9-445c-b848-316b489c7025	3479607	f674e133-ad04-4a2b-b148-27fd8757e663	3575230	70b836c8-4652-4ae5-ba22-334414a1f787	3755701	99025f52-ca5f-4599-ba0c-19ac7680627a	4040889	4f5faa1d-6595-48c5-a753-84f79f2a9fa1	3822427	05fbd5a5-0352-441c-bcae-9ed1fa26a458	3415218	54480255-2b62-48df-9602-d242e1874aaa	3366260	73abf44b-4b25-4679-bfa2-4bad3905d133	2958426	5ad3db2f-e049-405e-ae61-2e962341537e	3730681	7a6ce4b5-e1c4-40e1-a340-5e3a688244a7	3857381	b51b3ec6-4581-4919-a872-3d65598c520e	6173579	7cd554e7-2e39-4a6a-bf9a-b4388637e6b2	3917728	26f62a23-0cb0-4d4b-8078-7ac73ee9ffd8	3669435	25121322-83bc-4d51-bc2b-9987d0df941d	3582967	343fe192-1cb6-44ae-9a65-83c424d5d5e3	3652747	dbcf63c8-4106-4273-8642-5f7d4af1f51e	3376584	66715078-133b-435e-b685-5ac5e0610778	3610687	7b7b306b-8f22-4728-ab23-065fb1b0ec3e	3971450	a39d9b72-a24e-4ec1-86ba-1ce58f6f947c	4411555	aabfba1f-88b6-4dd3-bf1d-14c4c8a182f8	3701006	19d99774-058e-4faa-911d-1603a27938c4	3780166	8bfb663e-efee-4b59-82cd-83a91c5fe58f	3926404	e18cfd31-9237-46a3-92dd-d8028f1c8661	5058695	4b93528a-b7a8-43bf-a6f2-f08fda2f52a9	3592640	d03c4db2-0eda-4f2a-aefe-a3405ef6f512	3585675	ee61a95b-167c-4ce5-a9af-fc32363f21a6	3942392	7e1be893-6af4-416d-815c-b0392d79ae2f	3493547	3e9b4a7a-6785-4ae2-86ac-54b6444f002b	3498928	c037f8aa-34c7-4775-b1b3-0bacc81721ca	3707605	603e7ad5-7243-47a9-8aeb-ecdde92d4475	3362110	65dd13aa-c723-4f28-9d47-051525fd5bee	3793163	7bf2a843-a013-4d16-a2f3-65f8536a0919	3541644	ce53b099-6b34-4d16-a03f-259fb862195e	6004931	d018b638-6290-4336-8e12-9f94e37680a3	3902022	6cd99052-a8f8-49df-b828-bd3b7d1cfd9a	3661628	baa8e258-8934-4c34-985d-db1354b8b295	3774066	1f913ff9-5b69-4524-a804-91aadd694c8d	3840155	b911e72c-f12a-4a6d-8511-6eea609813e6	3749597	7745094b-7cc0-4a23-ae1a-8730ce63ce06	3705424	b7d28b2e-dc5e-4563-af71-204dfc536a1e	3704256	77e3aa2c-4089-4f1f-9157-a70dca353113	3682158	1a845adc-a495-40e9-85d7-894dec3c0852	3268942	b247bb32-10c0-4e97-b36c-3e0b068f3ab8	3641500	237189b5-da5c-4e90-804b-825f9c4cb3bf	3533394	d6694f2c-6906-4af9-9b86-9fbf724304f5	4953637	ee6679c1-1304-494f-ae58-2e6e80ba9cc7	3734889	b9447a02-c949-48ea-8920-b7bdbd0b01d2	3535295	6eac6660-fdfe-4898-b490-14357aabae91	4093473	c86062fd-8edf-4964-989e-3fa45af27590	3559680	fe4f0cb4-201d-44ef-be02-68f5c3fe13fd	3676122	e1017f56-b17d-4283-87db-73efaff5abd0	3975830	6e5944ae-e897-49b1-a38c-3ecec66f22c0	3713935	4fec18a6-4be8-4ba0-9f33-9d7ee509c676	3677470	c44c3049-3d6a-4307-be5a-bee4fb79cc17	3911618	3700d0c6-20e7-481e-bc68-03329f288479	3448495	ee0d6437-2fd6-4504-82a6-b6d1b8ac2181	3749342	4c4ef8bb-c527-4105-a8dc-bcee797fe38d	4737967	cd56fadb-cdd6-4b2e-8a03-d703e243d2be	3530500	b680409b-3d6d-4f14-ac36-2ffdf1e67e7b	3633235	6badcdec-c078-40c0-96c3-d7ab0619ade6	3511011	a8d85b7b-334c-4569-813f-4589e27a2648	3654971	daa6fc39-1830-487b-8012-056e410870f8	3697350	640538e8-992d-43f8-8e84-aea3015ae401	3771725	705337a7-0223-416e-82b8-04eee9245b93	3575649	c17481ca-26cc-49c4-b52b-553413c90abd	3459395	69e86a4e-a454-424b-873c-ecad3a8a5aa6	3666511	90d416cb-c1ac-4cc3-b700-9461572667b5	3438626	f2715fd5-22e1-4f27-a505-c125a740d8c3	4454076	ca491b5d-3abe-419d-adef-cf600aa99ef8	3682598	8b86e4b4-a1c7-4387-a21c-469fb431102a	3078805	776cee8c-5e6e-4ea8-81ea-8748b4693d77	3980024	7de4193e-8ee3-4694-8bbf-f3483963ec3e	2979218	3bf67467-1479-4e48-9549-af1763494723	3621147	65abd021-ae04-46c0-8dcf-a77758ca740e	3487697	4ec8608e-096c-4105-8faa-92ddfbdf9693	4383177	fce253a7-9cf7-4dd5-95dc-697d21c42cd3	3722615	e243e43b-26ee-4316-a596-f1c9b421fd66	3631924	68da85a6-839c-4d6a-ae21-e84350f99021	3746811	abbe2232-9595-4514-8265-3806ca24690a	3632477	aa49ce69-4ca8-4df3-9cec-dd0c5926ed6a	3895418	95939f1f-64b5-48da-97eb-e8cd316e75d6	6001685	7d512313-2878-4777-8d23-0b39e70d5c25	3780215	e24c0827-9443-4959-9bb7-6fc8fc6efd34	3595773	4cf118fb-3876-4350-91cf-c01a57e609ce	4014210	aadd39c4-db7d-4be6-9542-c7ee29ad6564	3433660	a12543b7-94f4-42e1-94a7-49387393734e	3178343	a083db77-dcf0-4c31-972e-b593625fe157	3394082	83a447dc-9670-41fa-9a55-4dc158b3a066	3837541	3a010fe3-8450-4a8c-83ba-b94ebc136bb8	3548829	8e25f215-ef81-4047-81e3-dff07aa6e322	3594053	c9c46ea9-d68d-4292-9a52-a82674eeeb0b	3492267	5afe0826-85da-4fa7-be91-7fc08cb678cd	4757652	dc6ebd9b-a834-49db-b9df-8abcb6307d11	4255689	5fef77be-a6df-4e3e-83c4-2aeb8e273c5e	3578607	5c0aa31e-52a1-4073-9097-53c4f90ac56a	3786237	e334c0fd-a5f5-4eeb-b6f7-03aaa464f1cf	3784771	b26bae8b-810f-4cea-950b-0ef84c8951f3	3818497	0340f5b5-9a76-472d-bd09-04e507c64016	3888097	d497e9fb-b0e3-437d-9f36-4350a7586d27	3533828	f747d176-f361-4bc0-85ae-b0222b2526db	3617926	6c811d79-b7ca-4c34-9ddf-264155e20c9f	3554226	ff22c02c-3e68-4e3c-a828-73a765c14434	3509985	2dfcdeba-0545-497d-95bf-bfa8f2e27325	3379633	3cd5a229-09c7-48f9-8546-b577d5d29225	5949531	dcf7c9c2-d839-4f43-884f-fc9ca128f1d4	3485506	a61443e9-c059-4793-aca9-f508dcd26108	3743794	adf73d5c-e4f1-439a-a2d5-7c70827fa6b9	3530930	208a1859-255e-49de-bd74-30bb1ef4dd6a	3612177	097cd957-abfb-492d-8c73-5aff9c539f4e	3874743	c8c5ae92-251e-4652-9cb2-9a2f5429165b	3436700	f0147e33-40ef-4497-8d24-c12a21a90ece	3639902	ab679459-8124-4434-98f6-66173b96fa3f	3443998	ca7b3713-a5d2-4d56-bf9f-eb9f7ee1a7e1	3962745	3888c0e2-fd97-4789-b38c-94080716420d	3976656	e11f8988-139b-4d59-8dc4-c4c8e1b0b761	3846363	14444d5a-2d1e-47e2-8724-ee3ae972bd4e	3640928	8744e071-a93e-4312-b956-b8fee7b7c22b	3883956	ea2ddc76-4d2a-4c58-bda9-23d1b07cd87e	4197494	3799b3a0-60d3-4591-ae53-38cf88de9b3f	7899633	f99aaffa-0b22-4e86-a13e-695fc8677d6a	4360740	ee4bea92-e2d7-4f9d-9438-bec949db3c12	3538580	96145299-c9f0-4502-a124-fe98b9a8bf1e	3518197	3b022aeb-7600-4ee6-b755-52e3223b38a6	3658970	079b6bd6-fd26-4631-95b2-2e7a0c43bedd	3521374	b4609a2d-b9bd-49c1-9fd8-ce4812f56fc5	3667137	e998fbc3-0d91-4ae7-a869-024384cc0fe2	3837912	124573d5-85c3-47b2-992c-d008e2861bf5	3574970	1af56d14-8a39-4f65-84c9-f1c26e4b280a	3420159	ea920061-fbc4-4389-8475-640106add89a	3489896	27a8e581-7503-41aa-a8a0-6de28c323866	6200029	df66c9a8-60d0-400c-9d43-e2285e743482	3662699	297ee05e-f5b4-45ab-ba67-3417318659b3	3704579	25425b11-0f56-4621-8095-c94a83925e50	4271126	10827439-6f8a-4484-9c40-086002f86aae	3595959	202603d4-1c0a-401c-a22c-5a010db580c1	3809943	91604b01-8f04-482b-876f-1873beb7b112	3741537	2fe35393-1d6c-41a8-9539-f262ecc990b9	3614821	5092e200-f470-4304-bdf0-2398caf848fa	3450099	867c8861-a00d-43da-ab4d-7d8228a390ea	3429530	2f10812c-1541-432c-b2d8-546f12261b86	4285198	3eae0ec2-b288-4204-9d02-f0b20e539856	5611672	6a240872-753b-4b99-a0b0-a9252e55afd8	3896709	70378e69-994f-4afa-aa52-8a66d2f35175	3767975	57fca23d-a975-4540-bad1-2230d007c786	3370170	28a73406-7356-4a92-9afd-53757f6388c1	4011111	726f8c8d-06f5-48e4-a67a-6e488071f6aa	3780366	45840e69-acb0-4e13-91a0-122ed2d25ae1	3662273	2268faa9-1dc1-408c-bd9e-5180e8fae168	3901997	fabd93ae-0718-4adf-86f5-471426f77621	3529606	20432dba-fa5b-4b46-9499-2eec3b4797db	3742353	5e002a7e-496f-4e14-96ce-1c387a4e85be	3740109	32dc37dc-c1fa-4623-baab-d3fd4d94c6f7	4919725	53020927-3d92-428c-8592-51494584fddd	3658402	d9969854-c78a-4c83-9808-142d18bda833	3686738	aeb9a6fc-07e0-4cc7-b365-c427a4bae83c	3763479	04b301b5-90d6-4000-b1c9-fc7e2977cb0c	3780767	fcd6ebb4-17a5-48a8-baf6-342319b5ea8f	3638708	c3c2067f-9362-437c-8adc-02bcd32952c9	3729156	abef657e-599d-4b23-a8bf-7e3cf0bfb03a	3274988	b8314ffd-1af8-4a23-95d9-a86fc24dc2b0	4048841	fa2cfd06-e9e5-4b5b-907e-b8118c24ae04	3540568	12d276ef-0a97-4c91-b9c7-0067f48393ff	4137963	f13d9402-840a-4578-86a9-cf9cdbe05bad	3679298	8d02afa2-46f7-4eac-abfb-a6210bff9410	5687156	4b4d82a7-ecfa-417b-aaec-4df2ce9bbf18	3392592	039a1492-9ab7-4c49-ba34-f979b1db4f7b	3833293	863acb2d-6ffa-423f-8c43-d2cd35641d0a	3220599	3cf0b0ac-d778-4280-98ad-ec41dead1db7	3958874	b6867024-3f81-45de-aed6-6fead549e03a	3505097	2a42bac1-19b4-4ff4-b6d4-2b4ce240e0e8	3708068	57617079-9106-4031-937a-7635a9b92839	3702203	5c7d1283-ba28-4334-bac0-2cb02dfe5e23	3708494	8670f3bf-4453-459f-805f-07e304f9e77c	3141117	a31f8274-be7b-4c8a-be30-11de5f76d50b	3858099	527365b4-dbba-49ef-86a6-c90688453f21	3764075	d54ca0d4-283c-49fd-8115-e8a1d4e11f7e	4200416	739776ed-df81-41ae-beda-5745a6739df1	3592372	d0e87740-9126-4c44-b683-460ff2ecce47	3817813	2c83535a-6eed-49bf-a583-105977435e66	3831367	93769011-9f68-47f4-b209-0c03246dbc84	3573880	ef168fe2-8ef4-49ac-bddd-e1fc76ebc6c9	3899109	0257257f-a220-4454-9930-415dffc1ba4d	3599924	6dbd3551-36c0-405f-bbcd-ba6a215ac8bd	3714766	59d86e5a-6185-4cef-b726-fb8004d2d74a	3600280	6c30343f-d752-4e8c-a48a-fba51b81c56a	3562291	81620118-72ac-49f9-8f73-dd1b99e4d10b	3391883	4a40c599-d4f4-4b79-bc84-7c36d720eaaf	5920140	e85657a0-6b5a-46f3-9160-1e0ceb54597f	3643274	ed46209e-1649-4ce2-9fd3-874aa8d44052	3568724	8174647d-5358-486d-9273-2b2ce2bdaf39	3648607	e306d5bd-c979-4893-8aaf-025188745a2a	3834314	51d380df-63ba-4062-a7de-3497e8023064	3394615	4dd71217-da65-4d09-86bd-9c7a1ed78d6c	3741458	79775cd4-8382-4eb7-a8b6-ccc669141876	4235937	50624fba-1b33-469e-9444-3202e6edbe8a	4151263	3e3b055e-56d2-47f8-afc3-0b4499730c6e	3877338	12bf92b6-72a9-41b0-8190-d7675881fdc9	3809821	69064cc0-44cc-471d-834b-2d0b1381f9f9	3462489	2f6fe460-512d-4d73-a91a-2d7a3622aff7	5287973	a8053ba6-8d80-4fec-9871-cea962886f65	3596458	fa67b2c0-b411-4787-85dc-bc13c0d53b22	3423948	15ae42a4-7757-4744-965f-5ce2acd32429	3451041	2ef76b5c-9ce4-4ec9-b79d-75b6d73c7b88	3715278	c0f4357d-4d68-4a10-a0db-70a5e2e6caf8	3665784	963b11fb-cd51-4c82-b754-9253f4268d9f	3181848	82040392-41ac-4e87-8a62-01b09cc7edbc	3929038	caa12c42-7e52-49b4-8d86-c3738c22f3d9	3538081	725d2083-3693-4fef-90d4-f5acbb69beeb	4180801	2b8e99f5-91b7-4395-923c-6cc417812684	3972252	dc70cd58-d124-487c-82f8-48c407bfcaa7	3476278	850954b1-fee0-4450-8b1b-db555fad01e7	4701890	963fc76b-212d-43d1-8b04-a7cceba29e0b	3801531	eda4a625-3fa7-497b-9744-03fb277b9fdf	3813795	6aa1c557-17a6-465c-8019-a713071d7d44	3719066	8a513197-2707-4b5d-80ad-1062bf8a069c	3961944	d12ea0cb-7af9-4b8e-9722-eb701c56a092	3846539	08d252d4-59d9-449b-a7fb-b780a6f09fa2	4145265	06139be6-87e6-477c-84ca-ae554c4a5914	3685594	af603b4e-830f-423b-a7e5-d44b30ebcf86	3503122	8e7c467d-f430-41b8-81de-95b48456e08c	4256423	c590ea73-9bb8-4138-a26a-870d612ea791	3835400	c1c8b264-d698-44d8-8a21-e6df4c0cdfc1	3855352	1e0ad15a-f847-4c8e-aad5-9a159fd07c01	4238029	53a01b8e-6cf5-48a3-b496-1aae7b467277	3978944	f3ab3721-65ae-4be7-a603-a0c08e260f3f	3439178	05cd74ef-4d12-4b93-93a2-ddd1e6d3c55e	3568547	812d1438-3aae-4fd2-a2f4-eebd6e1cfcd0	3821367	f46a5390-4a0a-46da-84d4-bba812da5052	3443944	83adc511-003f-451f-9db0-8feea7b7d6cc	3655362	2076315a-513d-4a20-9033-0a4b8b59c84e	3541688	1f71e828-dbf1-4fcc-9850-15f2f299c804	3718358	d7e2e04c-1731-40ee-86ad-88cd710b1002	3954108	55685a4c-2432-4dc1-afd4-15cedd80d177	3512395	715e9537-ab07-4302-ba9d-9454f8005966	5745020	3685de56-9b1f-4b15-a438-ce715ada1a0e	3703723	b42cbc98-8001-4de3-9e43-76ab04f45b9e	3812519	ed308177-5a84-48e9-adb8-ad73459c8000	3849134	8f887feb-3d53-4846-8274-9b901af793bc	3868970	f434164d-a99c-4494-9c02-adc3097f746e	3284660	6a4c9a2e-104c-45ad-bd5a-6d09e0da67c4	3618101	01e49890-32d9-4bff-a22d-047a88c57ef7	3573147	cdafe65b-576a-48c7-a12d-24dbac26e33a	5191050	c144223c-0406-4ffc-8356-69d7af0bb420	3567336	1caaaa91-9e53-4f02-aaf8-55cc71db9d8b	3653705	7d61d615-b4e8-4c04-95e6-08ef20587121	3667367	5afc095b-aa88-4c42-9846-694fd4e19264	7304165	a76eb714-bc4f-453f-912d-185a01bdd7c6	3656378	1d0cb40b-101e-4f82-b322-82c978b2c83d	3696563	766220b7-b82c-4c8f-a3f6-7cd7be829346	3733829	1e513787-ed98-4435-aa1a-d630063fdc74	4343608	d65c98dd-d922-404c-8252-9c06c53aaba8	3630487	265364d4-8590-4f2c-af98-c0cc61adc805	3677103	b044b0f9-4363-488d-ba61-f9d5d6f832b2	4130054	61642c37-49b4-48ce-af98-989b23e61f01	3667001	e542524a-c975-4b9d-8f3c-ab52e88b0191	3785455	c23724fc-5d87-4aa0-a6a2-89fbe904b6e9	5962630	72ccdb41-e61d-4ca0-9673-a1fd93864fa9	3730719	eb8a42de-eeab-4d7f-afdd-958bcd1a0fc3	3519423	8dabc9b7-9cc1-4e94-893d-72c891a73c3b	3497179	dff5f5dc-9359-4a38-bc29-9622d3f4c753	3165390	afc21d5d-e2fe-43a7-b8e0-d6e0c580af12	3575713	efe5857e-ba19-406c-a5d3-92323b5e45e9	3790068	900a55be-02dd-4126-a818-e0ed5ea34c57	3550951	b7c659ca-ecaf-4c9f-9fc0-a0a5a940ab00	3663887	3dfe6c24-e932-400a-bc39-bf72186f1df5	3545501	26917ebe-f37e-46e2-8b5b-549033e567dd	3995167	3414d354-ac06-4363-85fc-98028e55028e	8131957	056b2c25-76ab-4a78-b7e9-a4331046d551	3738770	2ed15a7c-185f-4c8d-90f0-a67997998ee1	4868856	6d5237ab-1e4f-40b2-a1b2-fec2fb94f4ef	3656897	3c0e2bac-fbe8-4e62-a4b1-2e3282d0f523	3751039	521e6da1-39ff-4efe-bb05-5641ec677ed8	3756381	57a6a9b3-747c-4c78-b522-3f18f9dd4a49	3609821	0f3b8e4c-4029-4064-a5ed-04820b1968db	3550707	56d02a28-2181-4a46-882d-509cf8e89625	3444086	16633808-3341-4757-a083-75fe7aa19517	3643362	de15b249-ec1f-4b50-b9ea-0d61290d181c	3710273	6634747a-c1c3-4e1b-b538-b89631119b50	3633606	c80854f2-da99-4c25-91bb-3f8ecc629106	3598653	e329ba47-e4a3-455c-9c24-3bdabca95dfa	3630262	47067cb8-6eef-4772-a68b-f1809b52babe	5932018	412e3faa-6374-468e-943a-93d09385e940	3757710	85dd963a-0b44-4124-a204-04bba6680913	3500498	9e4362fe-9c6c-457f-8eda-31096f6c6974	3243788	a7872948-8160-4cf0-8f3c-ba6e7bb2dbc3	3462855	0c01cbae-81ab-477c-81ef-6cf6fc03acf3	3564119	f1a73f7e-811e-4831-beef-389c507f6107	3472891	7c10af38-b3d6-449c-bdfe-b1969195737e	3448514	09dc5167-5c98-4a01-9f21-8af68d4065e2	3769671	c7af264c-d3c7-4df5-8edf-4b933d5b5c99	3577796	0ad0ee1e-b524-4a9b-8ec5-121446dcb0ed	3878776	f190044c-0702-4ab8-b36b-b0107ef5c1fa	4977921	2f1f24da-6da7-427f-bcbc-9d3b358bd468	3942817	3a895464-1718-4faa-b592-1f7f2beb1e05	3630468	daf7d458-d099-4ff6-b35a-3ee0bd612891	3514644	35e2707e-3f1f-4dea-97b1-7001741aa9e9	3639456	3b42da64-6f7b-4995-b28f-aaf7948f95a5	3679098	8ab67c1c-9569-4384-8da1-a323620c565f	3403286	b34726e9-111f-4e0d-bba0-4a90ea9af675	3462792	5212a2c3-be67-4146-a05a-2fff302f3672	3879308	fb8687ce-c80e-4929-bfbb-2673c469673f	3552300	61e4b519-6ed5-4bcd-905f-30ab459cbdb6	3765487	3c3aa44e-c4a9-4b72-965c-1e40437d7194	5709103	c8952634-9e7b-480b-88f6-61516b54c728	3560473	03ed0232-0b1c-4c2e-9be0-bd43ceb7bb97	3565673	801903a0-fc65-432f-93a0-059218b430ea	3518485	6e22d2ab-ea92-422a-bc9c-4f37789a5d05	3634452	bc95ad8b-8e09-4edf-b58a-cfd6081c18cd	3538027	bc166317-11c3-4937-8add-b0a95b203a0f	3592449	9b2dab74-3575-4a42-904c-e5bcc07148c6	3971798	9b3a60d8-0fbb-4b41-bd78-6173e70ac9d4	3512513	5304ee45-bc14-43f3-91ed-ab356d78177a	3606810	d1a86a1a-28b4-43c2-9ced-60d206f59260	3685590	c18b9f95-eba1-4343-bb48-9d4e2f98cfdd	3442659	bd8e8ef4-c161-46d8-9b3b-31fef147052c	5052232	c31e03c3-bf1e-4c09-905d-6c1ccb12f1be	3955467	7238a07a-1483-4937-b3a1-a89eebf43d28	3962780	3d74c18a-9fc3-446c-8879-8e6dffb0da85	3926335	2370c2f5-001d-417f-a909-fc088746fa51	4486937	a49905c9-d569-4262-8cfb-d726cdfa9d6a	5018936	5cdc8a08-283a-4e23-9191-77b0e41df495	3604488	a3e97030-cc82-4531-8b01-8893b05d0443	3642326	0cdd26ba-9739-4a09-bdc1-71ad32504486	3505816	b3221fc6-2bcc-4b5f-a19d-f60e749fc2a2	3677025	bc93ed19-6240-49ee-92f4-03d4cd811e05	3490287	762c0cfa-a31f-4bbe-890f-9243c66c7fa8	3994786	e41c88ea-904a-4507-a8ba-434eb211d73c	3453475	86b76f0b-d8e9-497a-9416-c82ff0cf12ed	3496421	5dfa8c9d-22b7-4887-9b9f-e590dc0352ce	5884614	95d1485c-0c82-4af1-a2c8-3c188c028398	3563396	af99ebfb-b900-42d3-a85e-0a178b2017f0	3611087	69c66a53-d5f0-4821-962b-b3f55b4f7b54	3775361	f7675ff2-da90-4ad0-80e9-4ee307fea060	3834451	48f35762-67b2-4af3-8315-56e5362675a0	3694548	6ce4aa51-4f29-410d-8990-6e822149a4b4	3554915	6b86a835-4d9a-4418-9dc5-204843fa09ab	3681127	d4d9d7bc-6bc2-4346-8b7a-54de46ab16b1	4131516	fcc05b5b-97a9-466e-ba86-bc03bb0d64e1	3780278	81625c0e-7601-493c-a251-0a11e115b3e0	3416958	aea8ec4b-225b-47d1-b404-533316e10c36	3376021	333169a1-43fd-442c-aaee-d3dcf1f4610d	4997556	ac6f1214-52fe-4012-be37-2f60ad1c598f	3630429	7baef7e3-0e6a-436f-9078-243dbc1c252c	4377438	af4cbb9b-f19d-434d-97f5-a217d8d43266	3877274	656be642-ce9c-4901-b250-304787f9a971	3536888	c7aaaf3c-9e63-4e18-b1fd-3014e164db40	3538243	d47d4b70-b6ad-43db-bec3-e06a81ebef6b	4120367	5d0d6587-f5cf-43f6-8026-689c10870959	3846383	6a23d0bb-bbb7-4c64-be6e-47ac7631864a	3516642	5af86441-5cea-462d-80da-5fbef4567cd1	2940555	90504e49-9f30-49d0-b5b3-6fc4374b5793	3656931	3a9fef5d-389a-4a46-b409-bfba161f3dd8	4729238	5094868c-3705-4aa8-bdeb-46dfce5d4db7	3818438	7809ae56-396b-4edc-a109-cff4cdfae34b	3600661	2187fad1-953f-4a65-84b5-ae518f8f5288	4487197	ffd488c1-b697-4397-b1b5-6ffca30a5840	3845371	761cc833-0b0f-4ea0-9a22-bc2b5f8c430f	4075290	2c6e9cc8-95c6-410f-91d8-017d2c7eac1b	2910792	c64543df-798b-4aa6-aeba-7a8a5d43c44e	4102828	c0c480fe-6b5b-4439-aa46-6e5640e94291	3864356	2a958216-9202-4150-84ba-9f9e0230ed6d	4241837	3d5f5ab2-bd39-43ad-ae34-c6621c0c0561	3516550	2b614192-03dd-48a9-963e-2a895c4ef9e9	5736583	08ae1d4e-a9c1-4a7d-a4a9-80471d5d4ce7	3416577	256095f3-781a-4b1f-9121-c9c0e86ca8eb	3503640	ce53950c-b46c-4520-8d36-317e730068a2	3834759	ea800bc5-d6eb-4372-869d-23d449542020	3556489	085dc892-e59a-4c60-8543-6e25296f956f	3796975	111fbcd9-8d7f-4013-a990-28d64a834ee7	3460949	a62776d3-27b2-49a1-b6ba-98160c3a7bb2	3254488	946f9ea0-3fa6-43cd-81dd-91100c31e236	3648436	9e6931c2-1509-4829-a68a-33f463327886	3459292	a8b1a291-51cc-48b4-9b74-a936a84ddc58	3636221	d28400c5-b02a-40d4-b838-d39c69c52843	3557422	3f39ad90-7a61-4f0e-a742-04fc8dd8b5e0	4629514	74fc2aaa-46fd-4af8-9d15-6921e9e00ac1	3652786	466f897e-6ef5-4be9-bd55-8c1edd6eeac1	3536199	9128d002-3a54-465e-ac25-09499084d462	3646657	3e27c4de-6932-42de-ac53-f44953032b14	3579902	6a85cddb-56de-467a-a05a-4f909ba0f97a	3662552	4c838e82-1f39-45e9-b351-842df3766bc0	3583583	88d51469-82e6-4821-82b1-d841befbd5f4	3353058	5da89c09-6874-4ebd-bfc8-074ab876c71b	3508861	46c6bb5c-aee3-4166-8a30-a0fbcc83e6ca	3504784	a07ed868-64d0-41c3-a24e-dc5e27f42666	3392572	437d2e63-d8d1-4379-b95f-ad280be59440	5082181	fbf17816-ad44-4c54-9ed0-8741c2604e1f	3907213	3925d290-8239-4d5f-ae05-7aec3e55e75f	3720289	71f6dc62-5107-4e88-90b6-061018916d1f	4041818	6ee8bbe7-c792-482b-b8eb-aea2a25a01c8	3862474	9bed2a39-2f70-4f53-afbc-fe2198c958fb	3836485	5a5cc3a9-0b96-48af-a976-7be81b51c9e9	3510557	277633cd-e022-4c97-b20c-3001ff350afe	3730153	10cae999-f6d4-44d2-b840-4dc3562a9338	3496900	51802e56-f5cb-43a8-b207-0ce778767671	3560429	80fd7d2e-947f-4d39-8b28-d7195228ed0f	3610192	a68c8176-3f98-4645-9b81-519adc61a1ef	3914081	b6f3042c-dd4b-4e57-8100-275d490156cf	3738110	20fc86fb-ab52-43c2-bb70-774b28f564a4	3679450	c6bf3f92-d12f-4cd4-9dfb-4aa762af1ce2	3609142	11524aab-d96b-4fdf-803d-074abc60e0f2	3527274	578846f8-77c9-4c48-86a5-52ce8bd6699f	3606556	372c5839-bab3-4d46-97fa-8303dee9bc5f	3788280	a8367bac-eb7f-46cf-be00-71f17d69b936	3519077	cc9b8efa-7b28-4a3d-908b-a291bc9aa03e	3923290	3a0468e9-c9d9-4a50-a8e6-40e99f355ab0	3458515	5117905b-4531-4d10-89c6-0ac860cd8a50	3648529	714f1228-4c0e-44d5-a37b-9914d406c730	4064355	2ed83d71-7e59-43fd-b0b9-c06fa553606b	3516652	95e8cea1-201a-41c2-8cae-e69eac793f04	3763987	4e450469-b5a5-44d8-979a-c37a0a18821b	3627491	e1926fe0-8389-4039-9a2f-6bb67fe32f1d	3631304	826bb150-76c8-4e3f-a1d0-0dbccfcde124	3707814	6e77aa11-c723-42e6-9018-8757014811ab	3953082	3f315c87-3cf8-44a3-8c88-35a4a284e3a6	3440914	72bc5b0c-7c27-44f5-ac2d-334540791232	3961591	e9e35d2a-d476-453e-9c98-069ec0a910ea	4117052	9ad364b6-15d4-406d-9a01-5cc8df04a07e	3675212	cf503ab9-c873-421f-ad53-aecf85151479	3532078	0a196e3f-e122-4aa4-8808-4f2e7c4be847	3510699	fe950979-5e48-48be-ab03-9b612a2706e3	4087534	c582ef6d-ff2d-4fbb-b781-055dafad5fa7	3954093	f70fd029-477c-4f72-a5f4-c7008baa76c8	3527430	0d184d2d-7cda-4d00-9784-02d5b0a8520c	3692882	64144995-5a15-4969-b04a-4c57018ee22e	3721584	3b392a7b-2396-4bc3-b2e2-5b7ed3e5c053	3826806	3af79530-4e7f-4c9a-af19-359c60cdb79d	3571695	68f1332e-9dbe-4a70-a760-98e8a84f6e74	3642042	c34a3b70-04e2-4116-95a7-5dafd4260971	3477627	9c8bb05b-b233-4e05-8288-3948fa91109a	3646335	9c880a93-55e7-4cc6-9d7c-b4a52a791b44	3928974	09997846-764f-4e96-8421-1590cb98a051	3630458	bfbe95ae-95ce-4765-a89b-50044bdcd8d9	4376905	d2d78494-6344-4aae-9303-e675368f8673	3877881	51f867d5-f7a6-4944-ae7e-0730cb769be8	3603174	378f50ef-a735-4d62-a92f-d0305d24f25e	3864273	fd313024-d8d2-46cb-b3f9-419707a9c22b	3889485	0543bda8-657b-4bd6-971a-e1dabc038525	3602020	ee12acb1-9763-4a66-8df7-7ec9814101fe	3555194	745be940-4627-4bdd-af81-d0973cfce341	3826582	6837ffbb-51f3-4f98-8e79-9ce4d4b36b2f	3768963	6139863f-fdef-456e-a004-47271a04cd84	3669312	ea70e184-b658-4c37-9f60-ea7f31e028d1	3477636	0df6b9d3-540a-4b30-94de-a062c1aea94b	3952510	247641dd-d615-41e7-b2aa-4c6393294895	4813647	e6bf45b0-d369-446e-9408-1ef3bbb78414	3791589	04cfe367-c6ab-490d-998f-077a2bf17f61	3971705	948e78c7-d6b9-4c39-a56e-b1ccd6323005	3195993	75aead41-3108-42d2-ad3c-140ad2b7b4af	3586125	85adc60b-3c03-437c-882a-f8d19cd78f26	3831445	bcaef831-40e0-47d1-a602-f2e0846255f5	3775733	3a6f7241-6b83-4f3a-a0f6-b3428a6fc93d	4007538	9acded9b-fbc1-4119-bb9f-8c9694406552	3554519	20a10a69-6aad-4106-9c0a-3531261c7a30	3584863	9da656a6-42d4-4e26-b1ea-c91b56548d5a	3810921	6727c54d-257c-4741-b5d0-243e06d30d5f	5732868	eb843a94-0a88-4373-9182-82bdb3a96e80	3765448	b4497935-c548-47ef-803f-d26dcf321ce2	3520768	d20447bc-1e5a-4ab2-a04b-66ca5c75344d	3259478	5a528b04-9e79-4d2d-bdd0-e6ce9159dd76	3837394	58d6c3ca-19cf-4853-a227-aea65a205dbe	3136087	8383eee5-17c3-40df-a6be-51fe4c0c182e	3815046	6b2c798a-e6cc-4a57-bdb1-758e4c3f9dd3	3794204	e7b18ddd-08c4-4b4a-bfe9-7f26dad744b3	3495086	87efe56a-d14e-4d4e-b00a-1ce5664ef888	3867792	4a71b946-2e3c-4acf-bbbb-d88469642c63	3288806	de9b49fa-a557-4e29-949d-7cb4e9e05114	3721540	07356067-0ae3-420c-a452-2afcfa157661	12164399	bc9a189a-42c0-4dbc-8863-00339992900c	5142708	453268c9-72f7-4f70-b34f-32d862c83696	3896558	14f9508b-126b-4fc2-bbd3-0bb79e5dc76e	3109394	aa3ec622-6053-4a26-bd0e-62a768add312	3802871	d6cbb2be-731d-4e1e-9301-5d6c39e8efde	3569187	968c060b-bcfe-4b44-8ff4-bfa1a90c13ed	3447752	2c182f42-a934-4078-aed3-8ddb526dea80	3566299	73080086-0642-4d9a-8da2-4770059c534a	3572125	5eff552d-5ba1-4bff-9978-60ae178f8fab	3699178	340b21e5-614d-4fa3-9a0b-f4c9c9fe1c9e	3629138	bba06aa5-c9fa-4906-9c70-66429b34f8e5	3635723	e489b6ab-b990-4239-8bac-f09f2cec6f08	5968951	8bb468b2-551d-4576-9917-12e63d2623d1	3527278	a9bc232a-1d97-4feb-be9b-45b2dd3aa809	4039021	de9d2bb8-2c1f-4822-ad05-2c86d7b88da0	4180821	9c252ad4-58ee-4905-b9df-eae8bdd09495	3683986	bfd101bb-aeaf-4632-8d4d-31c25ee06ef8	3523657	b53e82b5-4b52-4980-ad6b-b03b35e19d97	3978974	ff2e0e5f-c994-48e4-bcde-4af703ca70b5	3816728	7f25bdc0-683a-4ad7-b99f-86313b5b4395	3581265	a25b6ab2-caa8-4e6d-876d-daa5e846ac3d	3693014	af6f5e71-15ef-438a-bc93-42b719b8f088	9754249	551b5415-8c2d-4144-b5e3-cf2fadab24fd	4010050	cecbd114-3b15-4f5c-a9ca-b41874808138	3195426	4de45186-9cdc-4c35-8b9c-8c2a41a248e3	3971275	47976983-ffb8-4daf-89ac-5d5448d74cdd	4128685	8726a219-ff0a-437e-a14d-3817e8bcbf5b	3939347	f5707e4e-42fb-4167-bb15-79b95f4cbb84	3841402	ddb9ab6e-8ede-4e15-88bd-cadff6a327c6	3638656	f06a3351-b439-4b64-a42b-565a4703fd7d	3405466	c60dd7e0-59a4-4bb1-bec4-252890ae5e41	3831386	04d721c4-fbb6-45f5-a0ac-59575b440b73	3683317	8606ed51-695e-4058-a114-047ef13f189a	3358821	9fd719d1-dab5-49ac-9696-b38926c803e6	3635727	22063b8c-3000-4a57-a462-1a32ea668cf1	3650836	f41c91b5-8846-4369-adfc-66bea01d4143	3756885	e30335c1-6592-4667-bb80-5b4b5fa89334	3678487	c21ecab3-4423-4d6e-87c8-968553d44fc8	3663906	e12a5441-22a5-43c2-be87-ce09db8d9426	3362115	572eb406-6ece-44b3-80e1-c07147af98a9	3567379	bce04732-d21a-44b7-8216-d38a3eecbeb5	4602131	e71fb0e2-78e4-49a1-a8b6-85b5261c8706	3520113	5eb6e1d5-7de3-43a6-917f-af916b0a0579	3352373	4aa27dcb-48d4-428d-b599-18713aeee80a	3703879	eed772ab-141c-4587-b666-62682321c13e	3970483	af725d83-bb2b-4d1a-bf9f-3fd5068e37a3	4122673	39d02f8d-8fc5-46ad-9be6-be3fe74320eb	3248417	f32ab587-42fb-4eb5-bb10-cd1659cdf920	3716393	618cadf9-1f1d-42b3-a283-c0f9740527c7	3745843	4a5eb823-1e20-4c4c-8ca6-27cfdd21aecb	5166308	1fc11391-a96f-42b4-96a5-99749e165230	4912477	e9a1a2bf-09fd-4411-8395-481d584464df	3793695	d3c993e3-8c21-4759-b151-d71b04f92ef5	3560135	80945eed-c323-4943-9bd1-7625c9944698	3713587	5434028f-9cc8-47a7-bb3c-53e3b99f7353	3886934	8f65fa1d-6905-4aa9-8104-916a058f0d28	3608536	8b1473c5-5381-41cf-b3cb-aea2987bd1aa	3563532	0239e68f-2bb5-4b9c-a186-5604613b71a9	4013086	b2906887-fa5c-4bf8-aafa-2074a2650dae	3092618	76937833-e909-4b9a-90b3-ec2c4f4e32ef	7656400	997d616b-8de5-46dc-919f-e0d3e1c3a514	3921725	fbbe776c-f03f-480b-80fb-4e6815c5769b	3604724	4fce34b1-17b3-4974-886b-c647b8716f6c	3382385	c2ed9428-925f-41db-a510-a52054288fc1	3984560	640bb751-690f-4850-9dad-27236984fbc6	4140710	b8041dcc-fdc2-4f84-9a33-1f83e4603b23	3856540	9672adca-6828-41c1-a443-64e19b74bec6	3598960	c95d624c-f411-41f4-9525-2a6dca829c9e	3731741	1330d137-e072-43f0-b6d3-ad0267b375b5	3600808	690e1825-12df-44ff-b236-066439625f00	4005681	245f6a71-d4fc-452d-9a79-1292dd726b38	3898708	9dc454f6-c3c0-4096-832f-b5382b81cb5b	4021620	e5b54613-9f85-4de1-b8ef-24c38394b1f7	5069986	29e062a0-d817-467a-8980-3694336bf8f4	3599058	f065bff9-0a81-4c58-8987-c9e0f314a1dd	3708710	e746bc81-6af6-4dd5-9f1e-2a9e37fb1582	3611180	26816fd6-8ea9-4baa-8215-082251ec22c6	3656574	bd8b4b45-80c3-49f7-9053-2bbf04798f3c	3752050	8182564e-a0e2-4a79-a326-8da87f54e305	3839369	03dbced2-d740-4414-a31a-d6a1c57dc9ad	3773562	cb22a8fc-79e5-4800-9c14-f070ae330219	4027305	7b403b86-40d6-47f6-8c50-2b34e8853091	3576412	0250a7a7-93f3-4d98-b2ab-7ae922364d04	3656369	342e93fb-ae65-4302-85b0-c7776ca319f1	5365408	0af01a56-f911-4920-b4f4-6403c8e84190	3915610	5d067544-18db-4a17-a170-ac6071b66ee9	3668266	397063ed-2134-4b81-a8a7-8e1998bf440d	3428455	9d5f457a-adbb-47bb-b466-8447dd9d587e	3849687	f75d6559-5b5c-4026-87c6-6ed85bcd5fd1	3869835	fd35a70c-6ff0-4319-a0a4-384ed3004d89	3398071	22a39aa5-b266-4f12-8930-c494c6e73bb7	3707844	a4e47771-3cff-499b-b90b-b9a1e8390d22	3533403	3ad795b4-cc06-43ce-8293-8f045a9773e3	3809396	e5e8e0dd-5363-4452-ae3a-76fdb818edd3	5352944	7962f6eb-257e-4358-8742-ad8aade21deb	3649678	538b032b-2cdc-4b12-b149-15785c7060d8	3570566	2c57343b-6a74-463d-a579-0d87b25657ae	3614382	30fc5b09-0455-4f95-82df-5cdb2711124c	3466820	e032c7f5-adb7-40d6-8c9b-911d65a1632b	4031069	19c8786e-f3a9-48ae-bdd4-188d4114d406	3780503	d0ae0de5-e933-4385-8f49-a00a3e02c4b5	3541541	8df2a2a4-16fc-471f-9a68-4cf1f045ded2	3544494	546f06a8-33a9-42f6-8916-8c7376e16eac	3412695	c2c77f60-c889-4462-896e-e3f7736f6c8f	3448426	8ea9f798-f8ae-4a75-9875-56c9c57f5a56	3720596	a1fdffbe-d8e5-453a-bc4f-752dc7f40244	3508748	94d55769-39a6-47e3-a236-68d080f162ef	3602201	c9e02873-e778-4366-8b7d-97f72048b883	3541620	9d3e7092-5e64-4c83-b35b-672ff0c315ba	3633797	0ce2e6d2-22a5-47ca-be5e-9e8b7ba07f0f	4171108	f25e72da-c57c-400f-899b-40419dc7208c	3597611	69fdf166-0796-4c43-88eb-7c67d9b0f2f3	3754172	cac4ed57-605f-4178-bb5e-0e89f418d9c7	3508929	255ce372-9012-4174-a457-e7810dc9d6fd	3575444	af99f720-6bf3-4565-9069-5faecf28b96d	4254008	ab3de0a2-a68d-48de-88e8-3b844295b5b7	4744591	483552a4-dc70-4468-92ad-ccb05ea7db3b	4002411	959bcb83-f69b-4a58-a188-e73739af19e6	3644966	ddaeac87-8b38-462c-bccc-ed52c9fd1983	3486381	fd9d668c-dc45-4584-ad6c-3152690c84b8	3676341	8ed5d584-6aec-474d-a596-a54ee3b636a8	3478693	945f1387-9341-44f3-afa5-ccbb5ff8ba4e	3032786	41408ec0-56f8-4bc5-b61c-06435e16ac14	4543887	0bec33d8-be74-4cce-bb47-430a1258df93	3575176	6a84b122-7792-44d6-bfb3-8d39815621e1	3854091	f4cd6143-8643-4ba3-a7c2-fc20ff06862f	3673183	1c0e9541-15c1-4641-b6d3-b90e21088ffd	4277005	629bf54c-1e0e-4c84-be8a-7ffd2b0f5f15	4693506	fe4bab62-e9c1-4cdc-9cae-fc7feba882a0	3851799	24d4b92d-8fcd-43f1-8b67-7eef638ec234	4071419	7da6ab4c-d88c-4f03-bc50-cc3eceb3f96c	3619221	e1727871-9b29-4927-b41e-99e3e97b7c61	3511999	25eaceb1-02ca-4309-a5e6-964df0b14c6a	3721696	d082c08f-70b2-43c7-a792-ed7055ef5454	3516853	4016a0bd-1afc-4ea6-8ff6-e99922ccdcab	3767056	bc509290-78f2-4633-9717-6c9c99092ac7	4244427	ca54c9ed-6120-49e0-a588-72fa80b19d30	3593749	deba0bbf-df96-490b-83f3-858fe52a7e40	3923559	c62397a7-e7e0-4816-bbcb-a2380002e3c5	5472497	bfbc4b1e-a275-478f-9c6c-80e30a1ccf7a	3518807	56ce675c-4ac2-432d-985f-c2f39f5ce4ae	3654869	750368f6-2e3e-4888-b0f2-86c098e278b5	3686635	58cd5041-72d5-4979-8d19-b60b7528f48b	3426925	a78fa4f1-b6dc-4bcc-a2d8-14146cef43ea	3726066	29345689-4173-4e46-8d49-ee98dfc354dd	3428811	75f0018e-680e-4fd5-9509-596fe84be509	3494182	d9e908d7-183e-43e3-88cd-c3b743ee8aaf	3609992	9001d861-53a8-48d6-950d-762d62040234	4192761	4e5d5ed5-b1d3-457d-ad2e-cb7b452d6dc5	3527958	aff36edb-e5af-470f-a989-d346b7466cc2	6449611	14ca3a9e-1643-4064-96ee-978c69f6cad7	3815525	8eab6035-0c5a-41a8-98c7-7331fd8926af	3922082	294ec53d-3305-402c-934c-33f110d9f5fa	3570307	fe56fffa-84d7-46b5-998c-396582408ca7	3759011	b8301638-a503-436d-a194-bc54bcb48fea	3570835	9d6d73f6-1c7a-4df5-8021-d348de2b9b0a	3708846	2f2885a2-a11a-413b-a7e6-7554c75d0456	3525592	ae186030-497d-40a4-9a2e-9ad3a5c68f48	3771905	8232d053-f3af-4de1-afb4-ae8fa945084f	3601629	f1d627a5-4a03-46c3-8478-16e400a57a85	3980606	17329f95-f454-4529-ad32-348eca37199e	3627227	ee06ba96-d1c3-4758-a0a0-286cd126b756	5742972	dcc076cb-df3d-457d-a8b6-2b89c6d876a2	3622231	03472500-d2ef-4f7b-a412-724c107762b3	3729517	aeaa8bc9-1b04-4af5-a946-22b90832f09d	4056535	fa0b1c12-6404-4c95-91d0-2b2b2ce08eb0	3303035	3fd99245-9146-4832-a496-2da3a3003074	3677138	9797f21e-ab60-485f-ae26-4ddce24cf0af	3625209	02ab0df0-d69b-49b6-a330-1ed11120478b	3679748	53a6f6c2-8cc4-4f91-8c43-4a647307c161	3556234	9d37503f-9e75-4695-a4d1-2b82808233c2	3619451	23404bed-82cd-4513-9bca-afc970ddd5c9	3586921	2d01e90a-44b2-451b-849f-bb9b2717c238	3706026	11cb8a2f-12fd-4c80-8855-ff0de8e26be5	3867519	b8f86d31-324e-4e8e-bcde-7d23611db80d	3755091	924815bc-4821-4c38-a9aa-2333ee73a8ac	3842077	13f0d456-94b9-4b8a-972c-f6bfaca58e70	3507218	27dcc361-69c5-47fd-b9a5-8a68942efcb2	3604742	a6985931-bf29-4797-9ad7-9cbc14d91db6	3889465	79f5faa8-16ff-4d9c-94af-9d0f44d6f849	3743780	c0d2e4c9-f22a-4134-bee1-1a65c5634019	3976172	897cc9d9-c6f2-46b5-a576-3acb7282a87d	3505371	2b9bb667-ec33-4f21-9ce6-a369b6e8bb14	3474958	4cf01012-2dd2-481b-8b6e-2ee92f5634fd	3865505	e112a780-82bd-42b5-ac4d-1ad526a63117	3670759	4e8d6f2b-c8ae-4836-8b47-e9ceb980a0a2	3517062	50a46d4e-a1e0-4dc4-89ab-f1d913e66e41	3641495	baf7196a-bcb7-4e6e-8734-2d0412f9eb46	3551924	b9b9798b-6a97-4f89-86c1-0a93a6deaec4	4294387	dec48097-71d9-4aee-ae51-6f247454cf64	3578945	31c43da1-dfa8-4336-8581-9475bc0de744	3816836	79fb9177-ebe1-4566-8c4f-6878dab13a11	3842179	f886ba74-ede0-4f69-82c9-9057799f1581	3347046	c2d428fe-ec98-4e48-aaef-21ff89fda7d2	3639095	2ead5673-5838-47d5-ab8b-806d985231ae	3589717	761755f8-3174-41c7-8f5d-48a1fb78752e	4812454	44b44d9c-e601-42f5-8fe4-c8aad85a6a4d	4102736	92f8124e-9e15-4803-b013-0d9920f63334	3598472	00ab1518-b045-468e-8dcd-e0531ef191fd	3342236	3fdc9876-0dda-42ab-8c9e-836b2ed6db4c	3747626	cdaa540f-a3c6-41b8-b0e5-14341d56c151	3690032	834991be-f78d-48d0-b4a1-ac53c2a12ab6	3713989	aaee9e96-f689-4bcd-9eee-dc09122681c3	3617036	94538168-ec7b-41f5-8f50-feb515e359e7	3610237	c20b4f74-f07a-4fa6-aa3d-3a8a01f33852	3703254	5b913db7-e256-4766-82c6-62aa7ced420c	3870739	856977e6-5504-470f-b770-8d4007706e08	6163569	09604ded-8a1e-4c9f-a4ac-8cb07d2f56f6	3795748	4328bc41-258e-45fa-b257-3f0e2898ad8c	3763190	394d1a4f-86d5-47af-8996-30522d2360f5	3624895	53a7f149-d268-4186-8c4d-2374adef8c91	3505786	29108855-bd45-4f3e-9ee7-c5f1ca13ea1a	3689049	e64bd9a4-41a0-4857-8742-fedae43cfb28	4063964	9b1d3f92-1356-4e19-912f-badebb6568d9	3583079	3174aa46-dc11-4e52-9ef5-fb1e5425dd53	3777013	b0565c72-b932-4736-b796-672c2745296f	4195841	89835c6c-ca8e-4187-8ba7-be965dc89f92	3308192	3c72b550-8af1-44be-915c-e47bd636d59e	3840165	a2f95bb7-1c27-4de0-b130-cd68356606cd	3743795	4afe4f94-dd95-495d-9e6b-3d3e73ae1c8b	3730163	3434e1cd-fe17-43d2-9c91-e432f4bb3dbc	3390617	59fd00fc-ce59-4b63-bb8b-49d323fde862	4058715	421405f2-5447-42c7-b22a-2bc6967f966f	3996443	1ec6f07b-6c7a-47bb-9c5e-df38f4a5c886	3924624	f98d18da-fcf4-4f8a-bcd8-343825930594	3820394	cef40f8c-9c50-4f51-820b-f00f0733011c	3949264	2fea7a88-464b-4974-a108-b5c10811c676	3925113	5d358bcf-9cda-4da8-8d8d-904618f4be87	3746351	52fecb21-1e5d-4ece-bc41-ab0216529873	3694070	1d48a723-991c-4fe6-a8b7-a7701197b7e9	7544279	340bbfe3-57d4-4e07-bff1-8be052558ee8	3884142	2f14810a-e567-4a22-b891-6832a189b82f	3152628	3e77f848-f0dc-4081-8c01-45fa161f5d98	3767731	5b0464e5-87c1-4dbf-9dc1-3a0b2af82bda	3950129	ab2523c4-2662-479f-916d-3ac74bdd70e8	3919922	35ec42bc-fcfc-460b-9654-2de6c4bc1311	3719917	66b72d13-528b-4ca1-aa6e-5a7f239d7ae0	3674606	95a583c3-f648-4b4a-92b0-31366edb9bb2	3367242	4739d4fc-af95-49a1-aa05-fcf41d262e16	3693860	24faf111-5221-46ce-ad89-8d926ed0c2db	3409372	e8d08f46-31b0-4525-b379-c218f8bba4dd	2960189	1b2ba80f-0953-4f26-8b84-053c0c1c5683	5406505	114174b0-585b-4844-93b8-e76d5b15a09f	3476508	dbb68b39-f7d7-48af-9751-5c800eb75c22	3556034	8933e462-2998-4a7b-af53-bef2f829bb75	3931722	386bab47-1d07-49dc-9c92-8172d8cd737e	3882661	69460a88-e86b-483f-8ce5-afc54f238113	3829758	c85b9f46-6f01-42d4-8173-16f22e17f8b5	3429505	28b8a747-1db6-4482-930b-54f26f9f0f66	3576011	cbb75dce-cd17-4c41-9dab-4405f42a499a	3638069	bb13a867-d3e3-4dcb-bd4e-c638764b11f2	5520072	28a63f89-be2b-407a-a958-32447caca921	5530107	24c40d0b-a8fe-4f88-bbed-85e51211d1c0	3634501	b09550d8-3ddc-45f5-990c-d85fa5c43f4c	3823355	ef9679fc-ca9e-44d3-8c00-b77a16812ae2	3731272	8bd503cc-c204-49a4-9d24-35459d4d2210	3912316	71d785cf-0ae1-4883-ae68-0fef63b4667f	3656325	67ca8053-baaa-4fc4-aaf0-49ebc786c809	3506300	7ce0ae6e-0bf0-465e-99a0-28cc04828b66	3993338	33454759-6223-4e2e-8573-259d6b9cfcf1	3637600	3431c72c-59de-4656-b1fd-df2815775049	3655646	392ff569-57eb-4a79-9078-dd29cd6dcaa9	3832276	668901ad-9501-4bba-b699-bf6f9a7f754e	3396624	eedf8845-a0fa-48ee-ac02-ff3edd27d58d	3381100	857c217f-2df4-4cb6-bd1f-cad6e6b80246	3436070	5351a093-d6b4-4799-9055-fa5a7cc14cea	3541537	3c3fedc7-317f-4949-a494-885cd016d17b	3535818	4bff8f08-68f2-4619-8f4e-8ec371a02bcf	3827002	d605eb04-0712-4d4a-98df-db946effc2d1	3593075	3200d99e-bfc9-413a-a2f7-c8f0bf054dc9	3574540	dd38bfef-3cde-486f-b816-cfdbcb4990cc	3622060	20f2e1c6-a87a-4fb6-bc92-52a0f6aa06af	3811038	09531c06-0a8e-4d30-9dee-90be8e52d17a	3649286	888ae669-1c05-42c1-9e18-9d533ef339c1	5251377	6d7e6760-43d8-434a-a138-95376b9fff2c	3767692	be2add3c-e2d3-4d68-9582-3959deb591ea	3669640	8cbf55b9-e4a5-4c01-a2ac-98e4d9cc1d20	3422740	274a6d12-e26d-47bb-8158-56c0f0da88ed	3591536	e626c1c4-542a-4838-b99f-6f02c6c47102	4128040	9f028c7f-53ce-4956-8b38-bd597300414f	3517801	2a52851e-f6f3-4a5b-bbc4-5f01d3231bb9	3870622	0f52a366-364e-4d6b-935d-bf3dcafa6e5e	3537939	c1fdabbb-2c37-4c72-bc54-24caa03b2df0	3747935	ff34a6af-201e-4f4e-a946-900218831eda	4086171	4b4a6a2a-d707-452b-99bf-94521308b303	4654741	fd59d50b-25d4-4afd-880c-ebeced6eade0	3809679	8cdd8722-6633-4e07-8711-3fa017690592	3461858	603e9c8d-8be2-4c90-8b2b-74b82b335915	3721232	ba0f79bb-c484-4b21-8543-e5a4f98584e9	3564369	eaeeebfa-e70e-47b7-89e8-e2216a4ad149	3634354	ffd6b86d-1df9-451c-9afb-d350a4a53466	3503861	a8530b63-521b-4c0a-a4c7-fb38516b1eb6	3575825	ab0ea03a-9418-4383-a5a9-02c8676fba92	3529679	54d56c14-cec9-4d02-93a7-7e51bd0669ae	3550893	1024b04d-7054-44bb-88a6-63d3ff2efe28	3767809	ed280800-16b8-49c0-bdab-5b65015fd59a	3627090	11fbb1c2-e3d2-4265-bd56-573cd64b7016	3467133	c5125416-36d9-4b23-a477-26d25b2bc5f4	6337428	63b0dc5f-9c4f-493d-b086-5a8db55f26aa	3546777	2bf8e773-1369-40f9-a8ad-ccbb6d0d8ad4	3798457	414965d9-f072-4635-a09d-294231a9deb3	3670270	38735334-5d3a-45ee-bf3c-f5ab2dfeac71	3687451	797799b1-2768-4c01-8fff-246ee708da55	3443230	6f487e55-e60f-4633-8ec0-3f278fcd4790	3654032	72f55d2f-e816-43c4-a515-af4afbb9a111	3717796	c325fb5d-60a0-4c4d-a4c8-8a6dad42bc52	3994160	99f28b56-0e9c-4388-9879-adc346ce77d7	3836480	c9e9ccba-1f92-49ea-bd70-b33fec7facaf	4003174	f5ff6e4f-7d20-4185-a3db-8f5cb2c7a436	4907369	a70c516d-e965-498c-a1b1-5344202ce4d0	3307042	1b6f7004-44b9-4fca-8d8c-35244dd1cd51	3575860	98cf9fad-2fe1-47a4-adb3-9047749459bf	3533677	d6ff9cdd-fe28-48bc-a9f4-412f9bd4eb8b	3556362	c0d28990-f68f-4100-bdc6-23d27cdc56de	3427447	8005f9c1-f5e2-4acd-9121-8884b40aeea0	3744142	444e6b22-4629-499d-8722-96ae255aebfb	3325480	82397bfe-418c-4501-be3a-9d4f5c32ae05	3779887	7a6b56fa-cc01-4f1a-9576-716efc1338ea	3845273	cc14946d-30cc-418c-8caf-7b23af0aeae0	3694798	9c8559d4-b479-4ff5-9c62-bf510baf3fb7	4071375	cc5692af-65f1-449a-ac31-22e6f7e2f1f3	5854592	a4921826-71f0-4a06-b5c6-a854e836cc1f	3411561	af38172f-36a2-48c2-81f3-302042abcf53	3847707	2301e458-36c7-4d3b-88fa-38821ac90cd7	3538986	7951c780-57f5-4984-bacc-7f43cb7baad7	3808042	86610097-0a39-4982-954c-bc1fc8199943	3601936	4e675982-72cf-4adc-ae26-280aa2343ae3	3755589	a8aa0253-4345-468f-bc65-14b22f929201	3849321	163d110a-e5c8-47eb-85ce-03f496177630	3736747	b04378e2-8cb2-441a-aa79-33aa8ff48881	3617867	79c0056e-b5c9-44e5-9bff-de8d8bf7519d	3441568	85f28d8b-7496-49e4-a207-70ae9c375e49	3465099	32f3c67a-7d90-4d24-a83e-cad1f8df4e4b	5261080	79b215f9-6cad-44b6-af31-81fd117d7bbc	3848685	e4412544-ab4d-459b-9e5c-7be09e364ef3	3272763	7da81183-725d-4961-89c0-7f28e6f5cb62	3711452	dae42469-f6a9-4909-be50-ac2076d8f81b	3390705	e3dffdde-63ba-4080-be67-9a9c937c996e	3657279	e7ee4450-d18c-42e2-96c4-96f96c75862e	4215798	cdcd5b55-6da6-48cd-a637-3f5c8bde202f	3798902	eb6f4c0a-556e-4927-8a0f-f94934fffe92	3526481	3212a342-456a-4a5f-bff6-d9c7ebf9ed83	3231480	b1887eef-9faa-428c-a7c1-33b56fa865f1	3745813	b90d76c9-3474-4186-a782-f4f3348a0a0d	5813021	aeb577e5-11e8-46ef-af5a-082d9b30566e	3844193	a00b9a0a-7fc5-4ea5-aefd-14b1472879a4	3738917	58d4bdaf-2b34-48f4-84d3-339cc7648853	3582615	d025c3fd-6add-48df-a2ae-094f28cd249a	3432262	f074714f-da63-4bbc-9fb2-c2e99f434025	3433860	2ccac291-13b0-40ad-ae31-ebaf175903da	3603589	10068171-237a-4ae3-8cdc-0f45e0599261	3702917	a705126a-585e-4ce4-b7ab-55fc908b1d47	3647933	5b01ff18-3c16-4358-8ed5-d4bd54f35018	4333418	9abdf556-1009-4f3b-b02a-4f413b90436c	3751953	f4f82a56-7cdb-4f44-8e17-a9be8d6a29c6	3324424	00a30911-0acc-4495-9b0b-6beae5efc4db	3524537	d154bec6-3487-48fb-a35f-7e67fb494155	5374294	a6ef7be3-dcee-4522-8204-3d5909523ce5	3722190	50ffd836-0f74-47d4-989f-03f99d782088	3794507	59df5b75-d889-493c-b4b1-8e02d7a11d9e	3791829	d0415347-797e-4f1d-93a9-7746bf639083	3890844	775133ca-51a9-4aa9-aadc-fb6db4d436ca	3590519	3205df4d-21be-4316-aebe-70a5c324bcad	3614455	01ea95a7-9d7c-41d9-b9e1-b54ce8d1ac6e	3275183	d8a064d9-40b9-472e-9698-e63b6b8be382	3697447	90079a6b-d54c-4668-a71c-9579de243750	3858133	10297f12-658d-49fa-bdea-3661a9ca639d	3920054	27482de5-88f8-44e8-b3e5-ce2c5b77d330	6205811	0926d788-2efe-42ed-a342-cb92055830b7	4127885	24fe8957-8a35-4e62-848a-1033b9e724d5	3809323	6d1784bd-d643-415b-85c1-741aefc6614a	3726902	d6f5ecdb-0773-4073-9490-15bc09453edb	4273310	053c7c97-dada-4829-82a0-4edd897cf5b7	3635542	d2da1a72-2e07-46d5-a8cd-56666a939106	3486240	d1b1a9dc-0950-41e3-979b-28913084e961	3131287	1264dd00-715d-426b-b376-dd168a8676d1	3479186	02b9f3a2-52d2-4ab2-a66c-4e0ddcf5373a	4204992	0b08d953-95bc-48b2-bf5e-a8f974efd1ac	3626137	927a2eec-027b-4433-b485-a57046268dd7	3595832	752e028b-b219-4c0b-8796-8c69f44e9c7b	4984379	b50f4f65-50a7-490c-b01e-2b0af5b86437	3172512	c7613cb4-c0da-41d6-a64c-8970efd18144	3621684	5db2fbea-fc9d-41b3-aeef-3d3a89fc25df	4134835	e9a375cc-2ed1-4967-9916-ea4553a17214	3680515	894cb67e-c8bd-4bbe-9a6f-cdfef295d628	3703019	e77964fd-096c-4121-8307-70d1e4a162f6	3453432	b4ac1ecc-8f05-4d36-a67c-698e746ca9cd	3631666	954e9188-f186-42ad-b584-5cbcc98d221a	3526545	7ecf60ca-d161-4f4d-90c2-587d18941d44	3136336	b7ca38ac-b769-431c-b140-9aaf5ff02aeb	3846583	4153280c-4783-448a-88b1-f868a7ba7ec7	5479634	c671b815-fc86-46fd-bd64-4ff5cd6fa336	3598574	ca70f697-18f7-4119-831a-06f6c1eb6456	3656902	53f36a93-74da-436c-aa93-45d88f17b9b1	3576935	efb5049a-e302-4e38-8e5f-cc85463e8e11	3671937	875afbee-f013-4e5d-9800-109c4c311f1c	4408912	74fade38-1c4b-43f9-9083-d4e069922b8e	3502135	d35a3c80-5ec6-4e97-a11d-4b8ccd8792f9	3533247	ab1a9710-be73-4c61-9fdf-d7572e0900b5	3839466	d8c39476-adb1-46b8-948d-011ed852a9e7	3578695	1c915805-0f73-4c39-8260-01a91f4c3eeb	3520910	92beae65-5883-4161-ad43-f2ea13aedb06	3829515	48c078b9-41c6-4e9c-b4e7-49b4f3280b05	3264210	49d58b41-9829-475f-b6c7-7391b7b0996c	3541019	089cd9aa-b0e3-4d06-ac70-c8297586e479	3410520	1ce7cf61-99a4-49ec-b075-b3e48a9f0aad	3477896	2e2c298d-c52e-4cd7-a4d9-7afe4be7a37a	4071829	5c428f26-0b22-4dde-b519-6f42ed35f931	3179453	6198fb72-d4b7-46f2-9a8f-3aab64056438	3644418	06e49bca-99ea-4f4b-9413-6df940c0b6ff	4021650	32af56de-1e3a-480a-ba35-1c79647b7622	3776133	758165e2-9c80-412a-962f-c0afb3adb708	3799458	4e8da0b9-0af7-4c8b-8107-21d764a17812	3517253	775dc734-1035-4cdb-9351-337ab7e0e8d3	5295681	136a3b3f-c42e-4139-884d-79d719248ffc	3562179	6ce9065d-5666-4b45-b025-8b36470d2b06	3954001	37e7daa7-c719-4c44-91fa-82cb2790dc80	3775009	806a0b8e-89b9-4c56-981b-994246b41690	3753179	1fc36902-8e51-4fc7-bf22-b1c81d031fbd	3882133	24ae941c-ff2e-4277-b05a-599767e02592	4012763	c9bf2d90-abea-47f0-8169-f66a9c568f3d	3669538	54fbb324-7fa9-4133-b3d4-50e165fb9c87	4792507	608e5242-5d38-441b-8e2e-53f088468c74	3679260	75459f28-1622-4391-a962-46f6395dbe44	3614738	5db4b96e-e006-40a6-b5e9-fc0c98e6cc69	4947079	1a545dd7-0b9d-487f-8bff-7794dd887b36	3519668	4275a292-50b6-4dec-b2ad-886ef76111df	3885217	9779adaa-95e8-4595-a7d1-c17b90a7d9da	3643562	b9aa3f37-3638-4f23-80f4-fe26e9928867	3863770	90eebda3-adbc-40cd-b84d-e230c4912a42	3584600	a4cfb299-febf-45c9-8503-ba2a76731d69	5009229	1bfb842e-7d0b-4fe2-bcd4-7c19163a5130	4227388	f593e027-4805-4c4d-ad3f-aad2e24d39b8	3647878	4a8a634d-48aa-424b-8e40-61f4c8774bcc	3573968	d29866dd-c9c6-4df6-aafa-d004f7f56472	3497594	12ebc79a-51a9-4f22-8544-431a955d77a0	5565871	9ab40f4a-84b8-40ab-96c6-a8523f82b2f8	3484519	9781a339-6d1c-4b83-ad17-1af8d3c52bf6	4145231	d38d9d90-67cb-434e-888b-7d979cd6b3e4	3910386	ef436d17-7e87-454e-b857-f07f16b53dbc	3934175	3d7f56c4-82aa-450a-b149-485d9096e51b	3640088	098dd767-1c78-4af6-a739-73f512707f54	3631621	e7a043df-8ae6-4eb4-814a-40f2db6c56e9	3542808	ce0d2543-f6df-4a1b-9f98-ea8e912a9388	3766245	311e9e48-2941-48ec-9762-3a0771c8ee41	3552500	95ba9e02-0bea-49cc-9689-54825a5817bc	3550736	61e1ecc8-8d2a-4f56-95f8-423d64815e66	3678575	963b3d63-4ced-4db3-8ee0-906a5ec23a77	4116964	1cdff108-98f2-44ea-9ad6-b7ae3ad8b272	3801610	9d79cf19-c988-4926-b7f0-a22c0de4e0b5	
";
            string[] sections = ss.Split('\t');
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < sections.Length; i += 2)
            {
                builder.AppendFormat("{0}\t", sections[i]);
            }

            File.WriteAllText("D:\\1.txt", builder.ToString());
        }

        private static void TestNode()
        {
            List<string> result = new List<string>();

            foreach( var line in File.ReadLines(@"C:\Users\liagao\Desktop\Sumup\result_refinedquery.txt"))
            {
                string[] sections = line.Split('\t');
                string nodes = string.Empty;
                for (int i = 1; i < sections.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(sections[i]))
                    {
                        nodes += long.Parse(sections[i].Substring(0, sections[i].IndexOf('(')));
                    }
                }

                result.Add(nodes);
            }

            HashSet<string> set = new HashSet<string>();
            Dictionary<string, int> resultDictionary = new Dictionary<string, int>();
            foreach (var item in result)
            {
                if (!set.Contains(item))
                {
                    set.Add(item);
                    resultDictionary.Add(item, 1);
                }
                else
                {
                    resultDictionary[item] += 1;
                }
            }

            foreach (var item in resultDictionary.OrderBy(item=> item.Value))
            {
                Console.WriteLine(item.Key.Substring(0, 20) + @" ==> " + item.Value);
            }
        }

        private static void TestStreamWriter()
        {
            using (StreamWriter writer = new StreamWriter("D:\\1.txt"))
            {
                while (true)
                {
                    writer.WriteLine("111");
                }
            }
        }

        private static void TestToStringFormat()
        {
            long ss = 111111111;
            double ss1 = ss / (double)10000;
            Console.WriteLine(ss1.ToString("f2"));
        }

        private static void TestEncode()
        {
            //string url = "https://stackoverflow/search?q=" + System.Web.HttpUtility.UrlEncode("QTest: Running a specific test from a Test dll ");
            /*string url = "https://stackoverflow/search?q=QTest%3A+Running+a+specific+test+from+a+Test+dll+";
            //NetworkCredential myCred = new NetworkCredential(SecurelyStoredUserName, SecurelyStoredPassword, SecurelyStoredDomain);
            var request = HttpWebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();

            using (var streamReader = new StreamReader(response.GetResponseStream()??new MemoryStream()))
            {
                var xml = streamReader.ReadToEnd();
                File.WriteAllText("D:\\1.txt", xml);
            }*/
            using (var client = new CookieAwareWebClient())
            {
                /*var values = new NameValueCollection
                {
                    { "username", "liagao" },
                    { "password", "loveyaya12345:)" },
                    { "domain", "fareast"},
                };
                client.UploadValues("http://login.microsoftonline.com", values);

                // If the previous call succeeded we now have a valid authentication cookie
                // so we could download the protected page
                //client.Credentials = new NetworkCredential("liagao", "loveyaya12345:)", "fareast");
                //string result = client.DownloadString("https://stackoverflow/search?q=QTest%3A+Running+a+specific+test+from+a+Test+dll"); 
                var result = StackOverflowSearchEngine.GetSearchResult("Using same XAP plugin twice with different configuration");
                foreach (var searchResultItem in result)
                {
                    Console.WriteLine(searchResultItem.ScrapeUrl + " ==> " + searchResultItem.TiTle);
                }*/
            }
        }

        public class CookieAwareWebClient : WebClient
        {
            public CookieAwareWebClient()
            {
                CookieContainer = new CookieContainer();
            }
            public CookieContainer CookieContainer { get; private set; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = (HttpWebRequest)base.GetWebRequest(address);
                request.CookieContainer = CookieContainer;
                return request;
            }
        }

        private static void TestList()
        {
            int[] array = new int[100];
            List<int> list = new List<int>(array);
            list[5] = 3;

            List<int> list2 = new List<int>(array);
            //Console.WriteLine(list2[5]);

            List<int> ss = new List<int> {1,2,3,4,5,6,7};
            Console.WriteLine(ss.Where(o => o > 3).Count());
        }

        private static void TestSplit()
        {
            string s = "a.b.c.d";
            Console.WriteLine(s.Split(new [] {'.'})[0]);
        }

        private static void TestGetFullPath()
        {
            Console.WriteLine(Path.GetFullPath("123"));
            Console.WriteLine(Path.GetFullPath("D:\\123\\4123"));
        }

        private static void TestQpsTimer(int qps)
        {
            Timer timer = new Timer(state=>
            {
                Console.WriteLine(DateTime.Now.Millisecond);
                Thread.Sleep(10000);
            });

            timer.Change(0, 1000 / qps);
        }

        private static void TestDateTime()
        {
            //var time3 = DateTime.Parse("2000-01-01 00:00:00").ToUniversalTime();

            //var time = DateTime.ParseExact("20000101000000-07", "yyyyMMddHHmmsszz",
                                           //CultureInfo.InvariantCulture);
            Console.WriteLine(new DateTime(636072655558721039));
            var time1 = DateTime.Parse("Wed, 2 Jan 2008 07:59:40", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            var time2 = DateTime.Parse("Wed, 2 Jan 2008 07:59:40");
            
            //Console.WriteLine(time3.CompareTo(DateTime.Parse("2000-01-01 00:00:00")));
            //Console.WriteLine(time.ToUniversalTime().Kind);
            Console.WriteLine(time2);
            Console.WriteLine(time2.ToLocalTime());
            Console.WriteLine(time2.ToUniversalTime());
            //Console.WriteLine(time2.ToUniversalTime().CompareTo(time2));
        }

        private static void TestMemoryStream()
        {
            string s = "1234567890";
            var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(s));
            using (var stream2 = new MemoryStream())
            {
                stream1.WriteTo(stream2);
                stream2.Position = 0;
                stream1.Close();

                Console.WriteLine(new StreamReader(stream2).ReadToEnd());
            }
        }

        private static void TestTask()
        {
            List<Task> tasklist = new List<Task>();
            foreach (var i in GetList())
            {
                tasklist.Add(Task.Factory.StartNew(() => { Thread.Sleep(3000); Console.WriteLine(i); }));
            }

            foreach (var task in tasklist)
            {
                task.Wait();
            }
            Console.WriteLine("{0, 10}", 67.1);
        }

        public static IEnumerable<int> GetList()
        {
            for (int j = 0; j < 5; j++)
            {
                if (j == 4)
                {
                    Thread.Sleep(3000);
                }
                yield return 1;
            }
        } 
    }

    public class SellerTypeIdentifyComparer : IEqualityComparer<List<long>>
    {
        // SellerType are equal if their names and product numbers are equal.
        public bool Equals(List<long> x, List<long> y)
        {
            return x.Count == y.Count && x.Except(y).Any() && y.Except(x).Any();
        }
        public int GetHashCode(List<long> product)
        {
            return product.Count;
        }
    }
    delegate DateTime GetTime();

    [DataContract]
    public enum FgStatus
    {
        [EnumMemberAttribute]
        Success = 0,
        [EnumMemberAttribute]
        Failed = 1,
        [EnumMemberAttribute]
        Running = 2,
    }

    [DataContract]
    public enum FgStage
    {
        [EnumMemberAttribute]
        LeaseAndDeploy = 0,
        [EnumMemberAttribute]
        RunningXping = 1,
        [EnumMemberAttribute]
        RecurringAnalysis = 2,
    }

    [DataContract]
    public class SubmitJobRunStatus
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public string Message { get; set; }
    }

    [DataContract]
    public class FunctionalGateStatus
    {
        [DataMember]
        public string ExperimentName { get; set; }

        [DataMember]
        public string ExperimentVersion { get; set; }

        [DataMember]
        public int BuildId { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public FgStatus Status { get; set; }

        [DataMember]
        public FgStage Stage { get; set; }

        [DataMember]
        public int TotalCases { get; set; }

        [DataMember]
        public int PassedCases { get; set; }

        [DataMember]
        public int FailedCases { get; set; }

        [DataMember]
        public int AnalysisTimes { get; set; }

        // DataPlatform analysis result url path
        [DataMember]
        public string LastAnalysisResult { get; set; }
    }


    [Serializable]
    class TestObject
    {
        public GetTime getTime;
        public string id;
        public string name;

        public string value;

        public string version;


        public void Start()
        {
            this.getTime();
        }
    }
}
