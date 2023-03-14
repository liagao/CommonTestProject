namespace ConsoleApp4
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    class Program
    {
        // "aw.answersnews-prod-hkg01.hkg01.ap.phx.gbl"
        public static string[] RemoteHostList = { 
            //"objectstoremulti.prod.hk.binginternal.com",
            //"ipeudss-vip.ipepersonalization-prod-hk2.hk2.ap.phx.gbl",
            //"inferenceswebapi.hk2.glbdns2.microsoft.com",
            "geospatial-backend-prod.hk.binggeospatial.com",
            "tc-urpvip.cacheeap-prod-hkg01.hkg01.ap.gbl",
            "hk.mediation.trafficmanager.net",
            "bing-local-rentalproperty-backend.hk.binggeospatial.com",
            "bing-microsegments-traillocations-backend.hk.binggeospatial.com",
            "bing-movies-movieshowtimes-backend.hk.binggeospatial.com",
            "bing-satori-libraries-backend.hk.binggeospatial.com",
            "hk2.platformcn.maps.glbdns2.microsoft.com",
            //"objectstoremultibe.prod.hk.binginternal.com",
            "sidamoamdxap-eap-vip.adsmediationsnr1-prod-hkge01.hkge01.ap.gbl" };

        public static string[] DeprecatedPackageList = { 
            "blis.notmigrated",
            "richpackagetracking.proactive",
            "lists",
            "halsey.tripplanner",
            "ListUtils",
            "ListsSync",
            "SparkV2",
            "informationdiscovery",
            "entityplugin.notmigrated",
            "timezone.legacy.notmigrated"
        };

        public const string LatestBadConfigFiles = @"D:\BusanMigration\latestbadconfigs.csv";
        public const string NewLatestBadConfigFiles = @"D:\BusanMigration\newlatestbadconfigs.csv";
        public const string BusanMigrationDir = @"D:\BusanMigration\";
        public const string LatestHkEndpointBadConfigFiles = @"D:\BusanMigration\latesthkendpointconfigs.csv";
        public const string NewLatestHkEndpointBadConfigFiles = @"D:\BusanMigration\newlatesthkendpointconfigs.csv";

        public const string ComponentDir = @"D:\data\ov19";

        public static string[] WhitelistPackages = { "ReverseGeocoder" };

        class ConfigItem
        {
            public string PackageInfo { get; set; }
            public string DRIContact { get; set; }
            public string Owner { get; set; }
            public string VP { get; set; }
            public string EM { get; set; }
            public string Raw { get; set; }
            public string Line { get; set; }
        }

        class PackageInfo
        {
            public string PackageName { get; set; }
            public string PackageVersion { get; set; }
            public string Owner { get; set; }
            public string DRIContact { get; set; }
            public string VP { get; set; }
            public string EM { get; set; }
        }

        class PluginInfo
        {
            public PluginInfo(PackageInfo package)
            {
                PackageInfo = package;
                PackageName = package.PackageName;
                PackageVersion = package.PackageVersion;
            }

            public PluginInfo(string packageName)
            {
                PackageName = packageName;
            }

            public PluginInfo(string packageName, string packageVersion, string pluginName, string pluginVersion, PackageInfo? packageInfo)
            {
                PackageName = packageName;
                PackageVersion = packageVersion;
                PluginName = pluginName;
                PluginVersion = pluginVersion;
                PackageInfo = packageInfo;
                if (packageInfo != null)
                {
                    packageInfo.PackageVersion = packageVersion;
                }
            }

            public PackageInfo PackageInfo { get; set; }
            public string PackageName { get; set; }
            public string PackageVersion { get; set; }
            public string PluginName { get; set; }
            public string PluginVersion { get; set; }

            public override bool Equals(object? obj)
            {
                if (obj != null)
                {
                    var instance = obj as PluginInfo;
                    if (instance != null)
                    {
                        return string.Equals(this.PackageName, instance.PackageName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(this.PackageVersion, instance.PackageVersion, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(this.PluginName, instance.PluginName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(this.PluginVersion, instance.PluginVersion, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return false;
            }
        }

        static void Main(string[] args)
        {
            FindAllNotMigratedFileFromexperimentFolder(ComponentDir, "result-badconfig.txt", (path) => FindAllNotMigratedPluginConfigFilesForEnvAlias(path));
        }

        private static void FindAllNotMigratedFileFromexperimentFolder(string expPath, string resultFileName, Func<string, Dictionary<string, string>> func)
        {
            var alivePackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // load from previous run
            var packageDic = File.ReadAllLines(LatestBadConfigFiles).Skip(1).Select(item =>
            {
                var parts = item.Split(',');
                var packageParts = parts[0].Split('@');
                var configItem = new PackageInfo() { PackageName = packageParts[0], PackageVersion = packageParts.Length > 1 ? packageParts[1] : string.Empty, DRIContact = parts[1], Owner = parts[2], VP = parts[3], EM = parts[4] };
                return configItem;
            }).ToDictionary(o => o.PackageName);

            // update owner information from hkendpoint file
            foreach(var item in File.ReadAllLines(LatestHkEndpointBadConfigFiles))
            {
                var parts = item.Split(',');
                var packageParts = parts[1].Split('@');
                if(packageDic.ContainsKey(packageParts[0]))
                {
                    var packageInfo = packageDic[packageParts[0]];
                    packageInfo.DRIContact = parts[2];
                    packageInfo.Owner = parts[3];
                    packageInfo.VP = parts[4];
                    packageInfo.EM = parts[5];
                }

                var configItem = new PackageInfo() { PackageName = packageParts[0], PackageVersion = packageParts.Length > 1 ? packageParts[1] : string.Empty, DRIContact = parts[1], Owner = parts[2], VP = parts[3], EM = parts[4] };

            }


            // initialize R_* package info map from xap_notmigrated repo
            var resourcePackageInfo = LoadResourcePackageInfo(packageDic);

            // initialize the relation between R_* and package/plugin info
            var packageResourceDic = LoadPackageResourceMap(Path.Combine(expPath, "ExperimentComponents.tsv"), packageDic, alivePackages);

            // initialize the relation between HASH and <R_*,plugin file>
            var iniResourceDic = LoadIniResourceMap(Path.Combine(expPath, "exp.resource.hardlink.manifest"));

            // initialize the relation between plugin file and the bad lines
            var fileList = func(Path.Combine(expPath, "RCache", "Ini"));

            // generate the result package->pluginFile+badlines
            var resultDic = new Dictionary<PluginInfo, List<string>>();
            foreach (var file in fileList)
            {
                foreach (var rcacheFileNameItem in iniResourceDic[file.Key])
                {
                    var pluginInfos = new List<PluginInfo> { new PluginInfo(rcacheFileNameItem.Item1)};

                    if (packageResourceDic.ContainsKey(rcacheFileNameItem.Item1))
                    {
                        pluginInfos = packageResourceDic[rcacheFileNameItem.Item1];
                    }
                    else if (resourcePackageInfo.ContainsKey(rcacheFileNameItem.Item1))
                    {
                        pluginInfos = new List<PluginInfo> { new PluginInfo(resourcePackageInfo[rcacheFileNameItem.Item1]) };
                    }

                    foreach(var item in pluginInfos)
                    {
                        if(alivePackages.Contains(item.PackageName, StringComparer.OrdinalIgnoreCase) 
                            && !WhitelistPackages.Contains(item.PackageName, StringComparer.OrdinalIgnoreCase))
                        {
                            if (resultDic.ContainsKey(item))
                            {
                                resultDic[item].Add($"{rcacheFileNameItem.Item2}({file.Value})");
                            }
                            else
                            {
                                resultDic.Add(item, new List<string> { $"{rcacheFileNameItem.Item2}({file.Value})" });
                            }
                        }
                    }
                }
            }

            OutputFinalResult(expPath, resultFileName, resultDic);
        }

        private static void OutputFinalResult(string expPath, string resultFileName, Dictionary<PluginInfo, List<string>> resultDic)
        {
            var resultPath = Path.Combine(BusanMigrationDir, resultFileName);
            File.WriteAllLines(resultPath, resultDic.Select(o => $"{o.Key.PackageName}@{o.Key.PackageVersion},{o.Key.PluginName}@{o.Key.PluginVersion},{o.Key.PackageInfo?.DRIContact},{o.Key.PackageInfo?.Owner},{o.Key.PackageInfo?.VP},{o.Key.PackageInfo?.EM},{string.Join(";", o.Value)}"));

            // write result for each hk endpoint
            File.WriteAllText(NewLatestHkEndpointBadConfigFiles, string.Empty);
            foreach (var endpoint in RemoteHostList)
            {
                var hkendpointresult = new Dictionary<string, Tuple<PackageInfo, HashSet<string>>>();
                foreach (var item in resultDic)
                {
                    if (!DeprecatedPackageList.Contains(item.Key.PackageName, StringComparer.OrdinalIgnoreCase))
                    {
                        foreach (var line in item.Value)
                        {
                            if (line.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
                            {
                                if (hkendpointresult.ContainsKey(item.Key.PackageName))
                                {
                                    hkendpointresult[item.Key.PackageName].Item2.Add(line);
                                }
                                else
                                {
                                    hkendpointresult.Add(item.Key.PackageName,
                                        new Tuple<PackageInfo, HashSet<string>>(item.Key.PackageInfo ?? new PackageInfo { PackageName = item.Key.PackageName, PackageVersion = item.Key.PackageVersion }, new HashSet<string> { line }));
                                }
                            }
                        }
                    }
                }

                File.AppendAllLines(NewLatestHkEndpointBadConfigFiles, hkendpointresult.Select(o=> $"{endpoint},{o.Value.Item1.PackageName}@{o.Value.Item1.PackageVersion},{o.Value.Item1.DRIContact},{o.Value.Item1.Owner},{o.Value.Item1.VP},{o.Value.Item1.EM},{string.Join(";", o.Value.Item2)}"));
            }

            var finalpath = Path.Combine(expPath, "results-finalconfigs.csv");
            var finalresult = new Dictionary<string, Tuple<PackageInfo, HashSet<string>>>();
            foreach (var item in resultDic)
            {
                if(string.Equals(item.Key.PackageName, "locationshift", StringComparison.OrdinalIgnoreCase))
                {
                    item.Key.PackageInfo.DRIContact = "BINGEXPCORTANACORTANAEXPERIENCE\\CortanaProactiveExperienceChinaDRI";
                    item.Key.PackageInfo.Owner = "FAREAST\\jasonqin,fareast\\xapmsnjvwrite,FAREAST\\kezhang,fareast\\dillyli,fareast\\fenli,Redmond\\cortanaaechina";
                    item.Key.PackageInfo.VP = "Haoyong Zhang";
                    item.Key.PackageInfo.EM = "Haoyong Zhang";
                }
                foreach (var line in item.Value)
                {
                    if (finalresult.ContainsKey(item.Key.PackageName))
                    {
                        finalresult[item.Key.PackageName].Item2.Add(line);
                    }
                    else
                    {
                        finalresult.Add(item.Key.PackageName,
                            new Tuple<PackageInfo, HashSet<string>>(item.Key.PackageInfo ?? new PackageInfo { PackageName = item.Key.PackageName, PackageVersion = item.Key.PackageVersion }, new HashSet<string> { line }));
                    }
                }
            }

            File.WriteAllLines(NewLatestBadConfigFiles, new string[] { $"Package Info , DRI Contact , Owner , VP , EM , Raw" });
            File.AppendAllLines(NewLatestBadConfigFiles, finalresult.Select(o => $"{o.Value.Item1.PackageName}@{o.Value.Item1.PackageVersion},{o.Value.Item1.DRIContact},{o.Value.Item1.Owner},{o.Value.Item1.VP},{o.Value.Item1.EM},{string.Join(";", o.Value.Item2)}"));

            File.WriteAllLines(finalpath, new string[] { $"Package Info , DRI Contact , Owner , VP , EM , Raw" }); 
            File.AppendAllLines(finalpath, finalresult.Select(o => $"{o.Value.Item1.PackageName}@{o.Value.Item1.PackageVersion},{o.Value.Item1.DRIContact},{o.Value.Item1.Owner},{o.Value.Item1.VP},{o.Value.Item1.EM},{string.Join(";", o.Value.Item2)}"));
        }

        private static Dictionary<string, PackageInfo> LoadResourcePackageInfo(Dictionary<string, PackageInfo> packageDic)
        {
            var resourcePackageDic = new Dictionary<string, PackageInfo>();
            Parallel.ForEach(Directory.GetDirectories(@"D:\xap_not_migrated", "*.sln", SearchOption.TopDirectoryOnly), dir =>
            {
                var package = Path.GetFileNameWithoutExtension(dir);

                PackageInfo packageInfo = null;
                if (packageDic.ContainsKey(package))
                {
                    packageInfo = packageDic[package];

                    if(string.IsNullOrEmpty(packageInfo.PackageVersion))
                    {
                        packageInfo.PackageVersion = GetPackageContactInfo(package, File.ReadAllLines(Directory.GetFiles(dir, "XapManifest.json", SearchOption.AllDirectories).First())).PackageVersion;
                    }
                }
                else if (Directory.GetFiles(dir, "XapManifest.json", SearchOption.AllDirectories).Any())
                {
                    packageInfo = GetPackageContactInfo(package, File.ReadAllLines(Directory.GetFiles(dir, "XapManifest.json", SearchOption.AllDirectories).First()));
                }

                foreach (var pack in Directory.GetDirectories(dir, "R_*", SearchOption.AllDirectories).Select(o => Path.GetFileName(o)))
                {
                    lock(resourcePackageDic)
                    {
                        if (!resourcePackageDic.ContainsKey(pack))
                        {
                            resourcePackageDic.Add(pack, packageInfo);
                        }
                    }
                }
            });

            return resourcePackageDic;
        }

        private static PackageInfo GetPackageContactInfo(string package, string[] content)
        {
            return new PackageInfo()
            {
                PackageName = package,
                DRIContact = content.Where(o => o.Contains("IcmContacts")).FirstOrDefault().Split(":")[1].Replace("[", "").Replace("]", "").Replace("\\\\", "\\").Replace("\"", "").Trim(),
                Owner = content.Where(o => o.Contains("Owners")).FirstOrDefault().Split(":")[1].Replace("[", "").Replace("]", "").Replace("\\\\", "\\").Replace("\"", "").Replace(",", " ").Trim(),
                PackageVersion = content.Where(o => o.Contains("Version")).FirstOrDefault().Split(":")[1].Replace("\"", "").Replace(",", "").Trim()
            };
        }

        private static Dictionary<string,string> FindAllNotMigratedPluginConfigFilesForEnvAlias(string dir)
        {
            var fileList = new Dictionary<string,string>();

            Parallel.ForEach(Directory.GetFiles(dir), file =>
            {
                //Console.WriteLine($"Parsing {file}...");
                var content = File.ReadAllText(file);
                var listContent = File.ReadAllLines(file);

                var lines = new HashSet<string>();

                Parallel.ForEach(listContent, line =>
                {
                    /*if(line.Contains("flt"))
                    {
                        Cons*/

                    if (line.Contains("Host=hk2.platformcn.maps.glbdns2.microsoft.com", StringComparison.OrdinalIgnoreCase))
                    {
                        lock(lines)
                        {
                            lines.Add(line);
                        }
                    }
                    else if (line.Contains("Xap-Prod-Hk--Group", StringComparison.OrdinalIgnoreCase))
                    {
                        string key = line.Substring(0, line.IndexOf('=')).Replace("Xap-Prod-Hk--Group", "Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase).Trim();
                        if (!content.Contains(key, StringComparison.OrdinalIgnoreCase))
                        {
                            lock (lines)
                            {
                                lines.Add(line);
                            }
                        }
                        else
                        {
                            foreach (var newLine in listContent.Where(o => o.Contains(key, StringComparison.OrdinalIgnoreCase)))
                            {
                                string value = newLine.Substring(line.IndexOf('='));
                                if (value.Contains("hk", StringComparison.OrdinalIgnoreCase))
                                {
                                    lock(lines)
                                    {
                                        lines.Add(newLine);
                                    }
                                }
                            }
                        }
                    }
                });

                if (lines.Any())
                {
                    lock (fileList)
                    {
                        fileList.Add(Path.GetFileName(file), string.Join(";", lines));
                    }
                }
            });

            return fileList;
        }

        private static Dictionary<string, List<Tuple<string, string>>> LoadIniResourceMap(string manifest)
        {
            var result = new Dictionary<string, List<Tuple<string, string>>>();
            Parallel.ForEach(File.ReadAllLines(manifest), line =>
            {
                var sec = line.Split(',');
                if (sec != null && sec.Length == 2)
                {
                    var key = sec[1].Split('\\')[1];
                    lock(result)
                    {
                        if (result.ContainsKey(key))
                        {
                            result[key].Add(new Tuple<string, string>(sec[0].Split('\\')[0], sec[0].Split('\\')[1]));
                        }
                        else
                        {
                            var dic = new List<Tuple<string, string>> { new Tuple<string, string>(sec[0].Split('\\')[0], sec[0].Split('\\')[1]) };
                            result.Add(key, dic);
                        }
                    }
                }
            });

            return result;
        }

        private static Dictionary<string, List<PluginInfo>> LoadPackageResourceMap(string tsv, Dictionary<string, PackageInfo> resourcePackageInfo, HashSet<string> alivePackages)
        {
            var result = new Dictionary<string, List<PluginInfo>>();
            Parallel.ForEach(File.ReadAllLines(tsv), line =>
            {
                var sec = line.Split('\t');

                if (sec.Length > 2)
                {
                    var parts = sec[1].Split("@");
                    if (parts.Length == 2)
                    {
                        lock(alivePackages)
                        {
                            alivePackages.Add(parts[0]);
                        }
                    }
                }

                if (sec != null && sec.Length == 3 && !string.IsNullOrWhiteSpace(sec[2]))
                {
                    lock(result)
                    {
                        if (result.ContainsKey(sec[2]))
                        {
                            var packageName = sec[1].Split('@')[0];
                            result[sec[2]].Add(
                                new PluginInfo(
                                    packageName,
                                    sec[1].Split('@')[1],
                                    sec[0].Split('@')[0],
                                    sec[0].Split('@')[1],
                                    resourcePackageInfo != null && resourcePackageInfo.ContainsKey(packageName) ? resourcePackageInfo[packageName] : null));
                        }
                        else
                        {
                            var packageName = sec[1].Split('@')[0];
                            result.Add(sec[2], new List<PluginInfo> {
                            new PluginInfo(
                                packageName,
                                sec[1].Split('@')[1],
                                sec[0].Split('@')[0],
                                sec[0].Split('@')[1],
                                resourcePackageInfo != null && resourcePackageInfo.ContainsKey(packageName) ? resourcePackageInfo[packageName] : null)
                            });
                        }
                    }
                }
            });

            return result;
        }
    }
}


/*
 * 
 * /*var packageConfigDic = new Dictionary<string, string>();
var packageConfigDicClone = new Dictionary<string, string>();
var packagehashset = new HashSet<string>();
var resourcehashset = new HashSet<string>();

foreach (var line in File.ReadAllLines(@"D:,emp\config.txt"))
{
    var parts = line.Split(',');
    packageConfigDic.Add(parts[0], parts[1]);

    if(parts[0].Split('@').Length == 2)
    {
        packagehashset.Add(parts[0].Split('@')[0]);
    }
    else
    {
        resourcehashset.Add(parts[0]);
    }
}

packageConfigDicClone = new Dictionary<string, string>(packageConfigDic);

var resourcePackageDic = new Dictionary<string, List<string>>();
var packageOwnerDic = new Dictionary<string, string>();

foreach (var dir in Directory.GetDirectories(@"D:\xap_not_migrated", "*.sln", SearchOption.TopDirectoryOnly))
{
    var package = Path.GetFileNameWithoutExtension(dir);
    foreach(var pack in Directory.GetDirectories(dir, "R_*", SearchOption.AllDirectories).Select(o=> Path.GetFileName(o)))
    {
        if(resourcehashset.Contains(pack))
        {
            if (!resourcePackageDic.ContainsKey(pack))
            {
                resourcePackageDic.Add(pack, new List<string>() { package });
            }
            else
            {
                resourcePackageDic[pack].Add(package);
            }
        }
    }

    if(Directory.GetFiles(dir, "XapManifest.json", SearchOption.AllDirectories).Any())
    {
        packageOwnerDic.Add(package, GetPackageContactInfo(File.ReadAllLines(Directory.GetFiles(dir, "XapManifest.json", SearchOption.AllDirectories).First())));
    }
}

string GetPackageContactInfo(string[] content)
{
    return (content.Where(o => o.Contains("IcmContacts")).FirstOrDefault().Split(":")[1].Replace("[", "").Replace("]", "").Replace("\\\\", "\\").Replace("\"", "").Trim() + "  "
        + content.Where(o => o.Contains("Owners")).FirstOrDefault().Split(":")[1].Replace("[", "").Replace("]", "").Replace("\\\\", "\\").Replace("\"", "").Trim().Trim()).Trim(',').Trim();
}

foreach (var item in packageConfigDic)
{
    if (item.Key.StartsWith("R_"))
    {
        if(resourcePackageDic.ContainsKey(item.Key))
        {
            bool merged = false;
            foreach(var package in resourcePackageDic[item.Key])
            {
                foreach (var item2 in packageConfigDic)
                {
                    if(string.Equals(item2.Key.Split('@')[0], package))
                    {
                        packageConfigDicClone.Remove(item.Key);
                        packageConfigDicClone[item2.Key] += "," + item.Value;
                        merged = true;
                        break;
                    }
                }
            }

            if(!merged)
            {
                var package = resourcePackageDic[item.Key][0];
                packageConfigDicClone.Remove(item.Key);
                if(packageConfigDicClone.ContainsKey(package))
                {
                    packageConfigDicClone[package] += item.Value;
                }
                else
                {
                    packageConfigDicClone.Add(package, item.Value);
                }
            }
        }
    }
}

var lines = new List<string>();
foreach (var item in packageConfigDicClone)
{
    string owner = "N/A";
    if(packageOwnerDic.ContainsKey(item.Key))
    {
        owner = packageOwnerDic[item.Key];
    }

    lines.Add($"{item.Key},{item.Value},{owner}");
}

File.WriteAllLines(@"D:,emp\NewPackageList.txt", lines);

var packageConfigDic = new Dictionary<string, Tuple<string, string>>();
foreach (var line in File.ReadAllLines(@"D:,emp\NewPackageList.txt"))
{
    var parts = line.Split(',');
    packageConfigDic.Add(parts[0], new Tuple<string, string>(parts[1], parts[2]));
}

List<string[]> result = new List<string[]>();
Dictionary<string, string[]> dic = new Dictionary<string, string[]>();

foreach (var line in File.ReadLines(@"D:,emp\DetailedPackage.txt"))
{
    var values = line.Split(",");
    result.Add(values);

    dic.Add(values[1], values);
}

foreach (var item in packageConfigDic)
{
    if (dic.ContainsKey(item.Key))
    {
        dic[item.Key][9] = item.Value.Item1;
    }
    else
    {
        result.Add(new string[] { "200", item.Key, String.Empty, String.Empty, String.Empty, String.Empty, item.Value.Item2, String.Empty, String.Empty, item.Value.Item1 });
    }
}

File.WriteAllLines(@"D:,emp\UpdatedPackageConfig.txt", result.Select(o => String.Join(',', o)));
*/


// write separate results for each hk endpoint
/*foreach(var endpoint in RemoteHostList)
{
    var path = Path.Combine(expPath, "results", endpoint + ".csv");
    var result = new Dictionary<string, Tuple<PackageInfo, HashSet<string>>>();
    foreach(var item in resultDic)
    {
        foreach(var line in item.Value)
        {
            if(line.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
            {
                if(result.ContainsKey(item.Key.PackageName))
                {
                    result[item.Key.PackageName].Item2.Add(line);
                }
                else
                {
                    result.Add(item.Key.PackageName, 
                        new Tuple<PackageInfo, HashSet<string>>(item.Key.PackageInfo?? new PackageInfo { PackageName = item.Key.PackageName, PackageVersion = item.Key.PackageVersion}, new HashSet<string> { line }));
                }
            }
        }
    }

    File.WriteAllLines(path, result.Select(o=> $"{o.Value.Item1.PackageName}@{o.Value.Item1.PackageVersion},@,{o.Value.Item1.DRIContact},{o.Value.Item1.Owner},{o.Value.Item1.VP},{o.Value.Item1.EM},{string.Join(",", o.Value.Item2)}"));
}*/


/*var result = new Dictionary<string, List<ConfigItem>>();
foreach (var item in RemoteHostList)
{
    result.Add(item, new List<ConfigItem>());
}

foreach(var item in File.ReadAllLines(@"D:\BusanMigration\configs.txt"))
{
    var parts = item.Split(',');
    var configItem = new ConfigItem() { PackageInfo = parts[0], DRIContact = parts[1], Owner = parts[2], VP = parts[3], EM = parts[4], Raw = parts[5], Line = item };

    foreach(var key in RemoteHostList)
    {
        if(configItem.Raw.Contains(key, StringComparison.OrdinalIgnoreCase))
        {
            result[key].Add(configItem);
        }
    }
}

foreach(var item in result)
{
    File.AppendAllLines(@"D:\BusanMigration\configs-result.txt", new string[] { item .Key });
    File.AppendAllLines(@"D:\BusanMigration\configs-result.txt", item.Value.Select(o=>o.Line));
    File.AppendAllLines(@"D:\BusanMigration\configs-result.txt", new string[] { string.Empty, string.Empty });
}*/


//FindAllNotMigratedFileFromexperimentFolder(@"D:\data\ov4", "result-ea.txt", (path)=> FindAllNotMigratedPluginConfigFilesForEnvAlias(path));
//FindAllNotMigratedFileFromexperimentFolder(@"D:\data\ov4", "result-os.txt", (path) => FindAllNotMigratedPluginConfigFilesForObjectStore(path));

//GetEnvironmentOwner();
//JsonConvert.DeserializeXmlNode(File.ReadAllText(@"C:\Users\liagao\AppData\Local\XTS\Clusters\PilotFish\GetClustersConfiguration.cache"), "AutoPilotEnvironments").Save(@"C:\Users\liagao\AppData\Local\XTS\Clusters\PilotFish\GetClustersConfiguration.xml");
//File.WriteAllLines(Path.Combine(@"D:\data\ov5", "result-outboundlist.txt"), FindAllIpList(Path.Combine(@"D:\data\ov5", "RCache", "Ini")));
//File.WriteAllLines(Path.Combine(@"D:\data\ov5", "result-outboundlist.txt"), FindAllOutboundEnvironmentList(@"D:\data\ov5"));           

/*private static List<string> FindAllOutboundEnvironmentList(string expPath)
{
    // initialize the relation between R_* and package/plugin info
    var packageResourceDic = LoadPackageResourceMap(Path.Combine(expPath, "ExperimentComponents.tsv"));
    var iniResourceDic = LoadIniResourceMap(Path.Combine(expPath, "exp.resource.hardlink.manifest"));

    var fileList = FindAllOutboundEnvironmentListFromFolder(Path.Combine(expPath, "RCache", "Ini"));

    var resultDic = new Dictionary<string, List<string>>();
    foreach (var file in fileList.Keys)
    {
        foreach (var rcacheFileNameItem in iniResourceDic[file])
        {
            var packageName = packageResourceDic.ContainsKey(rcacheFileNameItem.Item1) ? packageResourceDic[rcacheFileNameItem.Item1] : rcacheFileNameItem.Item1;
            if (resultDic.ContainsKey(packageName))
            {
                resultDic[packageName].Add($"{rcacheFileNameItem.Item2},{fileList[file]}");
            }
            else
            {
                resultDic.Add(packageName, new List<string> { $"{rcacheFileNameItem.Item2},{fileList[file]}" });
            }
        }
    }

    var result = new List<string>();
    foreach(var package in resultDic)
    {
        foreach(var file in package.Value)
        {
            result.Add($"{package.Key} , {file}");
        }
    }
    return result;
}*/

/*


        static Dictionary<string, string> GetEnvironmentOwner()
        {
            var result = new Dictionary<string, string>();
            //XmlDocument doc1 = JsonConvert.DeserializeXmlNode(File.ReadAllText(@"C:\Users\liagao\AppData\Local\XTS\Clusters\ApClassic\GetClustersConfiguration.cache"), "AutoPilotEnvironments");

            XDocument doc = XDocument.Load(@"C:\Users\liagao\AppData\Local\XTS\Clusters\ApClassic\GetClustersConfiguration.xml");

            foreach (var env in EnvList.Distinct())
            {
                XElement? element = doc.Descendants("Name").FirstOrDefault(o => string.Equals(o.Value, env, StringComparison.OrdinalIgnoreCase));
                if (element == null)
                {
                    // No such env
                    //result.Add(env, "Not Exist!");
                }
                else
                {
                    var owner = element.Parent?.Descendants("Owner").FirstOrDefault();
                    if (owner != null && !string.IsNullOrWhiteSpace(owner.Value))
                    {
                        // Found the owner for this env
                        result.Add(env, owner.Value);
                    }
                    else
                    {
                        result.Add(env, GetOwnerFromParentVE(element, doc));
                    }
                }
            }

            doc = XDocument.Load(@"C:\Users\liagao\AppData\Local\XTS\Clusters\PilotFish\GetClustersConfiguration.xml");

            foreach (var env in EnvList.Distinct())
            {
                if(!result.ContainsKey(env))
                {
                    XElement? element = doc.Descendants("Name").FirstOrDefault(o => string.Equals(o.Value, env, StringComparison.OrdinalIgnoreCase));
                    if (element == null)
                    {
                        // No such env
                        result.Add(env, "Not Exist!");
                    }
                    else
                    {
                        var owner = element.Parent?.Descendants("Owner").FirstOrDefault();
                        if (owner != null && !string.IsNullOrWhiteSpace(owner.Value))
                        {
                            // Found the owner for this env
                            result.Add(env, owner.Value);
                            Console.WriteLine(env + "->" + owner.Value);
                        }
                        else
                        {
                            var value = GetOwnerFromParentVE(element, doc);
                            result.Add(env, value);
                            Console.WriteLine(env + "->" + value);
                        }
                    }
                }
            }

            File.WriteAllLines(@"D:\EnvOwnerResult2.txt", result.ToArray().Select(o => o.Key + "," + o.Value));

            return result;
        }

        static string GetOwnerFromParentVE(XElement? element, XDocument doc)
        {
            if(element!=null && element.Parent!=null)
            {
                var ve = element.Parent.Element("ParentVirtualEnvironments");
                if(ve != null)
                {
                    foreach (var parentVE in ve.Descendants())
                    {
                        var parentId = parentVE.Attribute("ref")?.Value;
                        if(parentId != null)
                        {
                            var veElement = doc.Descendants("values").FirstOrDefault(o => string.Equals(o.Attribute("id")?.Value, parentId));
                            if(veElement!=null)
                            {
                                var owner = veElement.Descendants("Owner").FirstOrDefault()?.Value;
                                if(!string.IsNullOrWhiteSpace(owner))
                                {
                                    return owner;
                                }
                            }
                        }
                    }
                }
            }

            return "N/A";
        }*/


/*public static string[] EnvList = { "Xap-Prod-CO4" };
public static string[] EnvList = {
            "Xap-Prod-CO4",
            "Xap-Prod-BN2B",
            "Xap-Prod-Ch1b",
            "AdsServeApps-PPE-BN1",
            "AdsPAServeApps-PPE-BN1",
            "AdsPASnrDELite-PPE-BN1",
            "AdsServeApps-INT-BN1",
            "AdsPASnr-Int-Bn1",
            "AdQueryProbeBatch-Prod-Ch1d",
            "Nyx-Dev-Ch1d",
            "AdQueryProbeBatch-Prod-Ch1b",
            "AdQP-Dev-Ch1b",
            "AdQPFE-Dev-MW1",
            "AdsCXServeApps-Prod-Ch1d",
            "AdsCXServeApps-Prod-Co3",
            "AdsCXServeApps-Prod-Hk2",
            "AdsCXServeApps-Prod-Sg1",
            "AdsPSSCP-Prod-Bn2",
            "AdsPSSCP-Prod-Ch1d",
            "AdsPSSCP-Prod-Co3",
            "AdsPSSCP-Prod-Db3",
            "AdsPSSCP-Prod-Db4",
            "AdsPSSCP-Prod-Hk2",
            "AdsPSSCP-Prod-Sg1",
            "AdInquiry-Prod-Ch1b",
            "AdInquiry-Prod-CO4",
            "AdInquiry-INT-CO4",
            "bingwidget-Prod-ch1d",
            "bingwidget-Prod-Bn2",
            "bingwidget-Prod-Hk2",
            "bingwidget-Prod-Bn2b",
            "bingwidget-Prod-dub01",
            "BingWidget-Prod-Ch1b",
            "BingWidget-Prod-Co4",
            "CASI-Prod-Merino-Bn1",
            "CASI-Prod-Merino-Ch1d",
            "CASI-Prod-Merino-Co3",
            "Casi-Prod-Merino-Co3b",
            "CASI-Prod-Merino-Db4",
            "CasiMerino-Prod-HK2",
            "SpeechEAP-PPE-CO4",
            "CUService-PROD-BN2",
            "CUService-PROD-CH1",
            "CUService-PROD-CO3",
            "CUService-PROD-DB4",
            "CUService-PROD-DB5",
            "CUService-PROD-HK2",
            "CUService-PPE-BN2",
            "CUService-PPE-CH1",
            "CUService-PPE-CH1D",
            "CUService-PPE-CO3B",
            "CUService-PPE-HK2",
            "CUService-PPE-DB5",
            "CUServiceEXT-INT-CO3B",
            "CUService-INT-CO3",
            "CUService-INT-CO3B",
            "CUService-Test-BN2",
            "CUService1-SANDBOX-BN2",
            "CUService1-SANDBOX-CO3B",
            "CUService2-SANDBOX-CO3B",
            "CUService3-SANDBOX-CO3B",
            "CUService4-SANDBOX-CO3B",
            "CUService2-SANDBOX-BN2",
            "CUService3-SANDBOX-BN2",
            "SpeechEAP-PPE-Ch1b",
            "SpeechEAP-PPE-db5",
            "SpeechEAP-PPE-HK2",
            "SpeechEAP-PPE-BN2B",
            "SpeechEAP-Prod-CO4",
            "SpeechEAP-Prod-Ch1b",
            "SpeechEAP-Prod-db5",
            "SpeechEAP-Prod-HK2",
            "SpeechEAP-Prod-BN2B",
            "Frontdoor-Prod-Bj1",
            "Frontdoor-Prod-Bn1",
            "Frontdoor-Prod-Ch1d",
            "Frontdoor-Prod-Co3",
            "Frontdoor-Prod-Db4",
            "Frontdoor-Prod-Hk2",
            "Frontdoor-Prod-Sg1",
            "FrontdoorCanary-Prod-Co3",
            "Halsey-Prod-Bn2",
            "Halsey-Prod-Ch1d",
            "Halsey-Prod-Co3b",
            "Halsey-Prod-db4",
            "Halsey-Prod-db5",
            "Halsey-Prod-HK2",
            "Halsey-Prod-Ch1b",
            "intenttools-prod-co3",
            "intenttools-prod-co3b",
            "intenttools-prod-ch1",
            "intenttools-prod-ch1d",
            "intenttools-prod-db4",
            "intenttools-prod-bn1",
            "intenttools-prod-bn2",
            "intenttools-prod-HK2",
            "intenttools-PPE-Co3b",
            "IPEAgents-Prod-Bn2",
            "IPEAgents-Prod-Co3b",
            "IPEAgents-Prod-Db3",
            "IPEAgents-Prod-Db4",
            "IPEAgents-Prod-Hk2",
            "IPEAgents-Prod-Ch1",
            "IPEInferences-Prod-Bn2",
            "IPEInferences-Prod-Ch1d",
            "IPEInferences-Prod-Co3",
            "IPEInferences-Prod-Db4",
            "IPEInferences-Prod-Hk2",
            "Lobby-Prod-Bn2",
            "Lobby-Prod-Ch1d",
            "Lobby-Prod-Co3",
            "Lobby-Prod-Db3",
            "Lobby-Prod-Hk2",
            "IPESRService-Prod-BN2",
            "IPESRService-Prod-Ch1",
            "IPESRService-Prod-Co3b",
            "IPESRService-Prod-Db4",
            "IPESRService-Prod-HK2",
            "IPESRService-Prod-Bn2",
            "IPESRService-Prod-Ch1d",
            "IPESRService-Prod-Co3",
            "IPESRService-Prod-Hk2",
            "Jupiter-Prod-Co3b",
            "Jupiter-Prod-HK2",
            "MMServe3-Prod-Hk2",
            "MMServe1-Prod-Db5",
            "MMServe3-Prod-Co4",
            "MMServe3-Prod-Ch1b",
            "MMServeRanking10-Prod-BN01",
            "MMServeSelection10-Prod-Bn01",
            "MMServeRanking10-Prod-DUB02",
            "MMServeSelection10-Prod-DUB02",
            "Relativity-Prod-Bn2",
            "Relativity-Prod-Ch1d",
            "Relativity-Prod-Co3b",
            "Relativity-Prod-Db3",
            "Relativity-Prod-Hk2",
            "CoreUX-Prod-BN2",
            "CoreUX-Prod-BN2",
            "CoreUX-Prod-Ch1b",
            "CoreUX-Prod-Ch1b",
            "CoreUX-Prod-Co4",
            "CoreUX-Prod-Db5",
            "CoreUX-Prod-DUB02",
            "CoreUX-Prod-DUB02",
            "CoreUX-Prod-Hk2",
            "CoreUX-Prod-Hkg01",
            "CoreUXEAP-Prod-Hk2",
            "CoreUX-Prod-BN2B",
            "CoreUX-Prod-HKGE01",
            "RealtimeUX-Prod-Bn2",
            "RealtimeUX-Prod-Ch1d",
            "RealtimeUX-Prod-Co3",
            "RealtimeUX-Prod-Db4",
            "RealtimeUX-Prod-Hk2",
            "Spyglass-Prod-BN1",
            "Spyglass-Prod-CH1d",
            "Spyglass-Prod-CH1b",
            "Spyglass-Prod-Co3",
            "Spyglass-Prod-Co4",
            "Spyglass-Prod-Db4",
            "Spyglass-Prod-HK2",
            "Spyglass-Prod-BN2",
            "Cache-Prod-CO01",
            "Cache-Prod-BN01",
            "Cache-Prod-CHI02",
            "Cache-Prod-DB5",
            "CacheEAP-Prod-HKG01",
            "APIQSG-Prod-Bn1",
            "APIQSG-Prod-Ch1d",
            "APIQSG-Prod-Co3",
            "APIQSG-Prod-Db4",
            "APIQSG-Prod-Hk2",
            "XAP-Prod-BN2B",
            "XAP-Prod-Ch1b",
            "XAP-Prod-Co4",
            "XAP-Prod-DUB02",
            "XAP-Prod-HKG01",
            "CommuteService-Prod-Bn2",
            "CommuteService-Prod-Ch1d",
            "CommuteService-Prod-Co3b",
            "CommuteService-Prod-Db4",
            "CommuteService-Prod-HK2",
            "CommuteService-PPE-HK2",
            "CoreUXLog-Prod-BN01",
            "CoreUXLog-Prod-CH01",
            "CoreUXLog-Prod-CO01",
            "CoreUXLog-Prod-DUB01",
            "CoreUXLog-Prod-HKG01",
            "XapTools-Prod-Bn1",
            "XapTools-Prod-Co3",
            "XapTools-Prod-Db4",
            "XapTools-Prod-Hk2",
            "XapTools-Prod-Ch1d",
            "XapTools-Int-Db4",
            "XapTools-Ppe-Bn1",
            "XapTools-PPE-MW1",
            "XapTools-Prod-Co4",
            "XapTools-Prod-Ch1b",
            "BingXPingEAP-Test-MWHE01",
            "BingXPingEAP1-Prod-Ch1b",
            "BingXPingEAP2-Prod-Ch1b",
            "BingXPingEAP-Prod-CHI02",
            "BingXPingEAP-Prod-BNZE01",
            "BingXPingEAP-Prod-CHIE01",
            "BingXPingEAP-Prod-DUBE01",
            "BingXPingEAP-Prod-HKGE01",
            "BingXPingEAP-Prod-MWHE01",
            "AdsCXServeApps-INT-Bn1",
            "AdsCXServeApps-PPE-Bn1",
            "AdsCXServeApps-INT-Bn2",
            "AdsCXServeApps-PPE-Bn2",
            "bingwidget-Dev-Bn1",
            "CASI-INT-Sandbox-BN1",
            "CoreUXFastDeployment-Dev-BN1",
            "CUService-PPE-Bn2",
            "CUService1-Sandbox-Bn2",
            "CUService2-Sandbox-Bn2",
            "CUService-Test-Bn2",
            "FrontdoorTIP-Test-BN1",
            "FrontdoorTIP-Test-Ch1d",
            "FrontdoorTIP-Test-Co3",
            "Frontdoor4-Test-BN1",
            "FrontDoor-Int-BN1",
            "IPEAgents-PPE-Br4",
            "IPEAgents-PPE-BN2",
            "IPEAgentsClient-PPE-BN2",
            "IPEAgentsProdTest",
            "IPEInferences-INT-Co3b",
            "IPESRService-INT-Ch1d",
            "IPESRService-INT-Co3",
            "IPESRService-PPE-Bn2",
            "IPESRService-PPE-Co3",
            "IPESRService-Sandbox-Bn1",
            "IPESRService-Sandbox-Bn2",
            "IPESRService-Sandbox-Co3",
            "IPESRService2-Sandbox-Bn2",
            "IPESRServiceEXT-INT-Bn2",
            "IPESRServiceEXT-INT-Co3",
            "IPESRServiceWS-INT-Bn2",
            "Lobby-INT-BN2",
            "Lobby2-DEV-BN2",
            "LobbyPerf-TEST-CH1D",
            "Lobby-PPE-CH1D",
            "CoreUXStaging-PPE-Bn1",
            "CoreUXStaging-PPE-BN01",
            "CoreUXExperimentation2-Test-MW1",
            "CoreUXExperimentation2-Test-CH01",
            "CoreUXExperimentation-Test-BN01",
            "CoreUX-PPE-CO4",
            "CoreUX-PPE-MWHE01",
            "Starlite-Prod-Prod-Ch1d",
            "APIQSG-PPE-BN1",
            "APIQSG-INT-BN1",
            "TTSService-INT-HK2",
            "TTSService2-PROD-HK2",
            "CoreUxLog-Test-CO01"};
public static string[] ValueList = { "101.198.192.45"
,"101.198.192.87"
,"101.198.193.197"
,"180.163.251.138"
,"27.115.124.241"
,"42.236.9.119"
,"65.55.106.74"
,"8353:lowes.com,6624:homedepot.com,7683:kohls.com,13777:target.com,1610:bestbuy.com,8552:macys.com,8986:michaels.com,3837:dell.com,9850:northerntool.com,3627:dickssportinggoods.com,8142:lightinthebox.com,1521:bedbathandbeyond.com,6224:hsn.com,10759:petco.com,16246:ebay.com,15667:williams-sonoma.com,7749:llbean.com,2506:campingworld.com,10721:personalizationmall.com,8514:mscdirect.com,9774:nike.com"
,"aaastore.adc.glbdns2.microsoft.com"
,"adsfdlogger-int.trafficmanager.net"
,"apartmentguide.com/apartments,rent.com,forrent.com,realtor.com/apartments"
,"apps.apple.com/"
,"apps.apple.com/au/app/"
,"apps.apple.com/ca/app/"
,"apps.apple.com/de/app/"
,"apps.apple.com/fr/app/"
,"apps.apple.com/in/app/"
,"apps.apple.com/us/app/"
,"archive.sap.com/discussions/thread/,discussions.apple.com/thread/,talk.collegeconfidential.com/,www.tomshardware.com/faq/,boards.cruisecritic.com/showthread.php,www.answers.com/Q/,www.quora.com/,answers.microsoft.com/en-,www.datalounge.com/thread/,talk.collegeconfidential.com/,girlschannel.net/topics/,okwave.jp/qa/q,q.hatena.ne.jp/,komachi.yomiuri.co.jp/t/,detail.chiebukuro.yahoo.co.jp/qa/question_detail/q,oshiete.goo.ne.jp/qa/,routy.jp/qa/,sooda.jp/qa/,soudan1.biglobe.ne.jp/qa,oshiete1.nifty.com/qa,jp.quora.com/,matome.naver.jp/odai/,ja.stackoverflow.com/questions/,ja.meta.stackoverflow.com/questions/,mori.nc-net.or.jp/qa,faq.doc-net.or.jp/index.php?app"
,"assets.msn.com"
,"baike.baidu.com"
,"baike.baidu.com,zhcnTopDomain,Model_Slapi,Model_Index"
,"bi:businessinsider.com,ct:chicagotribune.com,etnt:eatthisnotthat.com"
,"bing-local-trafficavailability-backend.bn.binggeospatial.com"
,"bing-movies-movieshowtimes.ppe.bn.binggeospatial.com"
,"bing-movies-movieshowtimes.ppe.co.binggeospatial.com"
,"bing-movies-movieshowtimes-backend.ppe.co.binggeospatial.com"
,"ch1d.cache.binginternal.com"
,"cn.tripadvisor.com"
,"cortanamusic.asgfalcon.io"
,"en.wikipedia.org,en.m.wikipedia.org,www.ndtv.com,www.breakingnews.com,news.cchgroup.com,newsroom.intel.com,www.citynews.ca,www.newswise.com,news.konpaevents.com,www.wcnews.com,www.suncommunitynews.com,www.dailynewsbin.com,www.renalandurologynews.com,xtq.zynews.cn,news.harvard.edu,news.nike.com,newsela.com,www.onenewspage.us,twitter.com,www.huffingtonpost.com,www.dfa.ie,www.foodnetwork.com,www.youtube.com"
,"en.wikipedia.org,en.m.wikipedia.org,www.ndtv.com,www.breakingnews.com,news.cchgroup.com,newsroom.intel.com,www.citynews.ca,www.newswise.com,news.konpaevents.com,www.wcnews.com,www.suncommunitynews.com,www.dailynewsbin.com,www.renalandurologynews.com,xtq.zynews.cn,news.harvard.edu,news.nike.com,newsela.com,www.onenewspage.us,twitter.com,www.huffingtonpost.com,www.dfa.ie,www.foodnetwork.com"
,"ext.tf.360.cn"
,"fandom.com,bert_v0.1,bert_v0.2,bert_v0.3,bert_v0.4,bert_v0.5"
,"fandom.com,bert_v0.5"
,"fandom.com,SocialNav,OGConcept,SchemaOrg,JsonLD,baike.baidu.com"
,"fandom.com,SocialNav,OGConcept,SchemaOrg,JsonLD,baike.baidu.com,zhcnTopDomain"
,"fandom.com,SocialNav,OGConcept,SchemaOrg,JsonLD,Wiki,JsonPath,zhcnTopDomain,baike.baidu.com,Model_Slapi,Model_Index"
,"fr.tripadvisor.ca"
,"fr.tripadvisor.ch"
,"geospatial-backend-staging.co.binggeospatial.com"
,"geospatialserve1-vip.geospatial-ppe-bn1.playmsn.com"
,"geospatialserve4.dev.bn.binggeospatial.com"
,"http://65.55.105.252:86/api/produce/databykeyhashinbody"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/006978d8-6f1b-433e-8f59-161ffae02380"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/013949e2-b886-484d-9d90-4184d9dd067f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/02277a6e-5309-4619-b8a2-66e20008aab8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/024154e4-551a-42ff-a854-e7b8e57ce547"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/02a601b4-8128-4048-ab18-1d18b3b5813c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/02bf9558-8ea8-447b-b96d-8cacfd040ff2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0372cc5a-ab01-4983-bc39-0867059bbbb8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/03c5679b-af5f-4f0a-a735-631047043b47"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/042fe5ae-1b73-4a60-9b77-7ac1fe6cf52f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0450c62f-732d-4bef-a359-e2f9e2ec6f44"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/04a31cae-9857-4682-aea9-746f9a643b68"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/050cdcda-d152-4a14-8c95-86f37acf2109"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/067af3f2-cf37-4baa-bc1f-24845bab94fc"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/06b042f2-ce66-438b-b122-65763d441cb2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/06c47dd1-d3b1-4a2b-958c-268f2251464e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/078a123f-4649-4b7f-ae55-2917b8d44043"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/078eea67-27f8-44b5-8542-1bea569159d2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/07936d32-cbb1-42b8-9517-0bdc4b0e9b5f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0824f2bc-0789-4cba-beea-d65dd9848bbb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/08bc62f4-d2d3-4a0b-a052-65e0a51edf7e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/098fea5c-0c49-4661-bcc9-a9ab393b674a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/09fa28cf-ebcc-4dad-b82e-e499a7b95ab4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0a01d695-cc4e-48e6-a1a3-d47709e8b53c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0a49d220-c4d8-4a6c-9627-d418914d4619"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0a79ff70-9491-4bb3-895a-c00bb35cd47a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0a98b786-a5f5-439b-b73a-55d266d1bdba"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0ae578c0-3e51-4714-b829-faf428edaee7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0be66952-83e3-493c-9f0e-3bec92c7824d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0bf3c415-c828-464d-9311-237f5a2efebf"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0c768fca-6606-40f1-8dbe-0bdf25ce159e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0c7f8c60-876c-4e36-89ee-c30151acc16b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0c970cb6-b028-4b7d-95a9-1cbdb7517534"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0d437f62-d4a1-43ea-9625-7161ffd761ec"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0df96775-7e0c-4168-ae92-cba49507fa89"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0e17ab64-94ad-430f-b879-94d6080b40f5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0e1bd223-e98b-40d0-8cd9-1786ecf2e01d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0e385bd9-f43c-4bc3-a2fa-e608468cd9c6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/0eacc9d8-6cb8-4ca9-af02-4e6b497d9000"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/10c8ebd4-9cae-44bc-9ef3-58e5956f8808"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1181c95e-9166-48e8-a87c-625ae4bc662e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/11aad4ea-fc6e-4a5c-b8aa-06443dbe3d7c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1214c6ae-d2cc-4272-b66a-0e001ca8fa5e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/14767cc3-da15-41f2-982b-8f68f742a45f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/148816ae-144a-4ff8-8cdd-fa5d0ce809b8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/15c055bd-6441-41ee-b150-829b8c0b8d39"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/15f1194d-fc6e-4509-8f2b-da42521617d6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/165ec46d-0785-4762-b717-23c03b9040aa"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/168f7540-3514-4a17-9628-1ea2591769ac"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/177ccf46-67e8-4d0c-96ce-4781b4da49c9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1793f5b1-8b66-4df1-ae6b-667ff469c928"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/17d17c7d-86d2-4bff-9d19-1804e3d61f4c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1828a81a-a03b-4b55-9ca3-4aea892b8cbf"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1865cbf1-dfd6-44c0-8c18-65962efdc168"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/18bee592-0988-4229-89b6-2995b0735952"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/191fe677-72de-404d-ba91-635a51460948"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1a3edc72-b10a-418b-8ff2-3555d422faff"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1ad6aa66-4fa1-461b-a185-059acd106f10"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1adab0ab-62b1-4447-bf36-995f7f2bfdde"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1bed1cfd-fe2a-4ce7-9e61-257ebdee954c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1cefd243-0d02-43a7-a164-569269ff7a3a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1d0cb3b0-a1b5-46e6-80dd-e06bb13e0cac"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1d3bf285-3e7a-4a1e-b7a3-9ddd91546fb3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1e14e4e2-4b54-4968-a94d-a2451cdb1910"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1f6a23b1-61b0-41a7-931e-596725a8c3b8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1faf4f91-48f0-4701-a5de-21105dfd49bd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/1ffce474-61ba-476d-a8b2-1c2262f12354"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2006d1e2-f96a-4321-86cd-cd575b05227f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/205a3139-e12e-4c52-bcbe-8728c96c6bf4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/209b482b-9776-41cf-904f-f94128562797"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/218bf1f0-80bd-419b-8d4d-91335c52c5a6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2198d6e1-04e8-4d7a-b5a8-a78a0916cd32"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/21b1937d-1357-4dad-bac7-c665fd1eb8f6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/21e65423-817b-4727-98d4-589a846f99b7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/24217e51-48b5-4652-934c-280d0b90f79c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/248f2b67-659a-4fd4-aa0b-2984b73d0d5d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/25f32549-9c93-4779-b87e-7fcd9385351e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/26548d5b-9571-431d-8eca-e32fe983f6fa"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2691876f-3d0d-4dca-bc60-ec6c9cd1b185"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/269e002e-27b4-43da-b4ec-0d5bfa59a9ed"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/26f88488-c17d-4f92-8d03-581878bbed1a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/274d681f-2c09-4f4d-91ac-e86d4be8fea2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/29763c0a-434b-46a7-8b63-8896112fa28a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2a816119-0303-4252-b5a3-a780977c5879"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2b04b57f-e9f0-4b63-97d8-7c7c34a9aa0b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2b3f717b-dfb9-46d1-8970-f9a27c776082"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2b51be07-c836-469f-927d-9f7116efeb9a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2b85cc79-5974-4292-9868-1bcf967379a7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2bda3dbc-948c-4fff-816d-8a4054e635da"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2ca61d41-3b0b-4e69-9fa1-e7b4aa9bdec0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2d54114a-52f4-4c03-87d6-211bb74ff36b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2d94bd4e-9366-4f9a-9857-04ad58019328"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2ed37b5b-86a2-4e00-b8c2-8a9495b53cad"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2eda9a14-442d-4fe5-9fe8-29c129fcf281"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2f26e40e-0e16-40ce-9521-77ab9be72f2a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2f4d3ab1-b164-4329-ae0b-6665e7e1e320"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2f5ac396-24c4-494d-ad0e-03ed0324453d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2faa0e5e-7a83-465c-8626-3fef7a66381e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2fc684db-0c7f-4dbf-8f2c-950c83a8f6ae"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/2ffb55c7-ca4d-44be-90d3-9d68622b2ec2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/301af9ce-338b-4214-adf7-e86962e26513"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3033ed37-480d-4fb8-a0f2-976286b9ebe4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/309e9ddf-55f6-4bef-bdbb-3935fb0b1899"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3215d95c-d273-4998-a291-53b013b07f59"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/324712bf-c21d-413a-b8ae-bc5997b8356c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3294bdb1-fd6d-4939-9b1a-a3284e1b65ac"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3331f38d-daa5-4210-bab4-186ce8d74600"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/33a36ce7-a05d-41b8-ba25-b4f2b571b674"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/33c9add1-5d4a-4a70-b634-4e91220be667"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/34cee59f-4c49-4568-a644-7b5111ba91a5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3658d0a9-5946-44fa-b1b0-2674b873b57d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/36a0a23e-c670-4dfb-a1be-2f99d284d8e2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/36bb799b-1238-407e-a4b0-10f066cd0ebc"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/36c782a2-9a95-49e5-8281-c43220f046af"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/376cd7ac-a0a2-4d2d-8a20-9f902b251a49"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/37b336c4-082e-4afb-a197-d15c525db338"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/37fd7ef4-7672-4f6d-9401-d0948ea58f03"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/382771b0-b943-4c67-9eba-9ac5a8401e90"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/387c1e61-7983-4a88-b73f-e4271793cc3c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3a481d56-c2f4-4d50-81be-1554fd039d55"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3acacd80-eee6-480d-a689-4a61d0be786e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3ae6e8a3-771f-4ada-be0c-5d29f1378b14"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3b112ec6-316f-4cb4-9bb6-878093333f55"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3b2c683c-0175-4762-aa6a-5a065dea5943"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3b4a9fd4-5e45-41f8-86c3-138a6eee3553"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3b943678-0244-473e-a23a-d3a7aa73234b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3d2098ed-f660-442e-b6d0-92b16069e43d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3e2c94b0-08d3-41fd-bd96-8e7473ca99a5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3e4ef9b7-62d1-4dde-9ac3-1e7c6cfc2d87"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3f2acbaa-c36c-44e1-9542-28436d34a695"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3f8e21c9-74e6-4455-b6fc-691aab4f9331"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3fc57856-730b-456a-ae89-c63eb4754953"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/3fd6a871-1ec3-47fc-a83e-68dca210e271"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4001cf83-9dbc-4201-afc4-2b2d4892629b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/401d60cf-3db8-4b64-8bdb-a10df60a4dbe"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/40241e9b-bfe5-4736-8463-84db0036c1d9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/40441a45-0fd6-4fd4-94f6-d8083b825ce9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/40c8a2e3-2e0a-47ca-b741-fd89ff70cab6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/41ab63bf-2c2e-4c58-bc9a-0e2172946c71"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/41fe44c3-0de1-41bf-b4fb-10472cbad744"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/42400975-4339-473f-a17e-f918c7b56ac8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/43207d46-f0d8-48b6-ae13-2fb87a06117a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/434877aa-d975-48c5-87be-bcdc33acb025"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/437e61ad-3a87-418e-8bc0-107d338a792d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/44825af6-ebdd-4dfe-8541-a100a68e1e1e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/452b3fc2-6200-4256-87fc-3f6fb59a7e21"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/45673f30-383a-4e8e-b9d8-9e4892754569"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4591b4e2-e54a-4470-addd-9e2e23559fcb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/45ac1169-39e0-4776-a49d-21c0760fd37e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/45be9f3d-57ab-44cd-a99b-3c4e0f893539"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/45dae6f1-ceed-42dc-b454-94fdb4ff71fb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/45dfc1b8-f471-4c36-92af-9fec8d45a35c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/46d8e4bc-30e1-4d10-a9ec-eb2668d7d91f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4776321a-489f-4631-895e-bdf6bc5b2d48"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4781544e-2a28-42c4-abea-1db141badc9a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/47dc3b0c-a6d7-4015-a261-f60b7437723b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/492288e4-eb89-4e34-8b60-41fd833c8915"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4927d363-201c-4ba7-9334-b92fd3096ebe"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/492f82c1-cc0d-4abb-978d-e07e765be6b6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4965caf8-3b9b-4a0b-86d0-ab01f1028d41"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4c1550d1-12ca-4154-84bc-84f8dd970c24"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4c476558-a1b6-4676-a244-4e34aeb069b0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4c6edde9-441f-4cd7-8f05-563f6ab44329"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4c8251a2-3158-46bc-8770-3d7cf84b4f42"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4c905cf6-7e03-4c85-85a8-848b13fa25d5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4e2c084d-a22b-4bb6-b019-40bf6eadb415"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4e50c98d-626e-4e8b-bebf-d4a9fe1df9fe"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4e7a0c20-0fc1-4a15-92e6-f717c0772c0c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4ee1d59b-0bc7-4229-bf24-1f51a17da8d0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/4f3f4346-1eba-4cc5-a441-d1612de7b851"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5008861d-ad18-450b-8efc-9cc3f153add9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/500f443a-cb0a-4f8e-a771-ebb092a79c88"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/50519c68-b73e-4aa5-98cc-bfa358790030"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5060320f-ff6d-4226-ac0d-e77f19d64e7e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/515bb4dc-682a-4d6e-abc0-8269939ae9ea"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/51c1ec49-34d6-4914-a526-1602e5fb2025"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/52647271-9209-481a-9f38-826ba4451604"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/526c1910-7556-4a4a-9132-26b88f21e970"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/52a4518f-9a0c-4301-9a06-b31495b4ef76"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/530f0bf2-d97d-4307-93b5-97bc22b957d1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/54c65b8c-ed2e-4cad-ac88-7d1d31357273"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/552a9efa-83ff-47b7-9f97-d4a7230af48a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5660f38a-54e3-463e-80d6-a17b3ed8f383"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/566a52fc-a368-4fcf-a7b5-bff2232dff03"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/57a6afa6-ff12-40f3-80dc-d5143db40a27"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/583d9347-2410-442a-9053-007c62899de4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/585282bd-3d5b-4dbe-9256-7d8ffccf48d4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/58541525-373a-4712-9c0e-5edc18730296"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/585d669a-b97a-49c1-b513-65a1e51fb9d4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/590b2eb2-129d-4f22-a564-dff75679e983"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5936dfba-0cec-476a-81b2-5c6350ae0f2a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/597f0413-2bc8-4415-ae4a-956d37845261"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/59ddfa9d-a4f1-472d-aebf-78a78e075da7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5abffc3d-782e-490e-9bf0-27a6725fa417"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5b2de84e-6d65-4017-95f2-0a0cb1cc5f4f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5ba3665d-0dd8-4e45-ae65-aed246bd4b65"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5bbc75d9-ce58-42f0-a9c6-9124cfaadd6f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5c2b4fb7-fd24-4d25-943f-c6876f5e0f4b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5c57f49f-1676-4329-9473-80dd3ec5c90f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5c8fbfc2-553a-4284-b823-21636e8d3750"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5ca0fc8d-a2d1-4696-a511-dbb7f7c4353e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5ce5cf4a-cb3a-4c38-ae30-56be69623853"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5e59afbc-519b-4aa9-a975-c7905ffe621e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5eecfc4e-b60d-4580-a358-d2fc53e6cc83"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5f7bf280-6020-4050-b621-65ec74fce082"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5f8060bf-a405-4862-92da-6281ca4fba21"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5fb6a8a0-b9a3-4736-ad79-45aa077680d0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/5fcf6c38-319b-46a2-a289-96e736227587"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/60277310-0fad-4dc7-bcfe-bb496fcbe5f4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6043b13f-e06e-4d72-9d40-240f0ceb335a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/60c76875-cd23-45de-ac3f-994ded7835bb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6134176a-b078-4465-b67a-cf6e599c8ce0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/61a9762e-14e9-4c4a-8381-9e52777d7033"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/623f3f6f-ac22-4940-8a10-7a21547eef6a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6268abb3-dac9-40df-bf5a-3b74892b2387"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/63efc742-3663-440e-b716-f01556e6eb2b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/641461a7-c899-45b9-a64c-91dc7e2613d6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6429aea1-bc8b-408d-a75c-10400a751e70"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/643627e0-362c-43ac-a508-59b38b15f81b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/646c37c6-817b-40d6-97af-42a87a407cb0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/64f0e427-7d48-4538-bf41-2806776dc3c8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/654ab509-86ab-4b77-bf85-5c0e154a59ec"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/654e745f-47b5-4d0a-a587-40be4f778f7b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/65574311-98a1-4971-a534-3afcf083df09"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6584045a-bce0-4f1f-b636-06b83d21b6b8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/658e23cd-72e5-489a-bbb2-9df2480e84e3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6718fd76-06bc-4396-a948-d2d5fe56ff70"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/671b5fd4-c1bd-454f-82e7-4a4ef8280dc2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6721acc5-a3ff-4fb5-9b2d-fa09f5d8d31e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/676830c1-d96c-4e90-9d0f-45c0ed5d94de"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6771af20-e176-44cd-955f-e31b76876407"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/67857c61-92f9-4bbe-a165-672ac30035bc"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/67e4207f-d5db-439f-adc9-1951016a7977"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/68288f7f-c07d-49ed-a427-5ed9cc027f57"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/69e4a6df-40ab-4ae7-883d-36cfb2e283ac"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6af5d2f0-fa3e-4111-858c-b59f0dfa7449"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6b316ddf-979e-40d8-9804-f2c57923d701"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6b847c4c-27bc-484c-b29f-8f82dae216a0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6c8fbacf-199c-4ce9-a60d-b0531fce1883"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6c92f5b8-6ba1-4cb6-b4a4-39475c9baab9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6ca76058-4a35-4578-bf40-e5600f08ab39"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6cd6cc44-a8f3-4c53-8a17-dda93bc456a9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6cf15c4c-9797-4e03-88d8-37151bff1532"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6dc5e230-9a8c-45be-9c44-356c4dd8232f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6e318879-8acb-4067-8a7b-56f26730798f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6ec29ae2-3bb7-424f-8916-31c8bf11b6b4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6f37db81-26a9-4201-a4fc-d4fdafedc1ce"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/6f91fe69-5d35-4523-94ce-9945f4f73ba2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/727b819a-5d73-4c2a-866f-57d2fd728eb0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/72859ed0-1155-4e74-adc7-0d935662bf72"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/74d483bb-c842-4323-95d2-9c1a8f27cd49"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/75b93986-305f-42b0-b18e-631f266f581a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/76470c81-c9f2-4078-8579-b73b2cfd0f56"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/769d1848-ef59-4e22-9db1-25e9db613d5d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/76e53d7b-4250-4cbb-9abd-bc2058d622aa"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/77a1fbb9-9c89-4a3b-acc0-2d4878f770b1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/77ba4fd0-4120-41c0-a87d-2aa43b44c85a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7825ee6d-2734-4e1a-ae09-3ba5ef293429"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/782a5609-80d1-41e2-b271-b3cf308671d6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/783d4bab-d140-484c-9ce0-2a35bf8868cc"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7859373a-47a9-443e-934d-e3e7b978d1e5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/78a1b4e1-063d-44dc-bf32-3cf727019657"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/791b5cc0-f9c5-4937-911c-ee81a04b4fb4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/797ffc2d-fb5b-44b5-98a3-9ff0671bc9fd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/79a3df74-9a73-41b4-be12-b9d073edc979"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/79e8d728-2045-41bc-8cb1-a6b8188a3e55"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7a3e3a8e-f86e-4284-b039-b7745c873bc5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7abdb2ba-0d15-4857-99e7-7314cdb0a0c7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7b54041f-08b5-45c5-9c91-a8f9e579fbd2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7b5a6c7a-3688-471c-9745-38616326ae77"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7bf29702-7109-489a-ad58-23b1462fff0d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7c002f7f-f9c5-41a1-9808-045e46762a45"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7c0d90c7-55a6-4447-866f-6d6241aa91ef"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7c767bea-7803-4964-b7ba-0aec54fc1a8f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7c7c1f84-4dd9-4c9a-96ce-33146746c9f7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7c7d8e02-a0e7-487d-a004-fdd452a533c6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7c8df09d-938f-4ab8-be07-2e331f271600"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7cd082f4-c369-4c30-a01e-c4f624baad94"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7d96d8f9-0107-4226-b71d-4b9649c2f592"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7da9f08b-80a5-41f3-8e18-22938d132d74"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7dd3dc1c-37c1-483d-959f-eabb00894604"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7dd8f650-3e7b-4776-aa42-a594a36bb164"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7df423c8-cd4a-487d-a434-e003b4d70bde"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7dfd13f1-7815-4023-b8f1-aa051a177acb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7e792617-085e-4a81-a115-6cf38b6b669f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7e9963a4-3e1d-4fdf-ad77-d04da43f6df5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7ebd3a9a-bc2e-4d84-b937-f98342334bba"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7f0d6180-e295-4266-b117-cfa64288c1cd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7f2aa7f3-e861-4804-a1df-57adecd56d9d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/7fcd1e1d-7b1d-49ac-b64c-5275ac8dd7a8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/806012bc-bf08-4102-9645-bf568d60a407"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/80d96559-d706-4019-a6f6-71b02ae1dd47"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8123311e-2ae6-4f2b-8c03-14c953a65089"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/81844b13-84b2-4351-a8ff-3b3ec5964cde"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/81c797f2-ad2c-45dd-991c-eb885f7c5812"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/820a335f-6739-4c80-a53f-e067699d8b42"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/821b85c1-ebfc-4ddd-ab31-a7a6d20797b4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/825e9f2c-93ff-45bf-acec-ff3385a0a8d5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/839fc439-0c08-4efa-b6dd-8c0051993e19"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/844b1684-27ea-4fea-a7ba-b42dfa555af4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/84670c8a-b81a-4b36-b1ae-d9cb251c6d14"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/866c52c9-b7a8-4af2-822b-a382783e245a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/86c4cf5f-8c51-4ab7-ab8c-569df8318b26"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/879c5cd7-00b0-498d-83e1-da03fafa5247"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/88fd4a69-1b74-4039-ade9-b315e72023b0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8926a5fb-d75b-4b08-b740-2845c697c3ed"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/893d749e-5e28-45d1-97e1-aeef290fc25d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8958dcbb-9c2e-4b37-9104-b2f4154c0dc1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/89e20d07-7f4a-4c42-b5bd-d51e46b84002"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8a0a7523-e220-4848-a4ff-b6581d59a58c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8a1be4b7-d2d9-4aa6-a1a4-42a9828b82ff"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8a9c03e6-d21f-40d6-8da4-ad98dae32bbd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8b78dc5b-695b-4a85-9deb-c126571cd93c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8ba5d190-9a2d-4ee6-9151-2049b47d01a4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8c5b5649-aeb1-43af-b263-8bc153a5ce37"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8cc43956-0c30-4b69-88af-9ae97c9034f2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8cefedd7-941b-496f-a44f-100a27553ecf"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8e036518-338a-4a7c-bbb7-95c7b5333f2f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8e6f2d18-7c6d-4693-8810-649692e1656e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8ec45fad-2243-48df-99cf-9df8cb4a01f8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8f26bbbc-8414-4d89-b07a-fc9c9a5fca72"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8ffa8065-a318-4b32-8651-3509653a105f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/8ffecc90-31cc-4129-9c89-b589398a3bf7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/901d63b9-6d56-4675-a68b-d164bb43d5a1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9124c59c-b98e-409d-af8e-48e891d95f4e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/91307d67-9b09-4dfc-9500-7b100666e7f9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/91308eb1-67ee-4ef6-8746-0aa069d01d57"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/913a5b3f-6a0d-4927-8bb9-8a55acacf3ca"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/91742947-7dcd-4e4c-84e0-6b668522ac3b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/91aa6cdc-6bdc-4245-831d-ae3d0203969f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9222862b-6c41-4d96-ac32-3d439f5da0fc"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9291460c-2189-482d-aad2-1d4fe72a52cb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9295e54b-008f-4d0b-9c9a-3d479003acb3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/92c2a24c-583d-41ed-ad8c-d01bfee64505"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/940deb0e-6708-41a3-8408-528029df5430"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/94b76bef-d4a5-4859-bb57-1ca84d4590c9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/94c7c24a-b001-472b-8b35-c4af4bc9904a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/953c78f1-b15c-44eb-ab6e-dad2e0c601b1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/95b33501-15d2-47da-8839-b40da06fe750"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/95e907b4-19c9-4e54-ac4d-a2140e24f0c0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/95ff40cf-f134-4821-bde2-67fe66f470b8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/962077b8-d835-4a69-95b6-59f0ece476d3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/962bf187-724d-48f5-906b-ab2aafc8bc7e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/96e92a64-4fa0-4b81-8a14-78ef8b1fdb76"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9711f6ea-a0d7-40dd-866c-82a5e67cc2bb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/973cd11f-c63f-4bfb-a691-b210798c6eef"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/979ef8b3-c5f8-44a5-b1a5-6273ec3e8a01"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/97ad867b-2dc8-48f7-b2c2-df68c89b8d7a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/97c6ab03-20c9-4684-887f-dd877d22769b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/97cc7acf-d099-43d3-be55-b99fae9d3c39"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/98f3029e-2f84-40e7-baab-4fc13893cd34"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9939d925-c9e2-447c-8232-e8a553532984"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/99a21be1-96d5-43b4-8a5a-66d4f3ef8ae9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/99e6998b-3455-4f02-9336-7c3ae666d2ab"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/99f65224-fcec-42db-af81-37dbe760c3e4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/99fac8a7-e19d-4bb5-adf7-b923baa67bed"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9a2e34c2-9ae5-4ee6-a9d7-55964ebb14ae"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9bb96818-3d3f-46a5-9f8b-8ce48c7992f2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9bed024d-03a8-4977-ad91-7cb75edac67f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9c57aa8c-cc88-4543-8846-19e31c2a6b2b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9cebd0fc-e41e-447d-ae14-6e62cb583bb2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9d26b6a3-902e-4013-bd96-dca1d7886de2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9db4c711-5bc2-4ef2-ba75-69b427ab94da"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9dfead3d-fc69-46ce-8759-8c1dd9998e3c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9e0ba8f3-55d9-4629-b85a-5531904c487d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9e1cdc59-2878-416b-99d1-9d9cdf857b81"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9e46949e-b9bc-44d0-82dd-2ab5ec78a6bf"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/9e7734af-943c-4f99-b75d-9e1b90706685"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a138e86e-f3ba-4276-b64d-de939e331d58"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a1881cf6-17ce-40bb-bf50-86ca29e58ef9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a189e5a2-8721-4235-89a6-caa3ce5c8b3b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a18a309e-76a2-492f-864c-0cd4af9864d2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a1ad85f0-f19f-4a5d-b2f6-ba3e59f096e2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a1fcbcd2-5545-49c9-a97c-51b7adf73b42"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a324958a-b937-4b85-a151-193ed60835f1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a46c6c70-a155-42e5-8289-e6d3eb52af67"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a4b61e2f-e44b-43e8-8eca-9b899c04e202"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a4d32bb8-02e3-4cc7-beb1-b0bde09a58b9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a4dc7903-9da7-4578-9cf8-97f4c630eab4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a5052c9b-3230-4075-b4df-07b1ae7a1679"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a5a3e6f6-13f8-4be0-808f-00c3fc3760a0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a61f49d5-bf33-4640-b5a3-ef4b42cafc15"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a671d4ad-fc7d-456b-8921-1ae67ba8106c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a707a020-8b01-478b-9993-3b1cc5d61f9c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a7911397-bbf5-48a7-8be9-d4df43cd53e6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a7ab5da7-6b42-429c-9255-154da2758e9c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a7fbc430-57a6-417d-9c8c-7d59be213f7c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a8192a17-d0fd-4b90-a80a-9105da9e1b1f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a84d75ec-1fad-47aa-ac66-aedae7567afb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a86180d8-e365-40be-90a7-187aa0a18554"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a883569b-98cb-47fa-844f-5df381caeda6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a8b66c11-e35d-4690-92b9-cbf0d19a7f83"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a8f2f26c-82cb-489a-a88e-c7f30d5f9a31"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a8f47925-44fb-4fa5-9e09-353ce8370b8c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a90e940d-35be-4684-a7e6-f0e87e60399e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a91ebe47-21e2-4aec-ad17-c4878d0522a5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a922fbcc-4226-4771-aa93-1c48aa692ed6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/a9c17195-66de-469b-8204-71a93c744426"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/aa26675a-5a9e-4005-ba63-f1bb5695038d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/aa4e50ea-a2ec-4b9a-b059-c3f0a78e8985"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/aacc4bce-7a06-4d36-a32a-a062ed34695f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ab3c1679-8770-476f-af21-230371e60c76"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ab76aa1e-f505-4488-a6ef-cb81d975292e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ac7864d9-aca6-4913-97d2-cc483c99e5b9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ad4f6761-d5db-4967-8bf0-5d404e4515d6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ad5a5788-0852-429a-a474-6f7aaa29d6e5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ad90acea-1ead-49cd-af58-9aa135f7eb88"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/adbaa058-e82e-4efa-81bd-d13ca7e28e1c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ae3a83ff-ca7c-4e86-b5fb-d58c38627ce0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/aee73d63-8af2-4810-89d3-44b562fa66ba"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/af1f3b2e-bb0f-462e-a543-9a135b637647"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/af6a2877-ba14-4880-b984-50259a0f3a44"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/af78a1db-c643-4f4e-a947-40a02f764372"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/aff5b970-e380-4f17-a32c-904b1a8a61f2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b0a0cf9f-c53f-42fe-bf33-4c6b010c47e1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b0ed5225-c3e0-479a-ac28-57bb5c488096"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b117ebd7-27c5-4eeb-8a75-47e5b19f144a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b12727e5-1f87-46ed-b0e8-df5b1e5699ea"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b15ad520-2632-4dda-94bc-d1c27c40b47a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b35c2f3c-2cd9-43ff-afa7-16aedd2cc3db"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b39f7256-bd05-4f72-9d10-4702bbe482f3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b3fb9c84-b16c-4db8-8430-2bed89b0ce16"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b44495e1-5758-473f-a822-8a4f6784b0a7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b5591221-cdf2-48c9-90c9-f12a7bdeaab8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b56a656f-bca4-4963-bbff-131f9ed2ca5d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b5a9902b-279e-4e82-8b42-056507282686"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b6314b4d-41f4-4f9f-84a5-2ea87255f8eb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b67a7842-0a3e-4d47-a130-ef502376d0aa"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b726ab4b-81da-40bc-96cf-79546fb8092e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b79308a3-248c-481a-a580-e4caddb33967"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b7b32e9c-ca5e-4c90-a4d2-69f6f6d0ea6a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b804a98a-8d6b-40be-afe1-371ffb15bbd6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b81c96a3-04db-41f6-87fb-cf3ddfb31938"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b82be41a-2033-4255-8f48-bcb38ea366d2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b8966109-5213-4eec-a1b7-bb754fd87906"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b90a281b-754c-4a38-a148-5b0d5617ff9a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b912a237-4050-4066-bc5c-e064b9a5439c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/b9a03a49-0cea-4f28-aa51-5ab63f1a7f9f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/baf25214-fe44-4862-bc1b-2bd5e1658e70"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bb422341-5c52-4431-8195-475e58165383"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bbd4736c-da67-45e9-b02e-9afe6e21b29e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bc0a50a7-4184-41f0-87e3-bf7d7ec559cd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bd3f9442-07a4-4c5a-83a8-a9458618ef5c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bd5469de-8903-4ca9-9842-c4d96ef3beba"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bdfea9a5-adf6-4c4b-a6e9-a4f8c7e47dc3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/be6a8fc4-1cca-49e3-9a4d-f2497bad6c1a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bea98776-5c96-454d-ab86-214ee2c37d52"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/beda4b8e-20b7-406f-9e37-1cab43cf2a4d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bf0766a7-e95a-49ec-86dc-7ec7cb4134fe"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bf37fef7-693b-43ca-9398-fc1c0953257d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bf5620a3-c5c5-4d5c-b48e-74406a826ea2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/bfe2d0b4-a9ab-4287-a747-e4a58ac1406a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c03b9f2e-9757-46cd-932a-1587d96cf437"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c06bcb47-0c03-4b44-9529-9b29e24baad2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c0c29c0f-8659-47bd-add6-18cfbfda0e4c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c15a483e-c534-45b7-bc8e-1551fe32792a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c169d81a-c510-4724-abc6-c02a631c1049"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c17b5efb-ce77-4020-a755-47fb708d2471"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c1dffb67-bdcd-4c95-af2a-5eb59d7cc14a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c25007de-4e01-47d1-94dc-22984647e4de"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c365b5fd-a308-4aca-b915-00fe4deeec6e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c3d4f6b9-dae0-486f-b070-f624529fc9b8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c3e98974-9527-492c-a032-6b7c8a1fcf07"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c43265ca-0d27-411a-94e4-8571e25d4fe5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c43fb0bc-f58a-46f3-b2bc-ff1a8309ef3c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c473ff53-4e75-4aef-83d9-f650cbe44d86"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c4bc322b-e4e3-4f52-a68d-0cbeccc9bf0c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c4e73cdb-7c2d-4522-91fa-b29aeaed3f52"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c4e8fdc5-a72c-4989-aa0d-642bc7a00b38"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c5c3e9d6-9083-490f-9c29-5d97b7531cb3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c6522786-c13e-4847-b094-2b2e836c8d7c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c65a3f1b-b82e-4066-a1c9-1344393354ca"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c712343c-1e75-40e5-ae26-52cf9d34083b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c72414c6-4d83-4d48-abea-9d03a3da13e8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c817d812-2c9a-486e-9818-157522302d56"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c866fc61-89d4-48c8-8237-b145fcb680a2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c879c2d3-07c2-46b4-a531-0f726cd53351"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c8acb736-2b9b-4c4b-b748-fe7513827047"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c8b0e518-fc91-40e6-b115-dbf0221c386e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c9523047-beae-4850-b4d1-ee5e5bbf2799"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c9652b01-d30c-4379-937e-bfde8e376794"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c9c8b2a1-35e4-4297-b5cb-66c1832a3663"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/c9f4909c-31e8-4a37-95c5-911ffbe6ba21"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ca0ed959-1e2e-423c-b339-f5ac664d22f7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cbc5e4c2-d6dc-45ec-9912-cbe9b5624f44"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cbd140de-376f-4186-be4c-ac3979d0aa99"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cc3342a4-5012-4d97-80ed-f0ebb0e44846"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cc8019ab-7fdd-4f0d-a56a-4b1b2d160095"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ccabc3c4-0f83-4d4f-832f-46aa3a679666"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ccb8bcc2-0b4e-402f-9018-2bab0cb22f38"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ccbeb6e1-959b-4165-b46d-546963767af6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cd6f42f6-5099-4abe-bb4f-d83c93cef671"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ceb72ecc-eda6-4341-b42d-57955e9d1d23"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cec7f76e-10d6-45d1-83f5-d1b47d9656cb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cedf4bf0-288e-4401-a06e-0af27a17dacb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ceec58c6-2363-4054-a888-6e1473b9f6d2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/cf8a320b-c9bf-429c-b5e5-d94e4181db68"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d0255782-76ef-45d8-b847-4ab3438a9b15"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d0336670-0af1-41e5-a33d-79147eb6e8a4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d0422563-a181-49c0-961c-fd572c05af31"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d065c639-f2f4-4e3a-8b4b-080d8e99f117"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d0a94397-9d78-44c9-b804-a7a257a34184"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d0c92149-633e-4cad-8fc2-559fb969c0c8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d183173f-9a3a-4127-8562-9b2656344d35"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d184fdcb-886b-4403-b897-48e965c5bb76"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d1887f4f-7204-4278-9d5e-f025d04edb9c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d231f176-f95e-4dbb-bee2-e6a849b38354"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d2442a31-4d1f-4612-ad3b-6ed17099efcb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d2a0a874-242a-4090-845a-68e68fe8e74b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d2dc2496-38bf-4de6-b75c-8b7efb9a6c67"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d3481b23-0990-4636-936a-db46d931f3ba"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d3b3b566-5a22-480a-88ac-c949e877e7f4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d3c66dcf-a2e4-4a8e-9038-6957c8e16dc1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d3ce02e2-e35f-4430-82a2-d37beb475113"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d456a5ec-ebcf-4c47-9353-65a1175cc0c7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d4bf809c-97a6-444c-aeec-43cd17edbd6f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d52679e5-7531-4014-80a6-5ea73683c3ba"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d56fea76-8648-4a0f-bb31-711e3121375a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d579b35c-4c3c-47d5-85f6-bc20deb49c9c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d586da2d-4bb5-4323-b331-1642ba2c9406"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d66f7880-3061-441c-9398-6574e3f109cb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d6905d37-86a1-4b84-bb93-bccf8c4fe6ed"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d6d6ca04-0607-4730-90e0-f19e3116641a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d6f7f019-f94d-483d-8506-8c393d91a9c7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d74782b6-eb37-4bc4-974f-89081df046ae"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d74b74b3-1645-404d-9292-337f34e44d37"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d758b04d-a138-4e78-881f-b44eb55a83c5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d75d88aa-3f8d-454c-95e2-73d0fe061a4d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d7b1c058-b515-4f5c-89bd-4ea07f2c53f0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d977a503-f136-4b10-9852-8a9f9a9fb238"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/d98a12d7-ad67-475e-980f-d66866d56f53"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/da1ddce1-bc35-476c-bb13-fcf4fb39497b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/da7424b7-3aa0-4a73-88b7-ac8631e16562"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/db16b10a-0245-4cdb-bc70-56ae466a8a97"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/dba5f494-ecb7-4ea3-8218-48e8d37ddc4e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/dc476f9b-8eeb-4bfb-95f1-e96fbef680ed"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/dcc6d10f-0333-4108-90de-34404c573bc0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/dd88a2fb-faf8-4c0e-b883-a063e8e733f6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/de133c0b-a208-4b58-9364-fa5952cf0a77"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/dfd0cccb-6326-45d0-857c-f3d97e7234d5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e06ddc0c-5c1a-48c5-9ca1-615bd01102d8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e08b473a-8dde-400a-9d5e-cd07cd8519d5"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e0ce5dc5-8288-46d5-bbdf-4452752c5f57"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e15ed050-a4ed-4137-bcb0-e1171b27f871"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e1cacf40-0778-4119-9e9f-c4ff9f7725bd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e25d495a-49f3-4cb5-bbfb-b2cb841f3122"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e3022fd5-3d69-4c11-ace5-949ee8b37b21"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e30235a2-9f70-4934-ac51-8f7fd6509548"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e383e0b5-ffc2-4edc-9110-c8c0ce502649"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e385ffb2-a158-4c0f-b2c3-d6b4d3269e94"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e6b90b89-ea7d-411c-abe1-a01388a5c0b0"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e6ba7875-8950-4f44-87db-10732063f4b2"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e8b53a90-52b8-439f-ac3b-7573b8fa94a7"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e91bb895-376b-42fe-8822-54c0cd6e49c9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e942b1d2-766b-41ec-9934-1f4f47f62208"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e957735e-9a52-4d55-b11e-29d7d9883816"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/e9708853-cebe-438a-9212-88d58a0e4bcb"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ea5cb38b-c105-4c2d-bae7-e3afad7a2f1f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ea854ed9-14cb-4e12-8c72-38e5bcad190b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/eab1eccc-2084-44be-94d1-90ea6de56e6e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/eb0c788d-80e1-4a4f-86b4-6a624676af5f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/eba17ba0-890c-494b-abe2-b5fb983fad0f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ec1b80cf-4da7-497e-90d6-82f1041c815d"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ed11d633-2794-46d4-a0d7-babc8e6ff35a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/edc4019c-2366-4371-aeaf-2094e7af2073"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ee301a3a-b35d-4112-a5f7-ceab62ddb075"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ef3dc195-ac3d-4d1c-afd7-3c8294fdadfa"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ef9bd514-158b-4421-8766-7ed71aa93968"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/efc7fa50-9471-415c-9243-7e47f737a978"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f10e585c-73fd-4733-89ec-87a0ff39c38a"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f195beec-95b9-40a3-bc93-ffdbb9cc842c"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f1ccf46d-af40-44b7-925a-34576acf24d8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f46fb3e5-4e64-40cc-994c-36f74e098325"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f4d246e5-d09b-404f-b9d2-b79e9f70ad54"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f5afd47f-6d73-4e4a-b67b-cae0855e6604"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f5e7bf43-04ad-41c7-92f8-b9cd287eed75"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f711cce8-d6b0-43da-b1bb-2ac7a1e748d1"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f7ec32ee-0108-43c6-a3f1-40f69297303e"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f816676b-fbba-4b70-ad41-a3c94fa03ac6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f8354ce1-d3da-4a68-9863-cff35b2f56fc"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f8bd322d-3205-4409-88cf-2081607a1ba4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f982a3ca-c06b-4cf5-9b79-0907545d01d8"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f98f653e-b25a-4c91-9265-b58779c8e16f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/f9b9ed9a-fd2c-434f-a41f-4affa750e9c9"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/faa6885e-08aa-4c8f-9fe1-a6d9cedc4515"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fb34f19b-da1c-4a5a-bb5e-5a89ebd71569"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fbd40cdc-e0ab-4d76-8b8a-911bb946dc3b"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fbdf0b81-bbff-48f1-b2b1-5b06b706c3d3"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fd03cfef-5e55-42b7-b476-e3f2f42de0a6"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fd1b8497-c0e5-4f82-81cc-1c813b4db974"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fd8229f0-38d6-48a1-96d1-af9cfd2065b4"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fdf1ddff-ae66-4d4f-95f2-05ddff95fd3f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fe50cb3b-e5af-4f3f-a3ba-2b9ebef2cb8f"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/fede0d3d-bf2e-4d1b-894b-a3b1d4d894bd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ff5a11d4-f6e6-440c-8232-d4aaa9621cbd"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ffb813cb-d720-4e1e-9052-a435bca47c99"
,"http://cdn.marketplaceimages.windowsphone.com/v8/images/ffbf9806-b562-4b38-8418-3855bc3b3ae3"
,"http://ja.wikipedia.org/wiki/"
,"http://ldc1sn2b.ps.adc.glbdns.microsoft.com"
,"http://pscaptest.adc.glbdns.microsoft.com"
,"http://pscoxlite.adc.glbdns.microsoft.com,http://psbnxl1.adc.glbdns.microsoft.com,http://pschxl1.adc.glbdns.microsoft.com,http://pssgxl1.adc.glbdns.microsoft.com"
,"http://sg1.ps.adc.glbdns.microsoft.com"
,"http://support.microsoft.com/kb/"
,"http://wiki.answers.com/"
,"http://www.amazon.com/"
,"http://www.bizrate.com/"
,"http://www.chacha.com/"
,"http://www.drugs.com/"
,"http://www.ebay.com/itm/"
,"http://www.ebay.com/sch/"
,"http://www.ehow.com/"
,"http://www.fixya.com/"
,"http://www.healthgrades.com/"
,"http://www.homedepot.com/"
,"http://www.imdb.com/"
,"http://www.indeed.com/"
,"http://www.legacy.com/"
,"http://www.linkedin.com/"
,"http://www.livestrong.com/"
,"http://www.lowes.com/"
,"http://www.nextag.com/"
,"http://www.realtor.com/"
,"http://www.sears.com/"
,"http://www.superpages.com/"
,"http://www.tripadvisor.com/Hotel_Review?geo"
,"http://www.trulia.com/"
,"http://www.walmart.com/"
,"http://www.weather.com/"
,"http://www.weibo.com,http://t.qq.com,http://tieba.baidu.com"
,"http://www.whitepages.com/"
,"http://www.youtube.com/watch"
,"http://www.zillow.com/"
,"http://xlitefork.adc.glbdns.microsoft.com"
,"httpqas-pub.httpqas-prod-co4.co4.ap.gbl"
,"https://mobile.twitter.com/{0}"
,"https://tse{0}.mm.bing.net/th?id"
,"https://westus2.tts-frontend.speech.microsoft.com/synthesize/android"
,"https://westus2.tts-frontend.speech.microsoft.com/synthesize/wp8.1"
,"https://www.bing.com/images/blob?bcid"
,"https://www.bing.com/th?id"
,"https://www.kohls.com?auto_show_edge_shopping_flyout"
,"indxbnemealdc1.adc.glbdns2.microsoft.com:500,indxbnemealdc2.adc.glbdns2.microsoft.com:500"
,"indxbnldc1.adc.glbdns2.microsoft.com:500,indxbnldc2.adc.glbdns2.microsoft.com:500"
,"indxchldc1.adc.glbdns2.microsoft.com:500,indxchldc2.adc.glbdns2.microsoft.com:500"
,"indxcoapacldc1.adc.glbdns2.microsoft.com:500,indxcoapacldc2.adc.glbdns2.microsoft.com:500"
,"indxcoldc1.adc.glbdns2.microsoft.com:500,indxcoldc2.adc.glbdns2.microsoft.com:500"
,"indxdbldc1.adc.glbdns2.microsoft.com:500,indxdbldc2.adc.glbdns2.microsoft.com:500"
,"indxhkldc1.adc.glbdns2.microsoft.com:500,indxhkldc2.adc.glbdns2.microsoft.com:500"
,"indxpuseldc1.indexa.trafficmanager.net:500,indxpuseldc2.indexa.trafficmanager.net:500"
,"indxxlite.adc.glbdns2.microsoft.com:1000"
,"lightgc-backend.binggeospatial.com"
,"lightgc-backend.ppe.co.binggeospatial.com"
,"Local.Configuration.LocationExtractionSuppressorConfig"
,"Multimedia.LayoutPreference.QasDomainRule"
,"Multimedia.LayoutPreference.TriggerCondition"
,"Multimedia.LayoutPreference.TriggerConditions"
,"None_1:VIP-TC-MagpieBridge.CACHE-PROD-CO01.CO01.ap.gbl"
,"ObjectStoreMultiBE.Prod.CO.BingInternal.com"
,"OpinionSummary.Places.ZhCN"
,"PAA_1:BRAINWAVE-EXP-VIP.HAASPool-Prod-Bn2.Bn2.ap.gbl"
,"PeopleAlsoAskDlis.paarelclassifierv0_1:WestUS2BE.bing.prod.dlis.binginternal.com"
,"PeopleAlsoAskDlis.ranking_1:WestUS2BE.bing.prod.dlis.binginternal.com"
,"PeopleAlsoAskDlis.relevance_v1_1:WestUS2BE.bing.prod.dlis.binginternal.com"
,"PeopleAlsoAskNews.rank-t4-32_1:WestUS2BE.bing.prod.dlis.binginternal.com"
,"pl.tripadvisor.com"
,"ppe-api.msn.com"
,"ps1.adc.glbdns2.microsoft.com,ps2.adc.glbdns2.microsoft.com,ps3.adc.glbdns2.microsoft.com,ps4.adc.glbdns2.microsoft.com,ps5.adc.glbdns2.microsoft.com,psapac.adc.glbdns2.microsoft.com,psemea.adc.glbdns2.microsoft.com,psxlite.adc.glbdns2.microsoft.com,ps6.adc.glbdns2.microsoft.com"
,"sg1.cache.binginternal.com"
,"SocialNav,OGConcept,SchemaOrg,JsonLD,baike.baidu.com,zhcnTopDomain"
,"sp1.tf.360.cn"
,"th.tripadvisor.com"
,"ttsincubation-2.cloudapp.net"
,"udss.co3.glbdns2.microsoft.com"
,"vimeo.com,dailymotion.com,hulu.com,imdb.com,nbc.com,youtube.com"
,"watchtower.prod.wip.glbdns2.microsoft.com"
,"wit.agiencoderv4en_1:WestUS2BE.bing.prod.dlis.binginternal.com"
,"www.amazon.,www.ebay.com,www.bestbuy.com,www.walmart.com,www.target.com,shop.totalwireless.com,buy.gazelle.com,swappa.com,www.snapdeal.com,www.flipkart.com"
,"www.azlyrics.com:true,www.classic-country-song-lyrics.com:true,www.cowboylyrics.com:true,www.directlyrics.com:true,www.elyrics.net:true,www.lyrics.com:true,www.lyrics007.com:true,www.lyricsbox.com:true,www.lyricsmode.com:true,www.lyricstop.com:true,www.lyriczz.com:true,www.lyrster.com:true,www.metrolyrics.com:true,www.pop.genius.com:true,www.rap.genius.com:true,www.rock.genius.com:true,www.sing365.com:true,www.songfacts.com:true,www.songlyrics.com:true,www.songmeanings.com:true,www.urbanlyrics.com:true,www.lyricsfreak.com:true,www.onlylyrics.com:true,www.lyricsmania.com:true,www.lyricsty.com:true,www.lyricsbay.com :true,www.gugalyrics.com:true,www.justsomelyrics.com:true,www.musicsonglyrics.com:true,www.leoslyrics.com:true,www.lyricsdepot.com:true,www.oldielyrics.com:true,www.genius.com:true,www.songonlyrics.com:true,www.artists.letssingit.com:true,www.lyricsondemand.com:true"
,"www.microsoft.com/en-us/"
,"www.msn.cn"
,"www.ndtv.com,stackoverflow.com"
,"www.tripadvisor.at"
,"www.tripadvisor.be"
,"www.tripadvisor.ca"
,"www.tripadvisor.ch"
,"www.tripadvisor.cl"
,"www.tripadvisor.co.hu"
,"www.tripadvisor.co.id"
,"www.tripadvisor.co.kr"
,"www.tripadvisor.co.nz"
,"www.tripadvisor.co.uk"
,"www.tripadvisor.co.za"
,"www.tripadvisor.com.ar"
,"www.tripadvisor.com.au"
,"www.tripadvisor.com.br"
,"www.tripadvisor.com.gr"
,"www.tripadvisor.com.mx"
,"www.tripadvisor.com.my"
,"www.tripadvisor.com.ph"
,"www.tripadvisor.com.sg"
,"www.tripadvisor.com.tr"
,"www.tripadvisor.com.tw"
,"www.tripadvisor.com.vn"
,"www.tripadvisor.cz"
,"www.tripadvisor.de"
,"www.tripadvisor.dk"
,"www.tripadvisor.es"
,"www.tripadvisor.fi"
,"www.tripadvisor.fr"
,"www.tripadvisor.ie"
,"www.tripadvisor.in"
,"www.tripadvisor.it"
,"www.tripadvisor.jp"
,"www.tripadvisor.nl"
,"www.tripadvisor.pt"
,"www.tripadvisor.rs"
,"www.tripadvisor.ru"
,"www.tripadvisor.se"
,"www.tripadvisor.sk"
,"youtube.com,dailymotion.com,vimeo.com"
,"youtube.com,dailymotion.com,vimeo.com,myspace.com,wn.com"
,"youtube.com,wikihow.com"
,"youtube.com,wikihow.com,dailymotion.com,vimeo.com"};*/



/*private static List<string> FindAllIpList(string dir)
{
    var lines = new HashSet<string>();
    Parallel.ForEach(Directory.GetFiles(dir), file =>
    {
        Parallel.ForEach(File.ReadAllLines(file), line =>
        {
            if (line.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})");
                if (match != null && !string.IsNullOrWhiteSpace(match.Value))
                {
                    lock (lines)
                    {
                        lines.Add(match.Value.Trim());
                    }
                }
            }
        });
    });


    Console.WriteLine(string.Join(',', lines));
    return new List<string>(lines);
}

private static Dictionary<string, string> FindAllOutboundEnvironmentListFromFolder(string dir)
{
    var distinceResult = new HashSet<string>();
    var result = new Dictionary<string, string>();
    Parallel.ForEach(Directory.GetFiles(dir), file =>
    {
        var lines = new HashSet<string>();
        Parallel.ForEach(File.ReadAllLines(file), line =>
        {
            if (line.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})");
                if (match != null && !string.IsNullOrWhiteSpace(match.Value))
                {
                    lock (lines)
                    {
                        lines.Add(match.Value.Trim());
                    }
                }
                else if(line.Contains(".gbl", StringComparison.OrdinalIgnoreCase))
                {
                    lock (lines)
                    {
                        lines.Add(line.Split('=')[1].Trim());
                    }
                }
                else if (line.Contains(".microsoft.com", StringComparison.OrdinalIgnoreCase))
                {
                    lock (lines)
                    {
                        lines.Add(line.Split('=')[1].Trim());
                        //Console.WriteLine(line.Split('=')[1].Trim());
                    }
                }
                else if (line.Contains(".binginternal.com", StringComparison.OrdinalIgnoreCase))
                {
                    lock (lines)
                    {
                        lines.Add(line.Split('=')[1].Trim());
                    }
                }
                else if (line.Contains(".com", StringComparison.OrdinalIgnoreCase))
                {
                    lock (lines)
                    {
                        lines.Add(line.Split('=')[1].Trim());
                        //Console.WriteLine(line.Split('=')[1].Trim());
                    }
                }
                else if (line.Split('=')[1].Contains(".", StringComparison.OrdinalIgnoreCase))
                {
                    lock (lines)
                    {
                        lines.Add(line.Split('=')[1].Trim());
                        //Console.WriteLine(line.Split('=')[1].Trim());
                    }
                }
            }
            else if (line.Contains("=") && ValueList.Contains(line.Split('=')[1].Trim()))
            {
                lock (lines)
                {
                    lines.Add(line.Split('=')[1].Trim());
                }
            }
            else if(!line.Contains("$", StringComparison.OrdinalIgnoreCase) 
                        && line.Contains("=", StringComparison.OrdinalIgnoreCase)
                        && !line.Trim().StartsWith(";", StringComparison.OrdinalIgnoreCase))
            {
                var value = line.Split('=')[1];
                if(value.ToCharArray().Count(o=>o == '.') > 1)
                {
                    lock(distinceResult)
                    {
                        distinceResult.Add(value);
                    }
                }
            }
        });
        if(lines.Any())
        {
            lock (result)
            {
                result.TryAdd(Path.GetFileName(file), string.Join(',', lines));
            }
        }
    });

    Console.WriteLine(String.Join("\r\n", distinceResult));
    return result;
}


private static List<string> FindAllEnvList(string dir)
{
    var lines = new HashSet<string>();
    Parallel.ForEach(Directory.GetFiles(dir), file =>
    {
        Parallel.ForEach(File.ReadAllLines(file), line =>
        {
            if (line.Contains("Xap-Prod-EastAsia--Group", StringComparison.OrdinalIgnoreCase) &&
                line.Contains(".gbl", StringComparison.OrdinalIgnoreCase))
            {
                lock (lines)
                {
                    lines.Add(line.Split('=')[1].Trim());
                    Console.WriteLine(line);
                }
            }
        });
    });

    return new List<string>(lines);
}

private static List<string> FindAllNotMigratedPluginConfigFilesForObjectStore(string dir)
{
    var fileList = new List<string>();
    Parallel.ForEach(Directory.GetFiles(dir), file =>
    {
        Console.WriteLine($"Parsing {file}...");
        var content = File.ReadAllText(file);
        if (content.Contains("objectstoremulti.prod.hk.binginternal.com", StringComparison.OrdinalIgnoreCase)
                || content.Contains("OBJECTSTOREREPL.OBJECTSTOREMULTI-PROD-HK2.HK2.AP.PHX.GBL", StringComparison.OrdinalIgnoreCase)
                || content.Contains("OBJECTSTORE.OBJECTSTOREMULTI-PROD-HK2.HK2.AP.PHX.GBL", StringComparison.OrdinalIgnoreCase)
                || content.Contains("OBJECTSTOREMULTIBE.PROD.HK.binginternal.com", StringComparison.OrdinalIgnoreCase)
        )
        {
            lock (fileList)
            {
                fileList.Add(Path.GetFileName(file));
            }
        }
    });

    return fileList;
}*/