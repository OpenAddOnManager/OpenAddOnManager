using Gear.Components;
using OpenAddOnManager.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OpenAddOnManager.Windows
{
    public class WorldOfWarcraftInstallationClient : SyncDisposablePropertyChangeNotifier, IWorldOfWarcraftInstallationClient
    {
        public WorldOfWarcraftInstallationClient(WorldOfWarcraftInstallation worldOfWarcraftInstallation, DirectoryInfo directory)
        {
            Directory = directory;
            Installation = worldOfWarcraftInstallation;
            var rootExecutables = directory.GetFiles("*.exe");
            Executible =
                rootExecutables.FirstOrDefault(rootExecutable => rootExecutable.Name.Equals("wow.exe", StringComparison.OrdinalIgnoreCase)) ??
                rootExecutables.FirstOrDefault(rootExecutable => rootExecutable.Name.Equals("wowt.exe", StringComparison.OrdinalIgnoreCase)) ??
                rootExecutables.FirstOrDefault(rootExecutable => rootExecutable.Name.Equals("wowb.exe", StringComparison.OrdinalIgnoreCase));
            if (Executible == null)
                throw new WorldOfWarcraftInstallationClientExecutableNotFoundException();
            if (!Version.TryParse(FileVersionInfo.GetVersionInfo(Executible.FullName).FileVersion, out var clientExecutableVersion))
                throw new WorldOfWarcraftInstallationClientExecutableVersionFormatException();
        }

        protected override void Dispose(bool disposing)
        {
        }

        public DirectoryInfo Directory { get; private set; }

        public FileInfo Executible { get; }

        public IWorldOfWarcraftInstallation Installation { get; private set; }

        public string ReleaseChannelId => Directory.Name;

        public string ReleaseChannelName => Utilities.GetReleaseChannelNameFromId(ReleaseChannelId);
    }
}
