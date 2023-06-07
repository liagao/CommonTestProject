namespace AzureBlobDownloader
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using System.Text.RegularExpressions;

    internal class Program
    {
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=xapinfoblobstorages;AccountKey=9SY6TvimyjmnuVXbTL7C0nn/mFQMphrdBft2gH7OLRocje448TzkcJwt8Bde/iotaBHvNH+DYseN+AStaEoyAw==;EndpointSuffix=core.windows.net";
        private const string ContainerName = "processednps";
        private const string ResultFolder = "D:\\Results";

        private static BlobContainerClient ContainerClient = new BlobServiceClient(ConnectionString).GetBlobContainerClient(ContainerName);

        static void Main(string[] args)
        {
            //EnumerateDecompliedWorkflowPluginBlob();

            string a = @"
using System.Diagnostics;

namespace CommonTestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(0 % 1);
            Console.ReadLine();
            if (args.Length == 0)
            {
                StartInNode(0);
                StartInNode(1);
                StartInNode(2);
                StartInNode(3);
                StartInNode(4);
                StartInNode(5);
                StartInNode(6);
                StartInNode(7);

                Console.ReadLine();
            }
            else
            {
                var now = DateTime.Now;
                Parallel.For(0, 100,
                    o =>
                    {
                        while ((DateTime.Now - now) < TimeSpan.FromSeconds(10))
                        {
                            _ = new string[1000];
                        }
                    });
            }
        }

        private static void StartInNode(int nodeId)
        {
            Console.WriteLine($""Start in node: {nodeId}"");
            var p = new Process();
            var processStartTime = DateTime.UtcNow;
            p.StartInfo = new ProcessStartInfo
            {
                FileName = @""cmd.exe"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = $""/C \""start /Node {nodeId} ProcessorAffinityTest.exe {nodeId}\"""",
            };

            p.Start();

            while ((DateTime.UtcNow - processStartTime) < TimeSpan.FromMinutes(1))
            {
                var list = Process.GetProcessesByName(""ProcessorAffinityTest"");
                foreach (var item in list)
                {
                    if (item.StartTime.ToUniversalTime() > processStartTime)
                    {
                        long mask = 0x0FFFFFFFF;
                        item.ProcessorAffinity = (System.IntPtr)(mask << ((nodeId%2) * 32));
                        Console.WriteLine($""Reset the process affinity: {nodeId}: {item.Id}"");

                        item.WaitForExit();
                        return;
                    }
                }
            }
        }
    }
}

";

            Console.WriteLine(Regex.Unescape(a));
        }

        public static void EnumerateDecompliedWorkflowPluginBlob()
        {
            int i = 0;
            List<Task> tasks = new List<Task>(100000);
            foreach (BlobHierarchyItem packageItem in ContainerClient.GetBlobsByHierarchy(delimiter: "/"))
            {
                if (packageItem.IsPrefix)
                {
                    var latestVersionItem = ContainerClient.GetBlobsByHierarchy(delimiter: "/", prefix: packageItem.Prefix).Last(item => item.IsPrefix);
                    // download workflows
                    foreach (var workflowItem in ContainerClient.GetBlobs(prefix: $"{latestVersionItem.Prefix}Workflows/DecompiledCode/"))
                    {
                        Console.WriteLine($"Downloading workflow item: {workflowItem.Name} ...");
                        tasks.Add(DownloadDecompliedWorkflowPluginBlobAsync(workflowItem.Name, Path.Combine(ResultFolder, "Workflows", packageItem.Prefix.Trim('/'), workflowItem.Name.Substring(workflowItem.Name.LastIndexOf('/') + 1))));
                    }

                    // download plugins
                    foreach (var pluginItem in ContainerClient.GetBlobs(prefix: $"{latestVersionItem.Prefix}Plugins/DecompiledCode/"))
                    {
                        Console.WriteLine($"Downloading plugin item: {pluginItem.Name} ...");
                        tasks.Add(DownloadDecompliedWorkflowPluginBlobAsync(pluginItem.Name, Path.Combine(ResultFolder, "Plugins", packageItem.Prefix.Trim('/'), pluginItem.Name.Substring(pluginItem.Name.LastIndexOf('/') + 1))));
                    }
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        public static async Task DownloadDecompliedWorkflowPluginBlobAsync(string blobName, string downloadPath)
        {
            BlobClient blobClient = ContainerClient.GetBlobClient(blobName);

            BlobDownloadInfo download = await blobClient.DownloadAsync();

            var dirName = Path.GetDirectoryName(downloadPath);
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            using (FileStream downloadFileStream = new FileStream(downloadPath + ".cs", FileMode.Create))
            {
                await download.Content.CopyToAsync(downloadFileStream);
                downloadFileStream.Close();
            }
        }

        // write a function to calculate similarity of two string hashset
    }
}