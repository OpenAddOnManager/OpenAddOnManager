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
            client.InitializeFileSystemWatcher();
            return client;
        }

        WorldOfWarcraftInstallationClient()
        {
        }

        FileSystemWatcher fileSystemWatcher;
        string interfaceVersion;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                fileSystemWatcher?.Dispose();
        }

        void FileSystemWatcherErrorHandler(object sender, ErrorEventArgs e)
        {
            // TODO: report to user
            fileSystemWatcher?.Dispose();
            InitializeFileSystemWatcher();
        }

        async void FileSystemWatcherEventHandler(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Substring(Directory.FullName.Length).Equals("\\wtf\\config.wtf", StringComparison.OrdinalIgnoreCase))
                InterfaceVersion = await this.GetInterfaceVersionAsync().ConfigureAwait(false);
        }

        void InitializeFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(Directory.FullName);
            fileSystemWatcher.Changed += FileSystemWatcherEventHandler;
            fileSystemWatcher.Created += FileSystemWatcherEventHandler;
            fileSystemWatcher.Deleted += FileSystemWatcherEventHandler;
            fileSystemWatcher.Error += FileSystemWatcherErrorHandler;
            fileSystemWatcher.Renamed += FileSystemWatcherEventHandler;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
            fileSystemWatcher.EnableRaisingEvents = true;
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
