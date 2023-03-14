/*var packageConfigDic = new Dictionary<string, string>();
var packageConfigDicClone = new Dictionary<string, string>();
var packagehashset = new HashSet<string>();
var resourcehashset = new HashSet<string>();

foreach (var line in File.ReadAllLines(@"D:\temp\config.txt"))
{
    var parts = line.Split('\t');
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

    lines.Add($"{item.Key}\t{item.Value}\t{owner}");
}

File.WriteAllLines(@"D:\temp\NewPackageList.txt", lines);*/

var packageConfigDic = new Dictionary<string, Tuple<string,string>>();
foreach (var line in File.ReadAllLines(@"D:\temp\NewPackageList.txt"))
{
    var parts = line.Split('\t');
    packageConfigDic.Add(parts[0], new Tuple<string, string>(parts[1], parts[2]));
}

List<string[]> result = new List<string[]>();
Dictionary<string, string[]> dic = new Dictionary<string, string[]>();

foreach (var line in File.ReadLines(@"D:\temp\DetailedPackage.txt"))
{
    var values = line.Split("\t");
    result.Add(values);

    dic.Add(values[1], values);
}

foreach(var item in packageConfigDic)
{
    if(dic.ContainsKey(item.Key))
    {
        dic[item.Key][9] = item.Value.Item1;
    }
    else
    {
        result.Add(new string[] { "200", item.Key, String.Empty, String.Empty, String.Empty, String.Empty, item.Value.Item2, String.Empty, String.Empty, item.Value.Item1 });
    }
}

File.WriteAllLines(@"D:\temp\UpdatedPackageConfig.txt", result.Select(o => String.Join('\t', o)));
