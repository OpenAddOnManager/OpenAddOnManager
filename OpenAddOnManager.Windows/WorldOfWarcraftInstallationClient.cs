using Gear.Components;
using OpenAddOnManager.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAddOnManager.Windows
{
    public class WorldOfWarcraftInstallationClient : SyncDisposablePropertyChangeNotifier, IWorldOfWarcraftInstallationClient
    {
        public static async Task<WorldOfWarcraftInstallationClient> CreateAsync(WorldOfWarcraftInstallation worldOfWarcraftInstallation, DirectoryInfo directory)
        {
            var client = new WorldOfWarcraftInstallationClient()
            {
                Directory = directory,
                Installation = worldOfWarcraftInstallation
            };
            await Task.Run(async () =>
            {
                var rootExecutables = directory.GetFiles("*.exe");
                var clientExecutable =
                    rootExecutables.FirstOrDefault(rootExecutable => rootExecutable.Name.Equals("wow.exe", StringComparison.OrdinalIgnoreCase)) ??
                    rootExecutables.FirstOrDefault(rootExecutable => rootExecutable.Name.Equals("wowt.exe", StringComparison.OrdinalIgnoreCase)) ??
                    rootExecutables.FirstOrDefault(rootExecutable => rootExecutable.Name.Equals("wowb.exe", StringComparison.OrdinalIgnoreCase));
                if (clientExecutable == null)
                    throw new WorldOfWarcraftInstallationClientExecutableNotFoundException();
                if (!Version.TryParse(FileVersionInfo.GetVersionInfo(clientExecutable.FullName).FileVersion, out var clientExecutableVersion))
                    throw new WorldOfWarcraftInstallationClientExecutableVersionFormatException();
                client.Version = clientExecutableVersion;
                client.InterfaceVersion = await client.GetInterfaceVersionAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
            return client;
        }

        WorldOfWarcraftInstallationClient()
        {
        }

        string interfaceVersion;

        protected override void Dispose(bool disposing)
        {
        }

        public DirectoryInfo Directory { get; private set; }

        public IWorldOfWarcraftInstallation Installation { get; private set; }

        public string InterfaceVersion
        {
            get => interfaceVersion;
            private set => SetBackedProperty(ref interfaceVersion, in value);
        }

        public string ReleaseChannelId => Directory.Name;

        public string ReleaseChannelName => Utilities.GetReleaseChannelNameFromId(ReleaseChannelId);

        public Version Version { get; private set; }
    }
}
