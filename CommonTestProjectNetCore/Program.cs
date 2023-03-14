namespace CommonTestProject
{
    using CommonTestProjectNetCore;
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime;

    enum TestEnum
    {
        Value1,
        Value2,
        Value3,
    }

    public class PluginContext
    {
        public TestClass PluginService { get; set; }
    }

    public class TestClass
    {
        public ClassMember Member1 { get; set; }
        public ClassMember Member2 { get; set; }

        public ClassMember Member3 { get; set; }
        public string Property1 { get; set; }
        public string Property2 { get; set; }
    }

    public class ClassMember
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    
    public class TempClass
    {
        public TestClass PluginService { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Environment.ProcessorCount);
            Parallel.For(0, int.Parse(args[0]),
                (i) =>
                {
                    while(true)
                    {
                        var list = Enumerable.Range(0, 1000).ToList();
                        //Console.WriteLine($"{i}: {list.Count}");
                    }
                });

            /*IEnumerable<string> list = new List<string>() { "1", "2", "3", "4", "5", "6", "7" };
            ushort index = 0;

            while(true)
            {
                var i = index % list.Count();
                Console.WriteLine(index + "->" + list.ElementAt(i));
                index += 10000;
            }*/

            /*GC.Collect(0);
            Thread.Sleep(1000);
            Console.WriteLine("Gen0:" + GC.CollectionCount(0));
            Console.WriteLine("Gen1:" + GC.CollectionCount(1));
            //Console.WriteLine(GCSettings.IsServerGC);
            //TestExperimentCompare();

            //TestPropertyGC();

            //TestSerialization();*/

            Console.ReadLine();
        }

        private static void TestSerialization()
        {
            var serializer = new DataSerializer<ExperimentNodeTimeoutInfo>();
            using (var serializedStream = File.OpenRead(@"C:\Users\liagao\AppData\Local\Temp\tmp9688.tmp\dynamictimeoutdata_mdm.xml"))
            //using (var serializedStream = File.OpenRead(@"C:\Users\liagao\AppData\Local\Temp\tmp2165.tmp\dynamictimeoutdata_mdm.xml"))
            {
                serializedStream.Position = 0;
                var expNodeTimeoutInfo = serializer.DeserializeFromXml(serializedStream);
                if (expNodeTimeoutInfo != null)
                {
                    var experimentsNodeTimeoutInfoList = new List<ExperimentNodeTimeoutInfo>() { expNodeTimeoutInfo };
                    
                }
            }
        }

        private static void TestPropertyGC()
        {
            GC.TryStartNoGCRegion(10, true);
            const int LoopCount = 10000000;
            List<PluginContext> list = new List<PluginContext>(LoopCount);

            for (int i = 0; i < LoopCount; i++)
            {
                list.Add(new PluginContext() { PluginService = new TestClass()});
            }

            Console.WriteLine($"Start  phase1...");

            Stopwatch sp1 = new Stopwatch();
            sp1.Start();

            for (int i = 0; i < LoopCount; i++)
            {
                int index = i;
                list[i].PluginService.Member1 = new ClassMember { Name = index++.ToString(), Value = index++.ToString() };
                list[i].PluginService.Member2 = new ClassMember { Name = index++.ToString(), Value = index++.ToString() };
                list[i].PluginService.Member3 = new ClassMember { Name = index++.ToString(), Value = index++.ToString() };
                list[i].PluginService.Property1 = index++.ToString();
                list[i].PluginService.Property2 = index++.ToString();

                var tempClass = new TempClass { PluginService = list[i].PluginService };
                list[i].PluginService.GetHashCode();
                tempClass.GetHashCode();
            }

            sp1.Stop();
            Console.WriteLine($"Keep One Instance: {sp1.ElapsedMilliseconds}");

            GC.Collect(2);
            Thread.Sleep(10000);
            GC.EndNoGCRegion();
            Console.WriteLine($"Start  phase2...");

            Stopwatch sp = new Stopwatch();
            sp.Start();

            for (int i = 0; i < LoopCount; i++)
            {
                int index = i;
                list[i].PluginService = new TestClass()
                {
                    Member1 = new ClassMember { Name = index++.ToString(), Value = index++.ToString() },
                    Member2 = new ClassMember { Name = index++.ToString(), Value = index++.ToString() },
                    Member3 = new ClassMember { Name = index++.ToString(), Value = index++.ToString() },
                    Property1 = index++.ToString(),
                    Property2 = index++.ToString(),
                };
                var tempClass = new TempClass { PluginService = list[i].PluginService };
                list[i].PluginService.GetHashCode();
                tempClass.GetHashCode();
            }

            sp.Stop();
            Console.WriteLine($"Create Instance Every Time: {sp.ElapsedMilliseconds}");
        }

        private static void TestExperimentCompare()
        {
            var result = new List<string>();
            var dir = Directory.GetCurrentDirectory();

            var dirList = new List<Tuple<string, DateTime>>();
            foreach (var subdir in Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly))
            {
                var copyDone = Path.Combine(subdir, ".orchestration.copydone");
                if (File.Exists(copyDone))
                {
                    var creationTime = File.GetCreationTime(copyDone);
                    dirList.Add(new Tuple<string, DateTime>(subdir, creationTime));
                }
            }

            dirList.Sort((o1, o2) => o1.Item2.CompareTo(o2.Item2));

            for (int i = 0; i < dirList.Count - 1; i++)
            {
                Console.WriteLine($"start analyzing {dirList[i].Item1} and {dirList[i + 1].Item1}");

                result.Add(CompareDir(dirList[i].Item1, dirList[i + 1].Item1));
            }

            File.WriteAllLines("result.txt", result);
        }

        private static string CompareDir(string srcDir, string destDir)
        {
            double totalFiles = 0;
            double diffFiles = 0;

            var srcDictionary = GenerateHashDri(Path.Combine(srcDir, ".orchestration.manifest"));
            var destDictionary = GenerateHashDri(Path.Combine(destDir, ".orchestration.manifest"));

            foreach(var file in destDictionary)
            {
                totalFiles++;
                if (!srcDictionary.ContainsKey(file.Key) || !string.Equals(destDictionary[file.Key], srcDictionary[file.Key]))
                {
                    Console.WriteLine($"Found diff file: {file.Key}");
                    diffFiles++;
                }
            }

            return $"SRC: {Path.GetFileName(srcDir)}\t DEST: {Path.GetFileName(destDir)}\t Diff:{(diffFiles / totalFiles).ToString("P1")}\t Total: {totalFiles}";
        }

        private static Dictionary<string, string> GenerateHashDri(string path)
        {
            var dic = new Dictionary<string, string>();

            if(File.Exists(path))
            {
                foreach(var line in File.ReadLines(path).Skip(4))
                {
                    var secs = line.Split(",");
                    if(secs.Length == 12)
                    {
                        dic.Add(secs[1], secs[7]);
                    }
                }
            }

            Console.WriteLine($"Generated hash for {path} with count {dic.Count}");

            return dic;
        }
    }
}
