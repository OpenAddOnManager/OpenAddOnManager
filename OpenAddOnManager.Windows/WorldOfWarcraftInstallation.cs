using Gear.ActiveQuery;
using Gear.Components;
using Nito.AsyncEx;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAddOnManager.Windows
{
    public class WorldOfWarcraftInstallation : SyncDisposable, IWorldOfWarcraftInstallation
    {
        public static async Task<WorldOfWarcraftInstallation> CreateAsync(DirectoryInfo directory, SynchronizationContext synchronizationContext = null)
        {
            var installation = new WorldOfWarcraftInstallation { Directory = directory };
            if (synchronizationContext != null)
                installation.ClientsForBinding = installation.clients.SwitchContext(synchronizationContext);
            await installation.AddClientsAsync().ConfigureAwait(false);
            installation.InitializeFileSystemWatcher();
            return installation;
        }

        WorldOfWarcraftInstallation()
        {
            clients = new ObservableCollection<IWorldOfWarcraftInstallationClient>();
            Clients = new ReadOnlyObservableCollection<IWorldOfWarcraftInstallationClient>(clients);
        }

        readonly ObservableCollection<IWorldOfWarcraftInstallationClient> clients;
        readonly AsyncLock clientsAccess = new AsyncLock();
        FileSystemWatcher fileSystemWatcher;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClientsForBinding?.Dispose();
                fileSystemWatcher?.Dispose();
            }
        }

        Task AddClientsAsync() => Task.Run(async () =>
        {
            foreach (var subDirectory in Directory.GetDirectories().Where(subDirectory => subDirectory.Name.StartsWith("_") && !clients.Any(client => client.Directory == subDirectory)))
            {
                try
                {
                    clients.Add(await WorldOfWarcraftInstallationClient.CreateAsync(this, subDirectory).ConfigureAwait(false));
                }
                catch
                {
                    // TODO: report to user
                }
            }
        });

        void FileSystemWatcherErrorHandler(object sender, ErrorEventArgs e)
        {
            // TODO: report to user
            fileSystemWatcher?.Dispose();
            InitializeFileSystemWatcher();
        }

        async void FileSystemWatcherEventHandler(object sender, FileSystemEventArgs e)
        {
            using (await clientsAccess.LockAsync().ConfigureAwait(false))
            {
                for (var i = 0; i < clients.Count;)
                {
                    var client = clients[i];
                    var clientDirectory = client.Directory;
                    clientDirectory.Refresh();
                    if (!clientDirectory.Exists)
                    {
                        clients.RemoveAt(i);
                        client.Dispose();
                    }
                    else
                        ++i;
                }
                await AddClientsAsync().ConfigureAwait(false);
            }
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

        public IReadOnlyList<IWorldOfWarcraftInstallationClient> Clients { get; private set; }

        public IActiveEnumerable<IWorldOfWarcraftInstallationClient> ClientsForBinding { get; private set; }

        public DirectoryInfo Directory { get; private set; }
    }
}
