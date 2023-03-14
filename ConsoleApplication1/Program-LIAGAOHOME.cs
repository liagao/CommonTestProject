namespace ConsoleApplication1
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(@"asdasdasd\asdasd\asdasd");
            /*using (SmtpClient client = new SmtpClient())
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential("xapgate@microsoft.com", "@Mc$xxy749x3~x++LH");
                client.Port = 587;
                client.Host = "smtp.office365.com";
                client.EnableSsl = true;
                client.Send(new MailMessage("xapgate@microsoft.com", "liagao@microsoft.com", "123", "123"));
            }
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> globalConfigDic = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            var dirs = Directory.EnumerateDirectories("D:\\Demo");
            foreach (var dir in dirs)
            {
                var team = Path.GetFileName(dir);
                var teamDic = new Dictionary<string, Dictionary<string, string>>();
                globalConfigDic.Add(team, teamDic);
                foreach (var file in Directory.EnumerateFiles(dir, "*.ini", SearchOption.TopDirectoryOnly))
                {
                    var table = Path.GetFileNameWithoutExtension(file);
                    var tableDic = new Dictionary<string, string>();
                    teamDic.Add(table, tableDic);

                    foreach (var line in File.ReadAllLines(file))
                    {
                        var keyvalue = line.Split('=');
                        tableDic.Add(keyvalue[0], keyvalue[1]);
                    }
                }
            }*/
            /*string[] machines = "CO4AAP050C40947,CO4AAP053DEAD7A,CO4AAP0594CDF7A,CO4AAP05C9C6725,CO4AAP0736C6B18,CO4AAP0791122D5,CO4AAP07F785B20,CO4AAP0925E3637,CO4AAP0A4254C52,CO4AAP0AAC0B842,CO4AAP0B488511C,CO4AAP0E862BE4E,CO4AAP0F2276D61,CO4AAP0FA95D9EC,CO4AAP10A0F41E8,CO4AAP110F44183,CO4AAP1179B5199,CO4AAP11B73E45D,CO4AAP11B939D00,CO4AAP1228631D8,CO4AAP12F702D06,CO4AAP142A54EE0,CO4AAP146CB1CF0,CO4AAP146FCBEED,CO4AAP147858AD4,CO4AAP155BE4A95,CO4AAP15691B11F,CO4AAP15B4200BD,CO4AAP17EE473A2,CO4AAP181488791,CO4AAP18825023C,CO4AAP1A47D5DC6,CO4AAP1A5D43480,CO4AAP1A7A00A62,CO4AAP1AB34D7B8,CO4AAP1ABA4A04B,CO4AAP1B0796DC5,CO4AAP1B198C97D,CO4AAP1B36348FE,CO4AAP1B5E6E064,CO4AAP1B778351A,CO4AAP1BABF67AC,CO4AAP1C559E3F1,CO4AAP1C5AAE078,CO4AAP1CA7BE5D4,CO4AAP1CE368EA6,CO4AAP1D77227F2,CO4AAP1D88540FD,CO4AAP1E5F245C6,CO4AAP1E85B77FA,CO4AAP1E914E9BC,CO4AAP2038CC150,CO4AAP204A2A4C1,CO4AAP204B3C3F3,CO4AAP20DD643B1,CO4AAP21596F6EE,CO4AAP21BB2F75E,CO4AAP21C4CB3A7,CO4AAP2277DC54D,CO4AAP22A5EB499,CO4AAP22E353BEA,CO4AAP22F6185BE,CO4AAP23F59B72D,CO4AAP24A6EDD17,CO4AAP25E42722F,CO4AAP25FFF2193,CO4AAP261066716,CO4AAP2610F6B16,CO4AAP2652A31D7,CO4AAP269043B4E,CO4AAP26F1D4ABE,CO4AAP279A7CD9C,CO4AAP280AD6F51,CO4AAP28AD5DAF8,CO4AAP292CA0A0F,CO4AAP2A825D771,CO4AAP2AB55BD7A,CO4AAP2C1E846F7,CO4AAP2C9BD0DA6,CO4AAP2E126A912,CO4AAP30A6AE5FD,CO4AAP315935274,CO4AAP327E10F7B,CO4AAP33C4D8BAD,CO4AAP3435C7244,CO4AAP343C3564E,CO4AAP343DA8D9F,CO4AAP345848EA6,CO4AAP350AF50D3,CO4AAP3532DEB01,CO4AAP3536DD82F,CO4AAP367AB6A26,CO4AAP377DBD448,CO4AAP38BD4BD11,CO4AAP3A0899565,CO4AAP3A3D01CFF,CO4AAP3B0AD494C,CO4AAP3B89C800B,CO4AAP3B8D7DD00,CO4AAP3C40EA116,CO4AAP3C4FF9648,CO4AAP3C7E9FDDF,CO4AAP3D57A428A,CO4AAP3F060B85F,CO4AAP403527F82,CO4AAP4061F4F77,CO4AAP4087FF2CC,CO4AAP424962F66,CO4AAP434EC71BA,CO4AAP44B496977,CO4AAP44F2488F4,CO4AAP46445A62F,CO4AAP46A34C126,CO4AAP4781B4F3F,CO4AAP479AAF689,CO4AAP48C31D6C6,CO4AAP4A2941D0F,CO4AAP4AAAD52F0,CO4AAP4AC6F4F6F,CO4AAP4AE495676,CO4AAP4B6D8FBD2,CO4AAP4C3B58732,CO4AAP4C57E7241,CO4AAP4CCA2BC9E,CO4AAP4DF2D430E,CO4AAP4EED2A234,CO4AAP50DEF1D0E,CO4AAP5125AF18E,CO4AAP52A0EA194,CO4AAP5335371CD,CO4AAP5433B67CF,CO4AAP5489A9B92,CO4AAP54ED5AC3C,CO4AAP55012391C,CO4AAP55ED01A3C,CO4AAP57F249619,CO4AAP57F907120,CO4AAP5934B3AAD,CO4AAP5A8861B34,CO4AAP5ABC0B7F2,CO4AAP5B150E763,CO4AAP5B231116A,CO4AAP5CB98DCE9,CO4AAP5DBCB75D6,CO4AAP5DDCAB245,CO4AAP5EA5064CA,CO4AAP5EA9A7B91,CO4AAP5F2B2C5B2,CO4AAP602F5157F,CO4AAP608754EDE,CO4AAP616B5493C,CO4AAP61958A552,CO4AAP62AE184B9,CO4AAP62BFF73E9,CO4AAP638A82794,CO4AAP639886B77,CO4AAP6462D9BE5,CO4AAP64E259051,CO4AAP6563F6A5F,CO4AAP6661C4770,CO4AAP66BBDEBB9,CO4AAP676FBC549,CO4AAP68C273AD1,CO4AAP68E77EC03,CO4AAP6915184D2,CO4AAP69348ECDE,CO4AAP69E2E3739,CO4AAP6A57E3444,CO4AAP6B1680303,CO4AAP6B9D0A148,CO4AAP6BB552BD2,CO4AAP6D39A95A3,CO4AAP6D3D46D24,CO4AAP6D6CDDB05,CO4AAP6DCAFBCC8,CO4AAP6DFF671E2,CO4AAP6E17C393A,CO4AAP711D2690C,CO4AAP713886017,CO4AAP72A522733,CO4AAP72D536BC5,CO4AAP734B1C1AF,CO4AAP740048D94,CO4AAP7448A69B3,CO4AAP76A22CE7C,CO4AAP7771A2004,CO4AAP77AC3F51C,CO4AAP780A41953,CO4AAP78735ABC3,CO4AAP79B1A8FA3,CO4AAP7B7E39782,CO4AAP7BB63271E,CO4AAP7D0F683CB,CO4AAP7D36C0B33,CO4AAP7F6D8DEA3,CO4AAP80B7E41CE,CO4AAP81EF9A7A7,CO4AAP81F0B833A,CO4AAP82A5E9DF3,CO4AAP839833DE6,CO4AAP839E5B115,CO4AAP83BC5A38C,CO4AAP859FAAEF0,CO4AAP85D67EFDF,CO4AAP87094BC59,CO4AAP8764900AA,CO4AAP8776CB11E,CO4AAP88799BEC0,CO4AAP88D1AC0A9,CO4AAP88D53389E,CO4AAP88E4AAFBF,CO4AAP8A650EEDB,CO4AAP8AFE6DF2B,CO4AAP8CA6BAD9B,CO4AAP8E078473B,CO4AAP8F3734D94,CO4AAP8F45A280F,CO4AAP90A02AE0E,CO4AAP90C0CCFCD,CO4AAP9171F1EE5,CO4AAP927EF90ED,CO4AAP92DDBCB39,CO4AAP94766F838,CO4AAP96F3D47AA,CO4AAP9717913EC,CO4AAP97E504403,CO4AAP98A467EF0,CO4AAP9957637D3,CO4AAP99D5C0F19,CO4AAP99DF6DD0D,CO4AAP9BA8DA7E5,CO4AAP9BBE32709,CO4AAP9BE393963,CO4AAP9C8BB445D,CO4AAP9CC7D63D8,CO4AAP9D9F1834A,CO4AAPA1919AA3B,CO4AAPA340198C2,CO4AAPA3DCE23B2,CO4AAPA425DCC1C,CO4AAPA5E2607A5,CO4AAPA75D56AD3,CO4AAPA78C4275E,CO4AAPA7D1E9F0F,CO4AAPA8922E336,CO4AAPA98FC4841,CO4AAPAA914A4F5,CO4AAPAC21A05E8,CO4AAPACA6D4F92,CO4AAPACB46C80D,CO4AAPAD8FF6F09,CO4AAPAEF72A9FD,CO4AAPAFCD3C0DC,CO4AAPB034E3103,CO4AAPB0E7F63A4,CO4AAPB34B54A21,CO4AAPB37F45C9D,CO4AAPB3DB76E03,CO4AAPB3EA7A3C5,CO4AAPB442A3CAF,CO4AAPB61476194,CO4AAPB62FAADB9,CO4AAPB64E436E3,CO4AAPB66BE4A44,CO4AAPB702E5731,CO4AAPB7C3AE0DF,CO4AAPB890C1549,CO4AAPB8DD1F581,CO4AAPBBACA5A0F,CO4AAPBCD7627C4,CO4AAPBD93E23AC,CO4AAPBE616DAC1,CO4AAPBFB6D7E71,CO4AAPBFE1B2BCE,CO4AAPC261003E1,CO4AAPC263B2C0B,CO4AAPC2FBB6C78,CO4AAPC415C69E6,CO4AAPC48903977,CO4AAPC4E7CD34D,CO4AAPC4F3A9142".Split(',');
            string[] lines = File.ReadAllLines("D:\\2.txt");
            List<string> newlines = new List<string>();
            foreach (var machine in machines)
            {
                foreach(var line in lines)
                {
                    if(line.Contains(machine) && line.Contains("leaseToken"))
                    {
                        newlines.Add(line);
                    }
                }
            }

            File.WriteAllLines("D:\\3.txt", newlines);
            Regex r = new Regex(@"(\S*)=""(.*?)""");
            string s = @"2018-12-26T20:56:08.879182 [21192/27572/i:OrchestrationManager DeliverDataFolder] service=""ApplicationHost.1"" folderImageName=""DATAIMAGE~EntityAPI"" folderLocalPath=""D:\\Data\\ApplicationHostData\\Experiments\\EntityAPI_%VERSION%"" options=""[PartitionName: 'default'],[Priority: 'Normal'],""
";
            foreach(Match m in r.Matches(s))
            {
                Console.WriteLine(m.Groups[1].Value + "  ==>  " + m.Groups[2].Value);
            }*/
            //File.WriteAllLines(@"D:\4.txt", File.ReadAllLines(@"D:\3.txt").Select(o=>o.Substring(o.IndexOf(":")+2,15)));


            /*var startHour = int.Parse(args[0]);
            var endHour = int.Parse(args[1]);
            Parallel.ForEach(File.ReadAllLines("machine.txt"), new ParallelOptions {MaxDegreeOfParallelism = 200}, machine =>
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                foreach (var file in Directory.GetFiles(@"\\{machine}\D$\data\logs\local", "AppHost*.etl", SearchOption.TopDirectoryOnly))
                {
                    Console.WriteLine($"Start parsing file {file} on machine {machine}...");
                    var totalHour = (DateTime.Now - File.GetLastWriteTime(file)).TotalHours;
                    if (totalHour >= endHour && totalHour <= startHour)
                    {
                        p.StandardInput.WriteLine($"D:\\app\\tools.xnf_fdd2efcf_35906_1\\blt.exe -f \"ExperimentsDownloadEndDetail|ExperimentsLoadStart|ExperimentsLoadCompleted\" {file} > {machine}.txt");
                    }
                }

                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
                p.Close();
            });*/

            /*var result = new List<string>();
            foreach(var line in File.ReadAllLines(@"d:\1.txt"))
            {
                var index1 = line.IndexOf("-input");
                var index2 = line.IndexOf("-output");

                result.Add(line.Substring(index1 + 7, index2 - index1 + 7));
            }

            File.WriteAllLines(@"d:\2.txt", result.Distinct().ToList());

            Console.WriteLine(Environment.Version.ToString());*/
            /*var result = new List<string>();
            
            int i = 1, j = 100;
            foreach (var line in File.ReadAllLines(@"D:\src\apgold\autopilotservice\Bn2\XAP-Prod-Bn2\Machines.csv"))
            {
                if (line.Contains("amd64,PAH,100,HP14"))
                {
                    i++;
                    result.Add(line.Replace("amd64,PAH,100,HP14", $"amd64,PAH,{j},HP14"));

                    if (i % 20 == 0)
                    {
                        j++;
                    }
                }
                else
                {
                    result.Add(line);
                }
            }

            File.WriteAllLines(@"D:\src\apgold\autopilotservice\Bn2\XAP-Prod-Bn2\Machines.csv", result.Distinct().ToList());*/
            /*var request = (HttpWebRequest)WebRequest.Create("https://apac01.safelinks.protection.outlook.com/?url=https%3A%2F%2Fbinglivesite.microsoft.com%2Fperf%2Fapi%2FBing%2Fquery%3Fdataset%3DBing%26filter%3Dstart%253A2017-07-09%257Cend%253A2017-07-16%257Cscenario%253Abing%2B%253A%2Bbrowser%257Cmetric%253Aoverall_plt%257Cbrowser%253Aall%257Cdatacenter%253Aall%257Cdevicemodel%253Aall%257Cflightassignment%253Aall%257Cmarket%253Amkt%253Aen-us%257Cos%253Aall%257Cpage%253Apage.serp%257Cprotocol%253Aall%257Ctimeaggregation%253Aday&data=02%7C01%7C%7Ce43efaead0574c92f06c08d613288606%7C72f988bf86f141af91ab2d7cd011db47%7C1%7C0%7C636717462088506475&sdata=uaZxi3L1Zbk0wDkYR9fkWDVrPeq7LLm2QnQuNcyJjJk%3D&reserved=0c");
            request.MaximumAutomaticRedirections = 4;
            request.MaximumResponseHeadersLength = 4;
            // Set credentials to use for this request.
            request.Credentials = CredentialCache.DefaultNetworkCredentials;
            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            File.WriteAllText("D:\\1.html", responseString);
            Console.WriteLine(responseString);*/


            //DateTime ss = new DateTime();
            //Console.WriteLine(ss.ToString("yyyy_MM_dd") +"    "+ss.ToShortDateString());
            /*ReplaceNUMachinesToAHwithSUOnebyOne(@"D:\src\apgold\autopilotservice\Ch1b\XAP-Prod-Ch1b\machines.csv", ",NU,0,", new[] {",NU,0,", "WCS-*16-i22f{FPGA}" }, 
                new Dictionary<string, int>{
                    {"AH",2479},
                    {"AHP1",10},
                    {"AHP2",10},
                    {"AHP3",10},
                    {"AHP4",10},
                    {"AHP5",10},
                    {"AHCI",10},
                    {"AHCOMP",10},
                    {"AHDEBUG",22},
                    {"AHIRC",10},
                    {"AHPG",6},
                    {"AHVSTS",10},
                    {"PAH",313},
                    {"HC",4},
                    {"WD",4},
                    {"XCM",4},
                    {"ROXY",5} },
                new List<string>(){ "PAH", "AHP1", "AHP3", "AHP2", "AHCI", "AHCOMP", "AHPG"});*/
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        public static void ReplaceNUMachinesToAHwithSUOnebyOne(string filepath, string magicStr, string[] strArray, Dictionary<string, int> dic, List<string> useDefaultSUlist)
        {
            foreach(var item in dic)
            {
                var result = new List<string>();
                int temptotal = 0;
                int cur = 100;
                int i = 0;
                bool useDefaultSU = useDefaultSUlist.Contains(item.Key);

                foreach (var line in File.ReadAllLines(filepath))
                {
                    i++;
                    if (strArray.All(o => line.Contains(o)) && temptotal < item.Value)
                    {
                        result.Add(useDefaultSU? line.Replace(magicStr, $",{item.Key},100,") : line.Replace(magicStr, $",{item.Key},{cur++},"));
                        temptotal++;
                        if (cur == 105)
                        {
                            cur = 100;
                        }

                        Console.WriteLine(i + "   " + line);
                    }
                    else
                    {
                        result.Add(line);
                    }
                }

                File.WriteAllLines(filepath, result);
            }
        }
    }
}
