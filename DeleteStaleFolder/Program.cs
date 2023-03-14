namespace DeleteStaleFolder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    internal class Program
    {
        //private const string RootFolderFormat = @"\\{0}\K$\data\ApplicationHostData\ExpLoad\";
        private const string RootFolderFormat = @"D:\{0}\";
        private static void Main(string[] args)
        {
            // parse arguments
            var rootFolder = string.Format(RootFolderFormat, args[0]);
            int retainCount = args.Length >= 2 ? int.Parse(args[1]) : 2;

            // get all the folders that need to delete
            var list = new List<DirectoryInfo>();
            var rootFolderItems = Directory.EnumerateDirectories(rootFolder).ToList();
            foreach (var directory in rootFolderItems)
            {
                foreach (var subFolder in Directory.EnumerateDirectories(directory))
                {
                    list.Add(new DirectoryInfo(subFolder));
                }
            }

            // decide retain folders
            var dictionary = list.ToDictionary(o => o, o => o.LastWriteTime);
            var timeList = list.Select(o => o.LastWriteTime).ToList();

            var retainList = new HashSet<DateTime>();
            for (int i = retainCount; i > 0; i--)
            {
                var maxModifiedTime = timeList.Max();
                retainList.Add(maxModifiedTime);
                timeList.Remove(maxModifiedTime);
            }

            // start deletion
            Parallel.ForEach(dictionary, directoryInfoItem =>
            {
                if (!retainList.Contains(directoryInfoItem.Value))
                {
                    Directory.Delete(directoryInfoItem.Key.FullName, true);
                    Console.WriteLine($"{directoryInfoItem.Key.FullName} has been deleted!");
                }
            });

            // delete empty parent folder
            rootFolderItems.ForEach(DeleteFolderWithoutException);
        }

        private static void DeleteFolderWithoutException(string folder)
        {
            try
            {
                Directory.Delete(folder);
            }
            catch
            {
                // ignored
            }
        }
    }
}
