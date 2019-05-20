using System;
using System.IO;
using System.Linq;
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

        public static string GetReleaseChannelNameFromId(string releaseChannelIdentifier) =>
            string.Join(" ", releaseChannelIdentifier[1..^1].Split('_').Select(word => $"{char.ToUpperInvariant(word[0])}{word.Substring(1)}"));

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
