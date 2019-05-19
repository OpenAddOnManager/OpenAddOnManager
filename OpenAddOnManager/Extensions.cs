using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public static class Extensions
    {
        static readonly Regex wtfConfigSetVariablePattern = new Regex("^SET (?<name>[^ ]*) \\\"(?<value>.*)\\\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
