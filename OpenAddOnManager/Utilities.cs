using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public static class Utilities
    {
        public static Task<DirectoryInfo> GetCommonStorageDirectoryAsync() => Task.Run(() =>
        {
            var directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "OpenAddOnManager"));
            if (!directory.Exists)
                directory.Create();
            return directory;
        });

        public static string GetFlavorName(Flavor flavor) => flavor switch
        {
            Flavor.wow => "Release",
            Flavor.wowt => "PTR",
            Flavor.wow_classic_beta => "Classic Beta",
            _ => throw new NotSupportedException()
        };

        public static int GetStepsUpFromDirectory(FileInfo file, DirectoryInfo directory)
        {
            var traversingDirectory = file.Directory;
            var steps = 0;
            while (traversingDirectory != directory)
            {
                traversingDirectory = traversingDirectory.Parent;
                if (traversingDirectory == null)
                    return -1;
                ++steps;
            }
            return steps;
        }
    }
}
