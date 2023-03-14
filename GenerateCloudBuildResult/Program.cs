namespace GenerateCloudBuildResult
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            var file = XDocument.Load("BuildOutputManifest.xml");
            Parallel.ForEach(file.Descendants("Pack"), o =>
                                                       {
                                                           string packId;
                                                           lock (o)
                                                           {
                                                               packId = o.Descendants("Id").FirstOrDefault()?.Value;
                                                               o.Descendants("InCR").FirstOrDefault()?.SetValue("false");
                                                           }

                                                           if (!string.IsNullOrWhiteSpace(packId))
                                                           {
                                                               var root = @"K:\Ixp\ResourcePacks\ApplicationHostData\RCache\";
                                                               var resourcePath = Path.Combine(root, $"R_{packId}");

                                                               if (Directory.Exists(resourcePath))
                                                               {
                                                                   CopyDirectory(resourcePath, Path.Combine(Directory.GetCurrentDirectory(), "ResourcePacks", $"R_{packId}"));
                                                               }
                                                               else
                                                               {
                                                                   Console.WriteLine($"'{resourcePath}' can't be found!");
                                                               }
                                                           }
                                                       });
            file.Save("BuildOutputManifest.xml");
        }

        static void CopyDirectory(string srcDir, string tgtDir)
        {
            DirectoryInfo source = new DirectoryInfo(srcDir);
            DirectoryInfo target = new DirectoryInfo(tgtDir);

            if (!source.Exists)
            {
                return;
            }

            if (!target.Exists)
            {
                target.Create();
            }

            FileInfo[] files = source.GetFiles();

            for (int i = 0; i < files.Length; i++)
            {
                File.Copy(files[i].FullName, target.FullName + @"\" + files[i].Name, true);
            }

            DirectoryInfo[] dirs = source.GetDirectories();

            for (int j = 0; j < dirs.Length; j++)
            {
                CopyDirectory(dirs[j].FullName, target.FullName + @"\" + dirs[j].Name);
            }
        }
    }
}
