using Gear.ActiveQuery;
using Gear.Components;
using System.ComponentModel;

namespace OpenAddOnManager.Windows
{
    public class MainWindowContext : SyncDisposablePropertyChangeNotifier
    {
        public MainWindowContext(AddOnManager addOnManager)
        {
            AddOnManager = addOnManager;
            SortedClients = addOnManager.WorldOfWarcraftInstallation.Clients.ActiveOrderBy(client => client.ReleaseChannelName);
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
            set => SetBackedProperty(ref showPrereleaseVersions, in value);
        }

        public IActiveEnumerable<IWorldOfWarcraftInstallationClient> SortedClients { get; }
    }
}
