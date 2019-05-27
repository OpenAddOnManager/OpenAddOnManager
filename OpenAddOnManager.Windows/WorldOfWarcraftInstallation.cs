using Gear.ActiveQuery;
using Gear.Components;
using Microsoft.Win32;
using OpenAddOnManager.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAddOnManager.Windows
{
    public class WorldOfWarcraftInstallation : SyncDisposable, IWorldOfWarcraftInstallation
    {
        public WorldOfWarcraftInstallation(DirectoryInfo directory = null, SynchronizationContext synchronizationContext = null)
        {
            if (directory == null)
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Blizzard Entertainment\\World of Warcraft"))
                {
                    if (key?.GetValue("InstallPath") is string installPath && System.IO.Directory.Exists(installPath))
                    {
                        directory = new DirectoryInfo(installPath);
                        if (!File.Exists(Path.Combine(directory.FullName, ".build.info")))
                            directory = directory.Parent;
                    }
                    else
                        throw new WorldOfWarcraftInstallationUnavailableException();
                }
            }

            Directory = directory;
            clientByFlavor = new SynchronizedObservableDictionary<Flavor, IWorldOfWarcraftInstallationClient>();
            ClientByFlavor = new ReadOnlySynchronizedObservableRangeDictionary<Flavor, IWorldOfWarcraftInstallationClient>(clientByFlavor);
            clientsActiveEnumerable = clientByFlavor.ToActiveEnumerable();
            Clients = synchronizationContext == null ? clientsActiveEnumerable : clientsActiveEnumerable.SwitchContext(synchronizationContext);

            initializationCompleteTaskCompletionSource = new TaskCompletionSource<object>();
            InitializationComplete = initializationCompleteTaskCompletionSource.Task;
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        readonly SynchronizedObservableDictionary<Flavor, IWorldOfWarcraftInstallationClient> clientByFlavor;
        readonly IActiveEnumerable<IWorldOfWarcraftInstallationClient> clientsActiveEnumerable;
        FileSystemWatcher fileSystemWatcher;
        readonly TaskCompletionSource<object> initializationCompleteTaskCompletionSource;

        Task AddClientsAsync() => Task.Run(() =>
        {
            foreach (var subDirectory in Directory.GetDirectories())
            {
                WorldOfWarcraftInstallationClient client = null;
                try
                {
                    client = new WorldOfWarcraftInstallationClient(this, subDirectory);
                    if (clientByFlavor.ContainsKey(client.Flavor))
                        client.Dispose();
                    else
                        clientByFlavor.Add(client.Flavor, client);
                }
                catch
                {
                    client?.Dispose();
                }
            }
        });

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Clients?.Dispose();
                clientsActiveEnumerable?.Dispose();
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
            foreach (var clientKey in clientByFlavor.Keys.ToImmutableArray())
            {
                var client = clientByFlavor[clientKey];
                var clientExecutible = client.Executible;
                clientExecutible.Refresh();
                if (!clientExecutible.Exists)
                {
                    clientByFlavor.Remove(clientKey);
                    client.Dispose();
                }
            }
            await AddClientsAsync().ConfigureAwait(false);
        }

        async void Initialize(object state)
        {
            try
            {
                await AddClientsAsync().ConfigureAwait(false);
                InitializeFileSystemWatcher();
                initializationCompleteTaskCompletionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                initializationCompleteTaskCompletionSource.SetException(ex);
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
            fileSystemWatcher.Filter = "*.exe";
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public IReadOnlyDictionary<Flavor, IWorldOfWarcraftInstallationClient> ClientByFlavor { get; private set; }

        public IActiveEnumerable<IWorldOfWarcraftInstallationClient> Clients { get; private set; }

        public DirectoryInfo Directory { get; private set; }

        public Task InitializationComplete { get; }
    }
}
