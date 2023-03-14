namespace GenerateCopyDoneFile
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    internal class Program
    {
        private const string RootFolderFormat = @"\\{0}\K$\data\ApplicationHostData\ExpLoad\";
        private static void Main(string[] args)
        {
            // parse arguments
            var rootFolder = string.Format(RootFolderFormat, args[0]);

            // get all the folders that need to delete
            var list = new List<string>();
            var rootFolderItems = Directory.EnumerateDirectories(rootFolder).ToList();
            foreach (var directory in rootFolderItems)
            {
                foreach (var subFolder in Directory.EnumerateDirectories(directory))
                {
                    list.Add(subFolder);
                }
            }

            // start generate file
            Parallel.ForEach(list, path => File.Create(Path.Combine(path, "local.copydone")));
        }
    }
}
