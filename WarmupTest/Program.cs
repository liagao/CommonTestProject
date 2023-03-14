namespace ConsoleApp3
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class WarmupQueryInfo
    {
        public string QueryName { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public bool IsLimitedWarmupQuery { get; set; } = false;
    }

    public class PluginWarmupInfo
    {
        public string PluginName { get; set; } = string.Empty;
        public string PluginVersion { get; set; } = string.Empty;
        public ConcurrentBag<string> WarmupFileList { get; set; } = new ConcurrentBag<string>();
        public ConcurrentBag<string> SerializationExceptionList { get; set; } = new ConcurrentBag<string>();
        public ConcurrentBag<string> ExecutionExceptionList { get; set; } = new ConcurrentBag<string>();
    }

    class Program
    {
        static Regex BeginWarmupRegex = new Regex(@".*?\((.*?)\).*?ApplicationHost BeginWarmupQuery.*?queryFile=\"".*\\(.*?aqm|.*?ahrequest).*?traceId=(\S*?)\s+?workflow=\""(.*?)\""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static Regex WarmupExceptionRegex = new Regex(@".*?ApplicationHost PluginWarmupExcecutionException.*?pluginName=\""(.*?)\""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static Regex ExceptionRegex = new Regex(@".*?\((.*?)\).*?ApplicationHost Exception.*?type=\""(.*?)\""", RegexOptions.Compiled);
        static string[] limitedWarmupQueries = {"BFPR_1.aqm",
"ABNAnswer_ABNAnswerSingleEntityReQueryResponse.aqm",
"ABNAnswer_MultiEntityScenario.aqm",
"Local_GoBigV2_Flight_FRFR.aqm",
"Halsey.Proactive.WorkflowZhCN.aqm",
"BingMapsFirstPage(san_francisco).aqm",
"Ads.PaidSearchForNative.PaidSearchForNativeMainV2.aqm",
"Xap.BingFirstPageResults(QueryAnswersHermes).aqm",
"AssistantRegional.BingFirstPageResults_zhcn_beijingCarlimit.aqm",
"Flighted-Halsey.Proactive.Workflow01.aqm",
"Assistant.BingFirstPageResults(SessionUpdater_QASSerializer).aqm",
"Local.LpaClients.ProactiveWorkflowWithThemes.aqm",
"Assistant.BingFirstPageResults.TentativeRequest.Weather.aqm",
"BootstrapCommuteCardReplace102017.aqm",
"Xap.BingFirstPageResuls(AppInstallAd).aqm",
"MultimediaImageKnowledge_InIndexUrlWithOnline.aqm",
"Xap.BingFirstPageResults(LEX_CEX_MEX_Auction).aqm",
"EntityIdLookupAnswer-VideoVertical-LadyGagaPokerFace.aqm",
"Asst.Bfpr.WMod.sunlight.whitelist(sunrise).aqm",
"CardReplace.BestNearby.Accept.aqm",
"HomepageModules.Proactive.Workflow.WithNewsPrefences.aqm",
"Asst.BFPR.WMod.FactValue(what is the wind direction today).aqm",
"Asst.BFPR.FinanceWF.EarningReport_2.aqm",
"Lists.WarmupTLWorkflow.aqm",
"Assistant.BingFirstPageResults_en_us_HNF_Calories_In_Banana.aqm",
"ABNAnswer_Searchbox.aqm",
"FavoritesOldReadAndNewWarmup.aqm",
"Halsey_Proactive_Workflow_RichPackageTrackingV2_1_Debug.aqm",
"Halsey.Proactive.WorkflowZhCN.aqm",
"MultimediaImageInsights_Annotations_ClassifyRequest.aqm",
"Recommendations.Windows.Apps(GetSuggestionsRequest_WinStore_PicksForYou).aqm",
"Assistant.BingFirstPageResults_en_us_Sports_liverpool_game.aqm",
"Widget.Insights.Regional.Reactive.BilingualDictionary.aqm",
"Xap.BingFirstPageResults.actors_in_movies_directed_by_james_cameron_qpv3_flight.aqm",
"Widget.Insights.Proactive.DisambiguationSingleEntity.aqm",
"L2ItineraryCard_En_Us.aqm",
"WeatherDesktopContextRegionSeattleWeather.aqm",
"mobileGesture.aqm",
"RecallListQF.aqm",
"Xap_BingImageResults_Notifications.aqm",
"UniversalSearchBox.Windows10M2.Suggestions-1 kg to g.aqm",
"LGAlphaAnswer.Assistant.BingFirstPageResults.1_meter_to_centimeter.aqm",
"Asst.BFPR.ModMT.Weather.SuitableFor(WearJacketThereTomorrow).aqm",
"PersonalSearch_query.aqm",
"Local_GoBigV2_Flight_gobigwpo.aqm",
"Xap.BingFirstPageResults(StableId).aqm",
"Widget.Insights.Proactive.SnappMultipleDisambiguation.aqm",
"HouseSegment.BingFirstPageResult.aqm",
"UniversalSearchBox.Windows10M2.Suggestions-send email to debs .aqm",
"PersonalReactiveAnswer_Finance_Cortana_enus_1.aqm",
"UniversalSearchBox.Windows10M2.Suggestions-tom cruise height.aqm",
"Local.RetrieveContactsGrammar(schedule_meeting_with_andrey).aqm",
"InformationDiscovery_1.aqm",
"Flighted-Halsey.Proactive.Workflow02.aqm",
"Xap.API(pizza).aqm",
"Retail.EntityCategoryPage(PaginationOverrideCacheKey).aqm",
"Dates.Assistant.BingFirstPageResults.when_is_mothers_day.aqm",
"Dates.Xap.BingFirstPageResults.easter_is_when.aqm",
"WPO.BingFirstPageResults_en_gb_Local_GoBigV2.aqm",
"BingFirstPageResults_en-us_directionanswer_seattle_to_portland.aqm",
"UQU.ABFPR.playhavana.uqun_enableuqu_cortexdialog1p.aqm",
"EntityIdLookUpAnswer.PreWebEntityLookUpWorkflow(houses_for_sale_in_bellevue).aqm",
"UQU.ABFPR.debug_debug_rewrite_rewrite.uqun_enableuqu_cortexdialog1p.aqm",
"UQU.ABFPR.i_have_a_headache.uqun_enableuqu.aqm",
"UQU.ABFPR.is_fortnight_on_ps4.uqun_enableuqu_cortexdialog1p.withBingIdToken.aqm",
"UQU.ABFPR.what_is_the_weather_in_seattle.uqun_enableuqu_cortexdialog1p.aqm",
"UQU.ABFPR.what_time_is_it_in_seattle.uqun_enableuqu_cortexdialog1p.aqm",
"Xap.BingFirstPageResults_en_us_bingpage_waterfall.aqm",
"LEREU.Hotel1000EUV2.aqm",
"WPO.BingFirstPageResults_en_us_RecipeScaleContent.aqm",
"Xap.BingFirstPageResults_en_us_Recipe_Ingredient_Substitutions.aqm",
"USB.Main_android_app_flight.aqm",
"Xap.BingFirstPageResultsRegional_ja_jp_AdsExternalProviders.YahooJapanProvider_Workflow.aqm",
"ShowtimesMultiturnTitleScenario.aqm",
"ShowtimesMultiturnTitleScenarioOutOfTheater.aqm",
"XapBingFirstPageResults_ShowtimesV3_Theater_MultipleTicketingLinks.aqm",
"XapBingFirstPageResults_ShowtimesV3_Title_MultipleTicketingLinks.aqm",
"Xap.BingFirstPageResults(flowers_57984).aqm",
"Xap.BingFirstPageResults(flowers_57985_2).aqm",
"EntityPlugin.Xap.BingFirstPageResults.eqnasuppression.harry_potter_and_the_philosopher_stone.aqm",
"EntityPlugin.Xap.BingFirstPageResults.eqnasuppression.olympic_peninsula_cities.aqm",
"RetailInsights_DetailInsights_enus_bpdetail.aqm",
"BingFirstPageResults_ImageSkills.aqm",
"UniversalSearchBox.IE12.Suggestions-barack obama-FastEntity_flight.aqm",
"UniversalSearchBox.IE12.Suggestions-trump-FastEntity_flight.aqm",
"Finance.TR.12usdeur.Xap.Bfpr.aqm",
"Finance.TR.FFFHXFund.Xap.Bfpr.aqm",
"Finance.TR.MarketToday.Xap.Bfpr.aqm",
"Finance.TR.MsftQuickPrice.Xap.Bfpr.aqm",
"Finance.TR.MsftStock.Asst.Bfpr.aqm",
"Finance.TR.MsftStock.Xap.Bfpr.aqm",
"Finance.TR.MyStocks.Xap.Bfpr.aqm",
"Finance.TR.SecurityDataAPI.Xap.Bfpr.aqm",
"Finance.TR.SP500Index.Xap.Bfpr.aqm",
"Finance.TR.SpyETF.Xap.Bfpr.aqm",
"Finance.TR.USDCurrency.Xap.Bfpr.aqm",
"Finance.TR.USStockMarketToday.Bfpr.aqm",
"Finance.TR.UTPostWeb.Xap.BFPR.aqm",
"Xap.BingFirstPageResults_en-us_outdoorAttractionCarousel.aqm",
"Ads.PaidSearchForNative.PaidSearchForNativeMainV2[PA+ TA].aqm",
"Ads.PaidSearchForNative.PaidSearchForNativeMainV2[TwoClickMW.DB].aqm",
"EntityIdLookUpAnswer.BingFirstPageResults_bingpage_editing.aqm",
"Xap.BingFirstPageResults_google+jobs.aqm",
"Cricket.BingFirstPageResults_en_gb_GenericSchedule_cricketv3.aqm",
"Xap.BingFirstpageResults(best_smartphones_all_filters_en-in).aqm",
"Cricket.BingFirstPageResults_en_in_GenericSchedule_cktv3.aqm",
"EntityIdLookUpAnswer.BingFirstPageResults_bingpage_force_trigger.aqm",
"LocalDataGroupConfigExp.aqm",
"BFPR.TimeZoneV5.CheckDuration.aqm",
"LocalQnAAnswerWorkflow_SERP.aqm",
"Offers.BingFirstPageResults.GbMarketingCampaignFlight(argos_deals).aqm",
"ConditionHero.ConditionHeroWF_en_us_asthama.aqm",
"EntityAnswerColumnStore_FamouseActors.aqm",
"FactCarouselRanker.Xap.BingFirstPageResults.tom_hanks_his_age.aqm",
"Xap.BingFirstPageResults(AffiliateLocation - Sargento).aqm",
"Xap.BingFirstPageResults(AffiliateLocation - Digiorno).aqm",
"EntityIdLookUpAnswer.BingFirstPageResults_bingpage_editing_3.aqm",
"Xap.BingFirstPageResults(VGSplit-ML-Flowers).aqm",
"Xap.BingFirstPageResults(VGWithML).aqm",
"Xap.BingFirstPageResults(Streamer).aqm",
"EntityIdLookUpAnswer.BingFirstPageResults_bingpage_signed_in_analytics.aqm",
"Xap.BingFirstPageResults_UT_DAAG_MentalHealth_1.aqm",
"Xap.BingFirstPageResults_UT_DAAG_MentalHealth_2.aqm",
"Retail.EntityCategoryPage(OttoRetailSearchStoreFront_DeDe).aqm",
"Retail.EntityCategoryPage(OttoRetailSearchStoreFront).aqm",
"ComplexSymptom_OSFetch.aqm",
"Xap.BingFirstPageResults.UT_SportsContainerNBA.aqm",
"Xap.BingFirstPageResults.AlphaAnswerUT.aqm",
"Xap.BingFirstPageResults_en_us_RTBAdDataWorkflow_ArbiterKnob.aqm",
"Xap.BingFirstPageResults(CamelusPAdjust)warmup.aqm",
"Xap.BingFirstPageResults.UT_FlightBook.aqm",
"Retail.ShopHub.aqm",
"Xap.BingFirstPageResults_en-us_mmeqna_isinpage_best_computer_protection.aqm",
"Xap.BingFirstPageResults_en-us_mmeqna_isinpage_whats_the_differnce.aqm",
"Xap.BingFirstPageResults_en-us_mmeqna_nis_what_bears_eat.aqm",
"Xap.BingFirstPageResults_en-us_mmeqna_nis_what_elephants_eat.aqm",
"CityPlaceAnswerV25_en_us_paris.aqm",
"Xap.BingFirstPageResults_en_us_rtb_popnow_carousal.aqm",
"Ads.PaidSearchForNative.PaidSearchForNativeMainV2_TwoClickAlwaysPresent.aqm",
"Ads.PaidSearchForNative.PaidSearchForNativeMainV2_L15Bidder_AllAdsStack.aqm"};

        static void Main(string[] args)
        {
            //File.WriteAllLines(@"C:\Users\liagao\AppData\Local\Temp\tmp342C.tmp\Warmup2.txt", File.ReadLines(@"C:\Users\liagao\AppData\Local\Temp\tmp342C.tmp\Warmup.txt").Where(o=>o.Contains("BeginWarmupQuery]")));
            //TestRegex();
            //GenerateJittingEventAnalysisResult();
            //GenerateExceptionAnalysisResult();

            /*GC.Collect(2);
            
            if(GC.TryStartNoGCRegion(1000 * 1000 * 1000, 1000 * 1000 * 1000))
            {
                Console.WriteLine("!!!!");

                GC.EndNoGCRegion();
            }
            else
            {
                Console.WriteLine(":(");
            }*/

            TestPluginJitAnalysisResult();

            Console.ReadLine();
        }

        private static void TestPluginJitAnalysisResult()
        {
            //GenerateResult(@"D:\temp\warmup\normal vs new\control\", 5);
            //GenerateResult(@"D:\temp\warmup\normal vs new\morewarmer\", 5);
            //GenerateResult(@"D:\temp\warmup\normal vs new\morewarmer+sync\", 5);
            //GenerateResult(@"D:\temp\warmup\normal vs new\morewarmer+sync+sort\", 5);
            //GenerateResult(@"D:\temp\warmup\normal vs new\sync+sort\", 5);

            GenerateResult(@"D:\temp\2\9\8\", 10);
        }

        private static void GenerateResult(string baseDir, int threshold)
        {
            var baseDri = baseDir;
            var resultDic = new ConcurrentDictionary<Guid, Dictionary<string, int>>();

            // read plugin jit
            Parallel.ForEach(File.ReadAllLines(baseDri + "PluginJit.json"),
                line =>
                {
                    try
                    {
                        var kvp = JsonConvert.DeserializeObject<KeyValuePair<string, List<string>>>(line);
                        var dic = new Dictionary<string, int>();
                        resultDic.TryAdd(new Guid(kvp.Key), dic);
                        foreach (var value in kvp.Value)
                        {
                            string plugin;
                            if (value.Contains('/'))
                            {
                                var parts = value.Split('/');
                                plugin = parts.First();

                                // old format: {"Key":"CCEA1977D20F4891BA2332B32059D89B","Value":["EntityIdLookUpAnswer.RealEstatePriceQueryFilters/18"]}
                                // new format: {"Key":"CCEA1977D20F4891BA2332B32059D89B","Value":["EntityIdLookUpAnswer.RealEstatePriceQueryFilters/18/37521;33534;44567"]}
                                // we'd better support two formats now since during migration, two formats will exist in the same time
                                if (parts.Length < 2 || parts.Length > 3 || !int.TryParse(parts[1], out var latency))
                                {
                                    throw new InvalidDataException($"Data is invalid in file");
                                }

                                if (!dic.ContainsKey(plugin) || dic[plugin] < latency)
                                {
                                    dic[plugin] = latency;
                                }
                            }
                        }
                    }
                    catch { }
                });

            // read query/plugin warmup info
            var pluginWarmerDic = new ConcurrentDictionary<string, PluginWarmupInfo>();
            var queryDic = new ConcurrentDictionary<Guid, WarmupQueryInfo>();

            var fullPath = baseDri + "warmup.txt";

            Parallel.ForEach(File.ReadAllLines(fullPath),
                line =>
                {
                    if(GetEvent(line, out var eventName, out var parameters))
                    {
                        switch(eventName)
                        {
                            //experimentName="Exp in warmup query: SharedProd The Warmuped exp:SharedProd_20210303.19194018.01"
                            //queryFile="D:\\Data\\xap\\WarmupQueries\\Prod\\AWE\\Widget.Insights.Proactive.SnappMultipleDisambiguation.aqm"
                            //queryId=d62dbd49-f741-42f7-91e5-fa121c889722 traceId=f886b011-e820-423c-a654-a96fd617e6a2 workflow="Widget.Insights.BfprWorkflowV2"
                            case "BeginWarmupQuery":
                                var queryFileName = parameters["queryFile"].Split('\\').Last();
                                queryDic.TryAdd(new Guid(parameters["traceId"]), 
                                    new WarmupQueryInfo()
                                    {
                                        QueryName = queryFileName,
                                        IsLimitedWarmupQuery = limitedWarmupQueries.Contains(queryFileName),
                                        WorkflowName = parameters["workflow"]
                                    });
                                break;

                            //PluginWarmupBeginReplayFile]
                            //pluginName="Xap.ObjectStore{SocratesQnABlacklist.OSBkKey_Microsoft.Bing.Xap.Plugins.Void_Microsoft.Bing.Xap.Plugins.Void_SocratesQnABlacklist.OSBkValue}"
                            //pluginVersion="2.1"
                            //filePath="D:\\Data\\ApplicationHostData\\PluginWarmup\\SharedProd\\Xap.ObjectStore{SocratesQnABlacklist.OSBkKey_Microsoft.Bing.Xap.Plugins.Void_Microsoft.Bing.Xap.Plug_2-5e941c61afcc328f\\1\\f1c1ea7fa30247618d80a69db39f8cf0-173.bin"
                            //sourceWarmerName="BFPR_1.aqm" activityId="77698ef3a79b4fe4abd8c2e966f4aeca"
                            case "PluginWarmupBeginReplayFile":
                                var pluginName = parameters["pluginName"];
                                var pluginVersion = parameters["pluginVersion"];

                                var key = pluginName + "-" + pluginVersion;
                                if(pluginWarmerDic.ContainsKey(key))
                                {
                                    pluginWarmerDic[key].WarmupFileList.Add(parameters["filePath"] + "-" + parameters["sourceWarmerName"]);
                                }
                                else
                                {
                                    pluginWarmerDic.TryAdd(key,
                                        new PluginWarmupInfo()
                                        {
                                            PluginName = parameters["pluginName"],
                                            PluginVersion = parameters["pluginVersion"],
                                            WarmupFileList = new ConcurrentBag<string>() { parameters["filePath"] + "-" + parameters["sourceWarmerName"] }
                                        });
                                }
                                break;
                            case "PluginWarmupExcecutionException":
                                //PluginWarmupExcecutionException] pluginName="Xap.LegacyShimPluginWebAnswer"
                                //pluginVersion="22.1"
                                //path="D:\\Data\\ApplicationHostData\\PluginWarmup\\SharedProd\\Xap.LegacyShimPluginWebAnswer_22-54f5ae1a5918b60a\\1\\4be75ddf3f10415eba344152c8537ea8-3077.bin"
                                pluginName = parameters["pluginName"];
                                pluginVersion = parameters["pluginVersion"];

                                key = pluginName + "-" + pluginVersion;
                                if (pluginWarmerDic.ContainsKey(key))
                                {
                                    pluginWarmerDic[key].ExecutionExceptionList.Add(parameters["path"]);
                                }
                                else
                                {
                                    pluginWarmerDic.TryAdd(key,
                                        new PluginWarmupInfo()
                                        {
                                            PluginName = parameters["pluginName"],
                                            PluginVersion = parameters["pluginVersion"],
                                            ExecutionExceptionList = new ConcurrentBag<string>() { parameters["path"] }
                                        });
                                }
                                break;
                            case "PluginWarmupArgumentSerializationFailed":
                                //PluginWarmupArgumentSerializationFailed] pluginName="Widget.Insights.EntityLinkingRequestBuilder"
                                //pluginVersion="5.1"
                                //inputKey=4
                                //inputTypeName="Speller.AlterationProviderResponse_1"
                                pluginName = parameters["pluginName"];
                                pluginVersion = parameters["pluginVersion"];

                                key = pluginName + "-" + pluginVersion;
                                if (pluginWarmerDic.ContainsKey(key))
                                {
                                    pluginWarmerDic[key].SerializationExceptionList.Add(parameters["inputTypeName"]);
                                }
                                else
                                {
                                    pluginWarmerDic.TryAdd(key,
                                        new PluginWarmupInfo()
                                        {
                                            PluginName = parameters["pluginName"],
                                            PluginVersion = parameters["pluginVersion"],
                                            SerializationExceptionList = new ConcurrentBag<string>() { parameters["inputTypeName"] }
                                        });
                                }
                                break;
                        }
                    }
                });

            var plugnHasJitAndHasWarmer = new List<Tuple<string, int, string, string>>();
            var plugnHasJitButNoWarmer = new List<Tuple<string, int, string>>();

            foreach (var item in resultDic)
            {
                string traceId = item.Key.ToString();
                foreach(var pluginItem in item.Value)
                {
                    if (pluginItem.Value > threshold)
                    {
                        bool hasWarmer = false;
                        foreach(var warmupItem in pluginWarmerDic.Values.Where(o => o.PluginName == pluginItem.Key))
                        {
                            hasWarmer = true;
                            if (warmupItem.WarmupFileList.Any())
                            {
                                plugnHasJitAndHasWarmer.Add(new Tuple<string, int, string, string>(warmupItem.PluginName, pluginItem.Value, traceId, string.Join(";", warmupItem.WarmupFileList)));
                            }
                        }

                        if(!hasWarmer)
                        {
                            plugnHasJitButNoWarmer.Add(new Tuple<string, int, string>(pluginItem.Key, pluginItem.Value,traceId));
                        }
                    }
                }
            }

            File.WriteAllLines(baseDri + "plugnHasJitAndHasWarmer.txt", plugnHasJitAndHasWarmer.Select(o=> $"{o.Item1}\t{o.Item2}\t{o.Item3}\t{o.Item4}"));
            File.WriteAllLines(baseDri + "plugnHasJitButNoWarmer.txt", plugnHasJitButNoWarmer.Select(o => $"{o.Item1}\t{o.Item2}\t{o.Item3}"));


            var outerlierQueryList = new Dictionary<WarmupQueryInfo, IEnumerable<KeyValuePair<string, int>>>();
            foreach (var item in resultDic)
            {
                var outerLierPliginList = item.Value.Where(pluginItem => pluginItem.Value > threshold);
                if(outerLierPliginList.Any() && queryDic.ContainsKey(item.Key))
                {
                    outerlierQueryList.Add(queryDic[item.Key], outerLierPliginList);
                }
            }

            //File.WriteAllLines(baseDri + "result.txt", lines);
            File.WriteAllLines(baseDri + "outlierpluginresult.txt", outerlierQueryList.Select(o => $"{o.Key.QueryName}\t{string.Join(",", o.Value.Select(o1 => o1.Key + ":" + o1.Value))}"));
            //File.WriteAllLines(baseDri + "queryplugincount.txt", queryplugincountList);

            HashSet<string> serializeExceptionPlugins = new HashSet<string>();
            HashSet<string> executionExceptionPlugins = new HashSet<string>();

            File.WriteAllLines(baseDri + "pluginserializationexception.txt", pluginWarmerDic.Where(o=>o.Value.SerializationExceptionList.Any()).Select(o => o.Key).Distinct());
            File.WriteAllLines(baseDri + "pluginexecutionexception.txt", pluginWarmerDic.Where(o => o.Value.ExecutionExceptionList.Any()).Select(o => o.Key).Distinct());

            Console.WriteLine("OK!");
        }

        private static void TestRegex()
        {
            string text = @"D:\data\FriendlyPhoenix\BLT\2\AppHost_20210114T222200-08--T222300-08.etl:2021-01-14T22:22:10.281683 (8d7a7e6260f34c2d88a7831bdd0a34f8) [97052/82884/i:ApplicationHost BeginWarmupQuery] experimentName=""Exp in warmup query: SharedProd The Warmuped exp:SharedProd_20210115.18116429.01"" queryFile=""D:\\Data\\xap\\WarmupQueries\\Prod\\BingMapsFirstPage(san_francisco).ahrequest"" queryId=8d7a7e62-60f3-4c2d-88a7-831bdd0a34f8 traceId=0fae7972-f83d-422c-94f1-b40325b5cd73 workflow=""Xap.BingMapsFirstPage""";
            var matches = BeginWarmupRegex.Matches(text);

            string text2 = @"D:\data\FriendlyPhoenix\BLT\2\AppHost_20210114T222200-08--T222300-08.etl:2021-01-14T22:22:08.403087 (622754461382478b9b06668c3a29fe9b) [97052/46368/e:ApplicationHost Exception] type=""System.InvalidCastException"" message=""Unable to cast object of type 'PD.AnswersRanker.RankedContent_1' to type 'PD.BingGlobal.GlobalFacts.RankedContent'."" stackTrace=""   at Microsoft.Bing.Xap.InputOutputCollectionContainer`1.AddLast(Object serializableInstance) in d:\\dbs\\el\\xapp\\private\\xap\\apphost\\PlatformSchemas\\InputOutputCollectionContainer.cs:line 87\r\n   at Xap.PluginInternals.SchemaExtensions.CreateBondedContainerForObject(Object workflowOrPluginParameter, TaggedMemoryStreamAllocatorDelegate memoryStreamAllocator) in d:\\dbs\\el\\xapp\\private\\xap\\plugins\\PluginInternals\\src\\SchemaExtensions.cs:line 750\r\n   at Xap.ApplicationHost.PluginWarmupController.AddPluginArgToWarmupData(PluginContext context, Object[] execArgs, Int32 typeIndex, Int32 slot, PluginWarmupData warmupData) in d:\\dbs\\el\\xapp\\private\\xap\\apphost\\host\\src\\PluginWarmupController.cs:line 843"" recursionLevel=0";
            var matches2 = ExceptionRegex.Matches(text2);

            //foreach (Group group in matches.First().Groups)
            {
                Console.WriteLine(matches.First().Groups[2].Value);
                Console.WriteLine(matches2.First().Groups[2].Value);
            }
        }

        private static void GenerateExceptionAnalysisResult()
        {
            /*var fullPath = @"D:\temp\warmup\2.txt";

            var traceQueryDic = new Dictionary<string, WarmupQueryInfo>();

            var lines = File.ReadAllLines(fullPath);
            var totalCount = lines.Length;
            var currentCount = 0;
            foreach (var line in lines)
            {
                if (currentCount++ % 1000 == 0)
                {
                    Console.WriteLine(currentCount * 100 / totalCount);
                }
                if (line.Contains("BeginWarmupQuery"))
                {
                    Match match = BeginWarmupRegex.Match(line);
                    var key = match.Groups[1].Value;
                    if (match != null && !traceQueryDic.ContainsKey(key))
                    {
                        traceQueryDic.Add(key, new WarmupQueryInfo() { QueryName = match.Groups[2].Value, IsLimitedWarmupQuery = limitedWarmupQueries.Contains(match.Groups[2].Value), WorkflowName = match.Groups[3].Value });
                    }
                    else
                    {
                        File.AppendAllText(@"D:\temp\warmup\debugBeginWarmupQueryresult.txt", $"Failed to parse BeginWarmupQuery: {line}\r\n");
                    }
                }

                if (line.Contains("ApplicationHost Exception"))
                {
                    Match match = ExceptionRegex.Match(line);
                    if (match != null && match.Success)
                    {
                        var key = match.Groups[1].Value;
                        if (traceQueryDic.ContainsKey(key))
                        {
                            traceQueryDic[key].Exception.Add(match.Groups[2].Value);
                        }
                        else
                        {
                            File.AppendAllText(@"D:\temp\warmup\debugBeginExceptionresult.txt", $"Failed to parse Exception: {line}\r\n");
                        }
                    }
                }
            }

            File.WriteAllLines(@"D:\temp\warmup\result.txt", traceQueryDic.Values.Where(o => o.Exception.Any()).Select(o => $"{o.QueryName}   {o.WorkflowName}   {o.Exception.Count}"));
            File.WriteAllLines(@"D:\temp\warmup\allresult.txt", traceQueryDic.Values.Select(o => $"{o.QueryName}   {o.WorkflowName}   {o.Exception.Count}"));
            File.WriteAllLines(@"D:\temp\warmup\limitedresult.txt", traceQueryDic.Values.Where(o => o.Exception.Any() && o.IsLimitedWarmupQuery).Select(o => $"{o.QueryName}   {o.WorkflowName}   {o.Exception.Count}"));
            File.WriteAllLines(@"D:\temp\warmup\limitedallresult.txt", traceQueryDic.Values.Where(o => o.IsLimitedWarmupQuery).Select(o => $"{o.QueryName}   {o.WorkflowName}   {o.Exception.Count}"));
        */
        }

        private static bool GetEvent(string text, out string eventName, out Dictionary<string, string> parameters)
        {
            eventName = null;
            parameters = new Dictionary<string, string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    string[] parts = text.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 3)
                    {
                        if (parts[2].EndsWith(']'))
                        {
                            eventName = parts[2].TrimEnd(']');
                            for (int i = 3; i < parts.Length; i++)
                            {
                                var index = parts[i].IndexOf('=');
                                if (index > 0)
                                {
                                    parameters.Add(parts[i].Substring(0, index), parts[i].Substring(index + 1).Trim('\"'));
                                }
                            }
                        }
                        else if (parts[3].EndsWith(']'))
                        {
                            eventName = parts[3].TrimEnd(']');
                            for (int i = 4; i < parts.Length; i++)
                            {
                                var index = parts[i].IndexOf('=');
                                if (index > 0)
                                {
                                    parameters.Add(parts[i].Substring(0, index), parts[i].Substring(index + 1).Trim('\"'));
                                }
                            }
                        }

                        return true;
                    }
                }
            }
            catch
            {
                //Console.WriteLine(text);
            }

            return false;
        }

        private static void GenerateJittingEventAnalysisResult()
        {
            var basedDir = @"C:\Users\liagao\AppData\Local\Temp\tmpB932.tmp";
            var warmupQueryList = File.ReadAllLines(Path.Combine(basedDir, "4.txt")).ToList().Where(o => o.Contains("BeginWarmupQuery"));
            var jittingEventList = File.ReadAllLines(Path.Combine(basedDir, "4.txt")).ToList().Where(o => o.Contains("JittingStarted"));
            var result = new Dictionary<string, List<string>>();
            foreach (var query in warmupQueryList)
            {
                if (IsLimitedWarmupQuery(query, out string queryName, out string queryid))
                {
                    var jittingList = jittingEventList.Where(o => o.Contains(queryid)).Select(o=>o.Substring(o.IndexOf("JittingStarted"))).ToList();
                    if(result.ContainsKey(queryName))
                    {
                        queryName += "_1";
                    }
                    
                    result.Add(queryName, jittingList);
                    //Console.WriteLine(queryName);
                }
            }

            /*for (int i = 0; i < result.Count; i++)
            {
                foreach (var jititem in result.ElementAt(i).Value)
                {
                    for (int j = i + 1; j < result.Count; j++)
                    {
                        if(result.ElementAt(j).Value.Contains(jititem))
                        {
                            Console.WriteLine(jititem);
                        }
                    }
                }
            }

            var list = result.OrderByDescending(o => o.Value.Count).ToList();
            var newList = list.ToList();

            for(int i=0; i< newList.Count; i++)
            {
                foreach(var jititem in newList[i].Value)
                {
                    for(int j=i+1; j<list.Count(); j++)
                    {
                        list[j].Value.Remove(jititem);
                    }
                }
            }*/

            File.WriteAllLines(Path.Combine(basedDir, "result666.txt"), result.Select(o=> o.Key + "\t" + o.Value.Count));

            Console.WriteLine("OK");

            /*foreach(var item in limitedWarmupQueries)
            {
                if(!result.Any(o=>o.Contains(item)))
                {
                    File.AppendAllLines(@"C:\Users\liagao\Desktop\2\4.txt", new string[]{ item});
                }
            }

            var limitedList = File.ReadAllLines(@"C:\Users\liagao\Desktop\2\6.txt").ToList();
            var invalidList = File.ReadAllLines(@"C:\Users\liagao\Desktop\2\4.txt").ToList();

            var finalList = limitedList.Except(invalidList);

            Console.WriteLine($"{limitedList.Count} {limitedList.Distinct().Count()} {invalidList.Count}  {finalList.Count()}");

            //File.WriteAllLines(@"C:\Users\liagao\Desktop\2\7.txt", finalList);

            foreach (var item in invalidList)
            {
                Console.WriteLine(item + "==>" + limitedList.Count(o=> string.Equals(o, item)));
            }*/
        }

        private static bool IsLimitedWarmupQuery(string query, out string queryName, out string queryid)
        {
            queryName = string.Empty;
            queryid = string.Empty;
            foreach (var item in limitedWarmupQueries)
            {
                if (query.Contains(item))
                {
                    queryName = item;
                    queryid = query.Substring(query.IndexOf('(') + 1, query.IndexOf(')') - query.IndexOf('(') - 1).Replace("-", "");
                    return true;
                }
            }

            return false;
        }
    }
}
