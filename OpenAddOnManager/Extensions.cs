using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public static class Extensions
    {
        static readonly Regex wtfConfigSetVariablePattern = new Regex("^SET (?<name>[^ ]*) \\\"(?<value>.*)\\\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IReadOnlyList<FileInfo> CopyContentsTo(this DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory, bool overwrite = false)
        {
            var copiedFiles = new List<FileInfo>();
            foreach (var sourceSubDirectory in sourceDirectory.GetDirectories())
            {
                var targetSubDirectory = new DirectoryInfo(Path.Combine(targetDirectory.FullName, sourceSubDirectory.Name));
                if (!targetSubDirectory.Exists)
                    targetSubDirectory.Create();
                copiedFiles.AddRange(CopyContentsTo(sourceSubDirectory, targetSubDirectory, overwrite));
            }
            foreach (var sourceFile in sourceDirectory.GetFiles())
                copiedFiles.Add(sourceFile.CopyTo(Path.Combine(targetDirectory.FullName, sourceFile.Name), overwrite));
            return copiedFiles.ToImmutableArray();
        }

        public static async Task<string> GetInterfaceVersionAsync(this IWorldOfWarcraftInstallationClient worldOfWarcraftInstallationClient)
        {
            var wtfConfigFile = new FileInfo(Path.Combine(worldOfWarcraftInstallationClient.Directory.FullName, "WTF", "Config.wtf"));
            if (wtfConfigFile.Exists)
                using (var wtfConfigFileStream = wtfConfigFile.OpenRead())
                using (var wtfConfigFileStreamReader = new StreamReader(wtfConfigFileStream))
                    while (!wtfConfigFileStreamReader.EndOfStream)
                    {
                        var match = wtfConfigSetVariablePattern.Match(await wtfConfigFileStreamReader.ReadLineAsync().ConfigureAwait(false));
                        if (match.Success && match.Groups["name"].Value == "lastAddonVersion")
                            return match.Groups["match"].Value;
                    }
            return null;
        }
    }
}
