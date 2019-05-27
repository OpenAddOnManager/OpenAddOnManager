using Gear.ActiveQuery;
using Gear.Components;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;

namespace OpenAddOnManager.Windows
{
    public class MainWindowContext : SyncDisposablePropertyChangeNotifier
    {
        public MainWindowContext(AddOnManager addOnManager)
        {
            showPrereleaseVersions = App.ShowPrereleaseVersions;
            AddOnManager = addOnManager;
            SortedClients = addOnManager.WorldOfWarcraftInstallation.Clients.ActiveOrderBy(client => client.FlavorName);
            firstSortedClient = SortedClients.ActiveFirstOrDefault();
            firstSortedClient.PropertyChanged += FirstSortedClientPropertyChangedHandler;
            selectedClient = firstSortedClient.Value;
        }

        readonly IActiveValue<IWorldOfWarcraftInstallationClient> firstSortedClient;
        string searchFor;
        IWorldOfWarcraftInstallationClient selectedClient;
        bool showPrereleaseVersions;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                firstSortedClient.PropertyChanged -= FirstSortedClientPropertyChangedHandler;
                firstSortedClient.Dispose();
                SortedClients.Dispose();
            }
        }

        void FirstSortedClientPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IActiveValue<IWorldOfWarcraftInstallationClient>.Value) && firstSortedClient.Value is IWorldOfWarcraftInstallationClient client && SelectedClient == null)
                SelectedClient = client;
        }

        public AddOnManager AddOnManager { get; }

        public IReadOnlyList<TimeSpan> ManifestsCheckFrequencyOptions { get; } = new TimeSpan[]
        {
            TimeSpan.FromMinutes(5),
            TimeSpan.FromHours(0.25),
            TimeSpan.FromHours(0.5),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(4),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(12),
            TimeSpan.FromDays(1),
            TimeSpan.FromDays(2),
            TimeSpan.FromDays(3),
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(14)
        }.ToImmutableArray();

        public string SearchFor
        {
            get => searchFor;
            set => SetBackedProperty(ref searchFor, in value);
        }

        public IWorldOfWarcraftInstallationClient SelectedClient
        {
            get => selectedClient;
            set => SetBackedProperty(ref selectedClient, in value);
        }

        public bool ShowPrereleaseVersions
        {
            get => showPrereleaseVersions;
            set
            {
                if (SetBackedProperty(ref showPrereleaseVersions, in value))
                    App.ShowPrereleaseVersions = showPrereleaseVersions;
            }
        }

        public IActiveEnumerable<IWorldOfWarcraftInstallationClient> SortedClients { get; }
    }
}
