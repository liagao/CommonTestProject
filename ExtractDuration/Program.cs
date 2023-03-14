namespace ExtractDuration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public class PrefetchLogItem
    {
        public string MachineName { get; set; }

        public double PrefetchTimeInSeconds { get; set; }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var folder = args[0].ToUpperInvariant();
            var version = args[1];
            var fileName = args[2];
            var resultFileName = args[3];
            var startHour = args[4];
            var endHour = args[5];
            var env = args[6];
            var additionalArg = args.Length == 8 ? args[7] : string.Empty;

            Console.WriteLine("Start running Lens.exe...");
            // run Len.exe
            string searchString = $"\"for folder FILECLOUD~{folder}@{version.Replace(".", "\\.")} {{FILECLOUD~{folder}\\[{version.Replace(".", "\\.")}\\]{{duration: .*?; FsUtils-Exists\"";
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.BeginOutputReadLine();
            p.OutputDataReceived += (sender, data) => { Console.WriteLine(data.Data); Console.Out.Flush();};
            p.StandardInput.AutoFlush = true;
            var command = $"Lens.exe -sd now-{startHour} -ed now-{endHour} -f SyncAutopilotData-OM* -mf AH -env {env} -r {searchString} {additionalArg} > {fileName} & exit";
            File.WriteAllText(resultFileName, command);
            p.StandardInput.WriteLine(command);           
            p.WaitForExit();
            p.Close();

            Console.WriteLine("Start analysis...");
            //var line = "2017/12/04 19:06:04.359,BN1AAP273331862,i,Orchestration,Prefetch done,for folder FILECLOUD~PRODTEST2@1.1 {FILECLOUD~PRODTEST2[1.1]{duration: *1/15.002s; FsUtils-Exists: 2/0.000121s; FsUtils-ReadFileIntoBuffer: 1/0.316s; FsUtils-ReplaceFileEx: 1/0.000301s; FsUtils-WriteBufferToFile: 1/0.000560s; tmVersionFolderGC: 1/0.000108s; DataBusCacheDeliveryOperation: {duration: 1/0.001153s; tmAddCompletedPage1: 0s; tmAddCompletedPage2: 0s; tmDirectBufferedWrite1: 0s; tmDirectBufferedWrite2: 0s; tmFlushOldestSequance: 0s; tmGetFreePage1: 0s; tmGetFreePage2: 0s; tmPreallocate: 0s; tmWriteBuffer: 0s; tm_diskWriteBlocksRecvChunk: 0s; tm_egressBlocksRecvChunk: 0s; tm_flushRecvChunksToDisk: 0s; tm_peerDiscovery: 1/0.000503s; tm_postDeliveryCrcCheck: 0s; tm_processRecvChunk: 0s; tm_syncWriteRecvChunk: 0s; tm_waitRecvChunkAvailable: 1/0.000959s; cnt_commLatency_parents: [sum:0;sqrsum:0;count:0;max:0]; cnt_commLatency_peerDiscovery: [sum:0;sqrsum:0;count:0;max:0]; cnt_commLatency_siblings: [sum:0;sqrsum:0;count:0;max:0]; cnt_dataLatency_parents: [sum:0;sqrsum:0;count:0;max:0]; cnt_dataLatency_siblings: [sum:0;sqrsum:0;count:0;max:0]}; SyncDataFolder: {cnt_InLocalFileCount: 34; cnt_NeedCheckFileCount: 34; cnt_NeedDownloadFileCount: 0; cnt_NeedProcessFileCount: 0; cnt_NeedProcessFileSize: 0; cnt_SyncThreadCount: 2; duration: 1/1.683s; tm_DeleteFileDuration: 0s; tm_GetAllFilesDuration: 1/0.024922s; tm_GetCacheHdr: 0s; tm_SyncFiles: 1/1.682s; tm_TraverseDirectory: 1/0.056452s; tm_VerifyExistingFiles_Part1: 1/0.000075s; tm_VerifyExistingFiles_Part2: 1/0.056561s; HandlePackagesFiles: {duration: 1/1.600s; tm_GetAllPackagesDuration: 1/1.600s}}}}";

            List<PrefetchLogItem> durationList = new List<PrefetchLogItem>();
            var lineStart = $"{{FILECLOUD~{folder}[{version}]{{duration: *1/";
            var len1 = lineStart.Length;

            foreach (string line in File.ReadAllLines(fileName))
            {
                var machineNameStartIndex = line.IndexOf(",", StringComparison.OrdinalIgnoreCase);
                var machineName = line.Substring(machineNameStartIndex + 1, 15);

                var index = line.IndexOf(lineStart, StringComparison.OrdinalIgnoreCase);
                if (index > 0)
                {
                    var start = index + len1;
                    var end = line.IndexOf("s; FsUtils-Exists:", StringComparison.OrdinalIgnoreCase);
                    string value = line.Substring(start, end - start);
                    durationList.Add(new PrefetchLogItem(){MachineName = machineName, PrefetchTimeInSeconds = double.Parse(value)});
                    //Console.WriteLine(value);
                }
            }

            var sortedList = durationList.OrderBy(o => o.PrefetchTimeInSeconds).ToList();

            File.AppendAllLines(resultFileName, sortedList.Select(o=>$"{o.MachineName}\t {o.PrefetchTimeInSeconds.ToString("0.00")}"));
            var index1 = (int)(sortedList.Count * 0.8);
            Console.WriteLine($"Result: \r\nCount: {sortedList.Count}\r\nP1: {sortedList.First().PrefetchTimeInSeconds}\r\nP20: {sortedList[sortedList.Count / 5].PrefetchTimeInSeconds}\r\nP50: {sortedList[sortedList.Count / 2].PrefetchTimeInSeconds}\r\nP80: {sortedList[index1].PrefetchTimeInSeconds}\r\nP100: {sortedList.Last().PrefetchTimeInSeconds}");
        }
    }
}
