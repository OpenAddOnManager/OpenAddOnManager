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
            var flavorInfoFile = new FileInfo(Path.Combine(Directory.FullName, ".flavor.info"));
            if (!flavorInfoFile.Exists)
                throw new WorldOfWarcraftInstallationClientFlavorInfoNotFoundException();
            var flavorInfo = File.ReadAllLines(flavorInfoFile.FullName);
            if (flavorInfo.Length <= 1 || !Enum.TryParse<Flavor>(flavorInfo[1], out var flavor))
                throw new WorldOfWarcraftInstallationClientFlavorInfoNotFoundException();
            Flavor = flavor;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public DirectoryInfo Directory { get; private set; }

        public FileInfo Executible { get; }

        public IWorldOfWarcraftInstallation Installation { get; private set; }

        public Flavor Flavor { get; }

        public string FlavorName => Utilities.GetFlavorName(Flavor);
    }
}
