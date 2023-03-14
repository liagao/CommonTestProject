namespace ConsoleApp1
{
    using System;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            foreach (var file in Directory.GetFiles(@"C:\Users\liagao\Music\Kuwo\1"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Contains(" - "))
                {
                    string[] ss = fileName.Split(new string[]{" - "}, StringSplitOptions.RemoveEmptyEntries);
                    MoveFile(file, Path.Combine(@"C:\Users\liagao\Music\Kuwo", $"{ss[1]}-{ss[0]}{Path.GetExtension(file)}"));
                }
            }
            /*foreach(var file in Directory.GetFiles(@"C:\Users\liagao\Music\Kuwo"))
            {
                var fileName = Path.GetFileName(file);
                if(fileName.Contains(" - "))
                {
                    MoveFile(file, Path.Combine(@"C:\Users\liagao\Music\Kuwo", Path.GetFileName(file).Replace(" - ", "-")));
                }
            }

            foreach (var dir in Directory.GetDirectories(@"C:\Users\liagao\Music\Kuwo"))
            {
                var dirName = Path.GetFileName(dir);
                foreach (var file in Directory.GetFiles(dir))
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.Contains(" - "))
                    {
                        var newName = $"{dirName}-{fileName.Substring(0, fileName.IndexOf(" - "))}{Path.GetExtension(fileName)}";
                        MoveFile(file, Path.Combine(@"C:\Users\liagao\Music\Kuwo", newName));
                    }
                }
            }*/
        }

        private static void MoveFile(string file, string v)
        {
            try
            {
                File.Move(file, v);
                Console.WriteLine($"Copy file from {file} to {v}");
            }
            catch(Exception e)
            {
                Console.WriteLine($"Failed to copy file from {file} to {v}");
            }
        }
    }
}
