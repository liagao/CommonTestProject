using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApp4
{
    class Program
    {
        static string[] FinalChangedPackageList = {
            "maren.sln",
            "captionsecondround.sln",
            "SparkV2.sln",
            "aapanswer.deprecated.sln",
            "celebviraltweet.sln",
            "specialdays.sln",
            "postwebrecommendation.sln",
            "halsey.tripplanner.sln",
            "proactive.lists.sln",
            "healthconditionsanswer.sln",
            "ads.xap.lux.sln",
            "ads.xapplugins.creative.sln",
            "productsearch.notmigrated.sln",
            "ads.xapplugins.policy.sln",
            "ads.aim.targetsrequest.sln",
            "hyperlocal.sln",
            "ads.xapworkflows.psfordisplay.sln",
            "eventscatalog.sln",
            "mmrelatedentities.deprecated.sln",
            "extendedactions.sln",
            "qas.sln",
            "mmrelatedentitiesxap.sln",
            "ads.baseimpressionemitter.sln",
            "imageknowledge.sln",
            "adspluspersonalization.sln",
            "informationdiscovery.sln",
            "featurecache.sln",
            "fifaworldcuptweet.sln",
            "ipblis.sln",
            "finance.currency.sln",
            "finance.notmigrated.sln",
            "fitness.sln",
            "ads.contextual.xapworkflow.sln",
            "cjkbing123.notmigrated.sln",
            "fitnessplanner.sln",
            "ads.instantconfig.sln",
            "ads.pa.offerselection.plugins.sln",
            "msnjvplugins.sln",
            "ContextManager.sln",
            "food.sln",
            "friendsofcortana.sln",
            "geosearchservice.sln",
            "ads.requestprocessor.sln",
            "globalhrs.sln",
            "ConversationalQnA.sln",
            "aiservice.sln",
            "appcontentanswer.sln",
            "halsey.aeanswersadaptor.sln",
            "appextravelanswer.sln",
            "bilingualdictionary_cycle_2.sln",
            "autosuggest.deprecated.sln",
            "cortana.api.sln",
            "billpay.personaldataretrieval.pdpapi.sln",
            "billpay.sln",
            "binggc.deprecated.sln",
            "music.sln",
            "cortana.cu.sln",
            "bingnow.reactive.sln",
            "cortana.cu.userinformationretriever.sln",
            "autosuggest.notmigrated.sln",
            "bip.casi.sln",
            "bing.sln",
            "bestnearby.sln",
            "blis.notmigrated.sln",
            "bilingualdictionary.sln",
            "bing123.sln",
            "cortanafamily.sln",
            "bingaddonanswer.sln",
            "halsey.pa.amp.sln",
            "captions.deprecated.sln",
            "crox.sln",
            "coupons.proactive.sln",
            "crane.sln",
            "dictionary.deprecated.sln",
            "newsrecommendation.sln",
            "timeline.sln",
            "newsspeaker.sln",
            "election.sln",
            "entityidlookupanswer.notmigrated.sln",
            "oauth.sln",
            "timezone.legacy.notmigrated.sln",
            "entitylinking.sln",
            "traffic.sln",
            "entityidlookupanswer.deprecated.sln",
            "halsey.proactive.editorialinference.sln",
            "halsey.pabootstrap.mlb.sln",
            "dolphin.notmigrated.sln",
            "translatoranswer.sln",
            "halsey.proactive.meetings.sln",
            "tripplanner.sln",
            "halsey.proactive.scholar.sln",
            "halsey.proactive.calendar.sln",
            "oscar.sln",
            "osdlis.notmigrated.sln",
            "pagepreviewanswer.sln",
            "knowledge.sln",
            "halsey.proactive.sln",
            "halsey.profile.sln",
            "halsey.profilev2.sln",
            "LanguageGeneration.Legacy.sln",
            "dialog.scenarios.sln",
            "limcommons.sln",
            "lists.common.sln",
            "leisureplanner.sln",
            "listssync.notmigrated.sln",
            "lists.sln",
            "listsutils.objectstore.sln",
            "listutils.notmigrated.sln",
            "liteasag.sln",
            "ListsReactive.Deprecated.sln",
            "peopledisambiguation.sln",
            "locationshift.sln",
            "lomo.sln",
            "listsreactive.notmigrated.sln",
            "local.urllookup.sln",
            "maps.sln",
            "entityplugin.notmigrated.sln",
            "recommendednews.sln",
            "selfhost.sln",
            "semanticlinking.sln",
            "recipe.sln",
            "richpackagetracking.sln",
            "halsey.recommendations.sln",
            "userunderstanding.sln",
            "uservisits.sln",
            "richpackagetracking.ccpendpoint.sln",
            "locationextraction.deprecated.sln",
            "richpackagetracking.proactive.sln",
            "weather.deprecated.sln",
            "reminderprecise.sln",
            "remoteproxyanswer.sln",
            "xap.service.falconfederator.sln",
            "sessionmanager.notmigrated.sln",
            "xap.platform.deprecated.do.not.use.sln",
            "xap.service.oauth.connectedservicestore.sln",
            "xap.service.oauth.exo.gdpr.sln",
            "xap.service.oauth.federation.sln",
            "xap.sln",
            "xappartnerplugin.events.sln",
            "socialvoice.sln",
            "socialvoiceonnews.sln",
            "xap.service.bep.sln",
            "showtimes.sln",
            "showtimesanswer.notmigrated.sln",
            "local.pbx.notmigrated.sln"
        };
        static List<string> FinalChangedPackageVersionList = new List<string>();

        static string xapnotmigratedFolder = @"D:\xap_not_migrated\";
        static HashSet<string> ChangedPackageList = new HashSet<string>();

        static List<string> ChangedFileList = new List<string>();

        static void Main(string[] args)
        {
            UpdateXapNotMigratedRepo();
            Console.WriteLine("OK!");
        }

        private static void UpdateXapNotMigratedRepo()
        {
            foreach (var package in FinalChangedPackageList)
            {
                FinalChangedPackageVersionList.Add(GetPackageVersion(Path.Combine(xapnotmigratedFolder, package)));
            }

            //File.AppendAllText(@"C:\Users\liagao\OneDrive - Microsoft\Work\5 - Busan Migration\final package upgrade.txt", string.Join(", ", FinalChangedPackageVersionList));

            for (int index = 0; index < FinalChangedPackageList.Length; index++)
            {
                File.AppendAllLines(@"C:\Users\liagao\OneDrive - Microsoft\Work\5 - Busan Migration\finalpackage.txt", new string[] { FinalChangedPackageList[index] + "\n" + FinalChangedPackageVersionList[index] });
            }

            var packages = "maren, captionsecondround, SparkV2, aapanswer.deprecated, celebviraltweet, specialdays, postwebrecommendation, halsey.tripplanner, proactive.lists, healthconditionsanswer, ads.xap.lux, ads.xapplugins.creative, productsearch.notmigrated, ads.xapplugins.policy, ads.aim.targetsrequest, hyperlocal, ads.xapworkflows.psfordisplay, eventscatalog, mmrelatedentities.deprecated, extendedactions, qas, mmrelatedentitiesxap, ads.baseimpressionemitter, imageknowledge, adspluspersonalization, informationdiscovery, featurecache, fifaworldcuptweet, ipblis, finance.currency, finance.notmigrated, fitness, ads.contextual.xapworkflow, cjkbing123.notmigrated, fitnessplanner, ads.instantconfig, ads.pa.offerselection.plugins, msnjvplugins, ContextManager, food, friendsofcortana, geosearchservice, ads.requestprocessor, globalhrs, ConversationalQnA, aiservice, appcontentanswer, halsey.aeanswersadaptor, appextravelanswer, bilingualdictionary_cycle_2, autosuggest.deprecated, cortana.api, billpay.personaldataretrieval.pdpapi, billpay, binggc.deprecated, music, cortana.cu, bingnow.reactive, cortana.cu.userinformationretriever, autosuggest.notmigrated, bip.casi, bing, bestnearby, blis.notmigrated, bilingualdictionary, bing123, cortanafamily, bingaddonanswer, halsey.pa.amp, captions.deprecated, crox, coupons.proactive, crane, dictionary.deprecated, newsrecommendation, timeline, newsspeaker, election, entityidlookupanswer.notmigrated, oauth, timezone.legacy.notmigrated, entitylinking, traffic, entityidlookupanswer.deprecated, halsey.proactive.editorialinference, halsey.pabootstrap.mlb, dolphin.notmigrated, translatoranswer, halsey.proactive.meetings, tripplanner, halsey.proactive.scholar, halsey.proactive.calendar, oscar, osdlis.notmigrated, pagepreviewanswer, knowledge, halsey.proactive, halsey.profile, halsey.profilev2, LanguageGeneration.Legacy, dialog.scenarios, limcommons, lists.common, leisureplanner, listssync.notmigrated, lists, listsutils.objectstore, listutils.notmigrated, liteasag, ListsReactive.Deprecated, peopledisambiguation, locationshift, lomo, listsreactive.notmigrated, local.urllookup, maps, entityplugin.notmigrated, recommendednews, selfhost, semanticlinking, recipe, richpackagetracking, halsey.recommendations, userunderstanding, uservisits, richpackagetracking.ccpendpoint, locationextraction.deprecated, richpackagetracking.proactive, weather.deprecated, reminderprecise, remoteproxyanswer, xap.service.falconfederator, sessionmanager.notmigrated, xap.platform.deprecated.do.not.use, xap.service.oauth.connectedservicestore, xap.service.oauth.exo.gdpr, xap.service.oauth.federation, xap, xappartnerplugin.events, socialvoice, socialvoiceonnews, xap.service.bep, showtimes, showtimesanswer.notmigrated, local.pbx.notmigrated".Split(',').Select(o => o.Trim()).ToArray();
            var packageVersions = "1.40190.5, 1.40190.5, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.5, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.3, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.5, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40192.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.3, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.3, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.5, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4, 1.40190.4".Split(',').Select(o => o.Trim()).ToArray();

            UpdatePackageReferenceInfo(@"D:\xap_bing_deploy\src\Experiments\", packages, packageVersions);

            /*var packageInfoList = new List<Tuple<string, string, string, string, string, string, string>>();

            var asProps = GetPackageReferenceInfo("AutoSuggest");
            var eaProps = GetPackageReferenceInfo("EntityAPI");
            var afProps = GetPackageReferenceInfo("AdsFrontier");
            var recProps = GetPackageReferenceInfo("Recommendations");
            var sharedProdProps = GetPackageReferenceInfo("SharedProd");

            for(var index = 0; index < packages.Length; index++)
            {
                var package = packages[index];
                var packageUpper = package.ToUpperInvariant();
                packageInfoList.Add(new Tuple<string, string, string, string, string, string, string>(
                    package,
                    asProps.ContainsKey(packageUpper) ? asProps[packageUpper] : "-",
                    eaProps.ContainsKey(packageUpper) ? eaProps[packageUpper] : "-",
                    afProps.ContainsKey(packageUpper) ? afProps[packageUpper] : "-",
                    recProps.ContainsKey(packageUpper) ? recProps[packageUpper] : "-",
                    sharedProdProps.ContainsKey(packageUpper) ? sharedProdProps[packageUpper] : "-",
                    packageVersions[index]
                    ));
            }

            File.WriteAllLines(@"D:\temp2\src\1.txt", packageInfoList.Select(o=>$"{o.Item1}\t{o.Item2}\t{o.Item3}\t{o.Item4}\t{o.Item5}\t{o.Item6}\t{o.Item7}"));*/


            //CheckAllIniFiles(xapnotmigratedFolder);
            //GetChangedPackageNameVersions(xapnotmigratedFolder);
            //UpgradeChangedPackageNameVersions(xapnotmigratedFolder);


            /*Parallel.ForEach("maren.sln,captionsecondround.sln,SparkV2.sln,aapanswer.deprecated.sln,celebviraltweet.sln,specialdays.sln,postwebrecommendation.sln,halsey.tripplanner.sln,proactive.lists.sln,healthconditionsanswer.sln,ads.xap.lux.sln,ads.xapplugins.creative.sln,productsearch.notmigrated.sln,ads.xapplugins.policy.sln,ads.aim.targetsrequest.sln,hyperlocal.sln,ads.xapworkflows.psfordisplay.sln,eventscatalog.sln,mmrelatedentities.deprecated.sln,extendedactions.sln,qas.sln,mmrelatedentitiesxap.sln,ads.baseimpressionemitter.sln,imageknowledge.sln,adspluspersonalization.sln,informationdiscovery.sln,featurecache.sln,fifaworldcuptweet.sln,ipblis.sln,finance.currency.sln,finance.notmigrated.sln,fitness.sln,ads.contextual.xapworkflow.sln,cjkbing123.notmigrated.sln,fitnessplanner.sln,ads.instantconfig.sln,ads.pa.offerselection.plugins.sln,msnjvplugins.sln,ContextManager.sln,food.sln,friendsofcortana.sln,geosearchservice.sln,ads.requestprocessor.sln,globalhrs.sln,ConversationalQnA.sln,aiservice.sln,appcontentanswer.sln,halsey.aeanswersadaptor.sln,appextravelanswer.sln,bilingualdictionary_cycle_2.sln,autosuggest.deprecated.sln,cortana.api.sln,billpay.personaldataretrieval.pdpapi.sln,billpay.sln,binggc.deprecated.sln,music.sln,cortana.cu.sln,bingnow.reactive.sln,cortana.cu.userinformationretriever.sln,autosuggest.notmigrated.sln,bip.casi.sln,bing.sln,bestnearby.sln,blis.notmigrated.sln,bilingualdictionary.sln,bing123.sln,cortanafamily.sln,bingaddonanswer.sln,halsey.pa.amp.sln,captions.deprecated.sln,crox.sln,coupons.proactive.sln,crane.sln,dictionary.deprecated.sln,newsrecommendation.sln,timeline.sln,newsspeaker.sln,election.sln,entityidlookupanswer.notmigrated.sln,oauth.sln,timezone.legacy.notmigrated.sln,entitylinking.sln,traffic.sln,entityidlookupanswer.deprecated.sln,halsey.proactive.editorialinference.sln,halsey.pabootstrap.mlb.sln,dolphin.notmigrated.sln,translatoranswer.sln,halsey.proactive.meetings.sln,tripplanner.sln,halsey.proactive.scholar.sln,halsey.proactive.calendar.sln,oscar.sln,osdlis.notmigrated.sln,pagepreviewanswer.sln,knowledge.sln,halsey.proactive.sln,halsey.profile.sln,halsey.profilev2.sln,LanguageGeneration.Legacy.sln,dialog.scenarios.sln,limcommons.sln,lists.common.sln,leisureplanner.sln,listssync.notmigrated.sln,lists.sln,listsutils.objectstore.sln,listutils.notmigrated.sln,liteasag.sln,ListsReactive.Deprecated.sln,peopledisambiguation.sln,locationshift.sln,lomo.sln,listsreactive.notmigrated.sln,local.urllookup.sln,maps.sln,entityplugin.notmigrated.sln,recommendednews.sln,selfhost.sln,semanticlinking.sln,recipe.sln,richpackagetracking.sln,halsey.recommendations.sln,userunderstanding.sln,uservisits.sln,richpackagetracking.ccpendpoint.sln,locationextraction.deprecated.sln,richpackagetracking.proactive.sln,weather.deprecated.sln,reminderprecise.sln,remoteproxyanswer.sln,xap.service.falconfederator.sln,sessionmanager.notmigrated.sln,xap.platform.deprecated.do.not.use.sln,xap.service.oauth.connectedservicestore.sln,xap.service.oauth.exo.gdpr.sln,xap.service.oauth.federation.sln,xap.sln,xappartnerplugin.events.sln,socialvoice.sln,socialvoiceonnews.sln,xap.service.bep.sln,showtimes.sln,showtimesanswer.notmigrated.sln,local.pbx.notmigrated.sln".Split(','),
                package =>
                {
                    UpgradePackageVersion(Path.Combine(xapnotmigratedFolder, package));
                });
            */

            /*// step1: find all ini files
            ChangeAllIniFiles(xapnotmigratedFolder);

            Parallel.ForEach(ChangedPackageList, package =>
            {
                var folder = Path.Combine(xapnotmigratedFolder, package);

                // step2: upgrade packages
                Dictionary<string, string> versionList = new Dictionary<string, string>
                {
                    { "DataSchemasBaseline", "4.*"},
                    { "Xap.Sdk", "7.*"},
                };
                ChangePackageReferenceVersion(folder, versionList);

                // step3: upgrade properties
                Dictionary<string, string> propertyList = new Dictionary<string, string>
                {
                    { "TargetFrameworkVersion", "v4.7.2"},
                };

                ChangeProperties(folder, propertyList);

                // Step4: upgrade package version
                UpgradePackageVersion(folder);
            });*/

            /*Dictionary<string, string> versionList = new Dictionary<string, string>
                {
                    { "DataSchemasBaseline", "4.*"},
                    { "Xap.Sdk", "7.*"},
                };
            ChangePackageReferenceVersion(@"D:\xap_not_migrated\msnjvenglish.sln", versionList);*/
        }

        private static void TestFunc(out string[] test)
        {
            test = new string[] { "aaa", "bbb"};
        }

        private static void UpdatePackageReferenceInfo(string folder, string[] packages, string[] packageVersions)
        {
            var packageDic = new Dictionary<string, string>();
            var packageRefCountDic = new Dictionary<string, int>();

            for (int i=0;i<packages.Length;i++)
            {
                packageDic.Add(packages[i].ToUpperInvariant(), packageVersions[i]);
                packageRefCountDic.Add(packages[i].ToUpperInvariant(), 0);
            }

            foreach (var props in Directory.GetFiles(folder, "*.props", SearchOption.AllDirectories))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(props));
                foreach (XmlNode node in doc.GetElementsByTagName("PackageReference"))
                {
                    var key = node.Attributes["Include"].Value.ToUpperInvariant();
                    if (packageDic.ContainsKey(key))
                    {
                        node.FirstChild.FirstChild.Value = $"[{packageDic[key]}]";
                        packageRefCountDic[key] = 1;
                    }
                }

                doc.Save(props);
            }

            //Console.WriteLine(packageRefCountDic.First(o=>o.Value == 0).Key);
        }

        private static Dictionary<string, string> GetPackageReferenceInfo(string experiment)
        {
            var result = new Dictionary<string, string>();
            foreach (var props in Directory.GetFiles(@"D:\temp2\src\Experiments", "*.props", SearchOption.AllDirectories))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(props));
                foreach (XmlNode node in doc.GetElementsByTagName("PackageReference"))
                {
                    var key = node.Attributes["Include"].Value.ToUpperInvariant();
                    if(!result.ContainsKey(key))
                    {
                        result.Add(key, node.FirstChild.FirstChild.Value);
                    }
                }
            }

            return result;
        }

        private static void GetChangedPackageNameVersions(string xapnotmigratedFolder)
        {
            Dictionary<string, string> packageInfoList = new Dictionary<string, string>();
            Parallel.ForEach(Directory.GetDirectories(xapnotmigratedFolder, "*", SearchOption.TopDirectoryOnly),
                dir =>
                {
                    if(dir.EndsWith(".sln"))
                    {
                        bool hasOverride = false;
                        // find whether there is Busan override
                        foreach (var file in Directory.GetFiles(dir, "*.ini", SearchOption.AllDirectories))
                        {
                            var originalLines = File.ReadAllLines(file);
                            var newLines = new List<string>();

                            for (int index = 0; index < originalLines.Length; index++)
                            {
                                var line = originalLines[index];

                                if (line.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasOverride = true;
                                    break;
                                }
                            }

                            if(hasOverride)
                            {
                                break;
                            }
                        }

                        if(hasOverride)
                        {
                            var dirName = Path.GetFileNameWithoutExtension(dir);

                            var manifest = Directory.GetFiles(dir, "XapManifest.json", SearchOption.AllDirectories).First();
                            var version = File.ReadAllLines(manifest).First(o => o.Contains("\"Version\"")).Split('\"')[3];

                            packageInfoList.Add(dirName, version);
                        }
                    }
                }
            );

            File.WriteAllLines(@"D:\testresult.txt", new string[] { string.Join(',', packageInfoList.Keys), string.Join(',', packageInfoList.Values) });
        }

        private static void UpgradeChangedPackageNameVersions(string xapnotmigratedFolder)
        {
            Parallel.ForEach(Directory.GetDirectories(xapnotmigratedFolder, "*", SearchOption.TopDirectoryOnly),
                dir =>
                {
                    if (dir.EndsWith(".sln"))
                    {
                        bool hasOverride = false;
                        // find whether there is Busan override
                        foreach (var file in Directory.GetFiles(dir, "*.ini", SearchOption.AllDirectories))
                        {
                            var originalLines = File.ReadAllLines(file);
                            var newLines = new List<string>();

                            for (int index = 0; index < originalLines.Length; index++)
                            {
                                var line = originalLines[index];

                                if (line.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasOverride = true;
                                    break;
                                }
                            }

                            if (hasOverride)
                            {
                                break;
                            }
                        }

                        if (hasOverride)
                        {
                            UpgradePackageVersion(dir);
                        }
                    }
                }
            );
        }

        private static void CheckAllIniFiles(string xapnotmigratedFolder)
        {
            Parallel.ForEach(Directory.GetFiles(xapnotmigratedFolder, "*.ini", SearchOption.AllDirectories),
                file =>
                {
                    var originalLines = File.ReadAllLines(file);
                    var newLines = new List<string>();

                    for (int index = 0; index < originalLines.Length; index++)
                    {
                        var line = originalLines[index - 1];
                        var nextLine = originalLines[index];

                        if (line.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase) && nextLine.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"{file} has duplicated overrides...");
                        }
                    }
                }
            );
        }

        private static void UpgradePackageVersion(string folder)
        {
            Parallel.ForEach(Directory.GetFiles(folder, "XapManifest.json", SearchOption.AllDirectories),
                file =>
                {
                    var originalLines = File.ReadAllLines(file);
                    var newLines = new List<string>();

                    for (int index = 0; index < originalLines.Length; index++)
                    {
                        var line = originalLines[index];

                        if(line.Contains("\"Version\""))
                        {
                            var secs = line.Split('"');
                            var abnormalVersionSecs = secs[3].Split('-');
                            Version version = new Version(abnormalVersionSecs[0]);
                            var newVersion = new Version(version.Major, version.Minor, version.Build + 1);
                            var newVersionString = abnormalVersionSecs.Length > 1 ? $"{newVersion}-{abnormalVersionSecs[1]}" : newVersion.ToString();
                            newLines.Add(line.Replace(secs[3], newVersionString));
                        }
                        else
                        {
                            newLines.Add(line);
                        }
                    }

                    File.WriteAllLines(file, newLines, Encoding.UTF8);
                }
            );
        }

        private static string GetPackageVersion(string folder)
        {
            return File.ReadAllLines(Directory.GetFiles(folder, "XapManifest.json", SearchOption.AllDirectories)[0]).First(o => o.Contains("\"Version\"")).Split('\"')[3];
        }


        private static void ChangeProperties(string folder, Dictionary<string, string> propertyList)
        {
            Parallel.ForEach(Directory.GetFiles(folder, "*.csproj", SearchOption.AllDirectories),
                file =>
                {
                    var originalLines = File.ReadAllLines(file);
                    var newLines = new List<string>();

                    for (int index = 0; index < originalLines.Length; index++)
                    {
                        bool hasChangedProperty = false;
                        var line = originalLines[index];
                        foreach (var property in propertyList)
                        {
                            if (line.Contains(property.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                hasChangedProperty = true;
                                var secs = line.Split(new char[] { '<', '>' });
                                newLines.Add($"{line.Replace(secs[2], property.Value)}");
                                break;
                            }
                        }

                        if(!hasChangedProperty)
                        {
                            newLines.Add(line);
                        }
                    }
                    
                    File.WriteAllLines(file, newLines, Encoding.UTF8);
                }
            );
        }

        private static void ChangePackageReferenceVersion(string folder, Dictionary<string, string> versionList)
        {
            Parallel.ForEach(Directory.GetFiles(folder, "*.csproj", SearchOption.AllDirectories),
                file =>
                {
                    var originalLines = File.ReadAllLines(file);
                    var newLines = new List<string>();

                    for (int index = 0; index < originalLines.Length; index++)
                    {
                        bool hasChangedProperty = false;
                        var line = originalLines[index];
                        foreach (var version in versionList)
                        {
                            if (line.Contains($"\"{version.Key}\"", StringComparison.OrdinalIgnoreCase)&& line.Contains("PackageReference ", StringComparison.OrdinalIgnoreCase))
                            {
                                hasChangedProperty = true;
                                newLines.Add(line);
                                newLines.Add($"{line.Substring(0, line.IndexOf('<'))}  <Version>{version.Value}</Version>");
                                index++;
                                break;
                            }
                        }

                        if (!hasChangedProperty)
                        {
                            newLines.Add(line);
                        }
                    }

                    File.WriteAllLines(file, newLines, Encoding.UTF8);
                }
            );
        }

        private static void ChangeAllIniFiles(string folder)
        {
            List<string> solutionList = new List<string>();
            Parallel.ForEach(Directory.GetFiles(folder, "*.ini", SearchOption.AllDirectories),
                file =>
                {
                    var originalLines = File.ReadAllLines(file);
                    var newLines = new List<string>();

                    bool hasNewLine = false;

                    for (int index = 0; index < originalLines.Length; index++)
                    {
                        var line = originalLines[index];
                        newLines.Add(line);

                        if (line.Contains("Xap-Prod-Hk--Group", StringComparison.OrdinalIgnoreCase))
                        {
                            //Console.WriteLine("Changing file" + file + "...");
                            string newLine = line.Replace("Xap-Prod-Hk--Group", "Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase);
                            newLines.Add(newLine);
                            hasNewLine = true;
                        }
                    }

                    if (hasNewLine)
                    {
                        File.WriteAllLines(file, newLines, Encoding.UTF8);
                        ChangedFileList.Add(file);
                        var path = file.Replace(xapnotmigratedFolder, string.Empty, StringComparison.OrdinalIgnoreCase);
                        var packageName = path.Substring(0, path.IndexOf('\\'));

                        if(packageName.EndsWith(".sln"))
                        {
                            lock(ChangedPackageList)
                            {
                                ChangedPackageList.Add(packageName);
                            }
                        }
                    }
                }
            );
        }
    }
}
