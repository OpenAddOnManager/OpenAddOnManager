using Gear.ActiveQuery;
using Gear.Components;
using Microsoft.Win32;
using OpenAddOnManager.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAddOnManager.Windows
{
    public class WorldOfWarcraftInstallation : SyncDisposable, IWorldOfWarcraftInstallation
    {
        public static async Task<WorldOfWarcraftInstallation> GetAsync(DirectoryInfo directory = null, SynchronizationContext synchronizationContext = null)
        {
            if (directory == null)
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Blizzard Entertainment\\World of Warcraft"))
                {
                    if (key?.GetValue("InstallPath") is string installPath && System.IO.Directory.Exists(installPath))
                    {
                        directory = new DirectoryInfo(installPath);
                        if (directory.Name.StartsWith("_") && directory.Name.EndsWith("_"))
                            directory = directory.Parent;
                    }
                    else
                        throw new WorldOfWarcraftInstallationUnavailableException();
                }
            }
            var installation = new WorldOfWarcraftInstallation(synchronizationContext) { Directory = directory };
            await installation.AddClientsAsync().ConfigureAwait(false);
            installation.InitializeFileSystemWatcher();
            return installation;
        }

        WorldOfWarcraftInstallation(SynchronizationContext synchronizationContext)
        {
            clients = new SynchronizedObservableDictionary<string, IWorldOfWarcraftInstallationClient>(synchronizationContext, StringComparer.OrdinalIgnoreCase);
            Clients = new ReadOnlySynchronizedObservableRangeDictionary<string, IWorldOfWarcraftInstallationClient>(clients);
            ClientsForBinding = clients.ToActiveEnumerable();
        }

        readonly SynchronizedObservableDictionary<string, IWorldOfWarcraftInstallationClient> clients;
        FileSystemWatcher fileSystemWatcher;

        Task AddClientsAsync() => Task.Run(async () =>
        {
            foreach (var subDirectory in Directory.GetDirectories().Where(subDirectory => subDirectory.Name.StartsWith("_") && subDirectory.Name.EndsWith("_") && !clients.ContainsKey(subDirectory.Name)))
            {
                try
                {
                    clients.Add(subDirectory.Name, await WorldOfWarcraftInstallationClient.CreateAsync(this, subDirectory).ConfigureAwait(false));
                }
                catch
                {
                    // TODO: report to user
                }
            }
        });

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClientsForBinding?.Dispose();
                fileSystemWatcher?.Dispose();
            }
        }

        void FileSystemWatcherErrorHandler(object sender, ErrorEventArgs e)
        {
            // TODO: report to user
            fileSystemWatcher?.Dispose();
            InitializeFileSystemWatcher();
        }

        async void FileSystemWatcherEventHandler(object sender, FileSystemEventArgs e)
        {
            foreach (var clientKey in clients.Keys.ToImmutableArray())
            {
                var client = clients[clientKey];
                var clientDirectory = client.Directory;
                clientDirectory.Refresh();
                if (!clientDirectory.Exists)
                {
                    clients.Remove(clientKey);
                    client.Dispose();
                }
            }
            await AddClientsAsync().ConfigureAwait(false);
        }

        void InitializeFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(Directory.FullName);
            fileSystemWatcher.Changed += FileSystemWatcherEventHandler;
            fileSystemWatcher.Created += FileSystemWatcherEventHandler;
            fileSystemWatcher.Deleted += FileSystemWatcherEventHandler;
            fileSystemWatcher.Error += FileSystemWatcherErrorHandler;
            fileSystemWatcher.Renamed += FileSystemWatcherEventHandler;
            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public IReadOnlyDictionary<string, IWorldOfWarcraftInstallationClient> Clients { get; private set; }

        public IActiveEnumerable<IWorldOfWarcraftInstallationClient> ClientsForBinding { get; private set; }

        public DirectoryInfo Directory { get; private set; }
    }
}
