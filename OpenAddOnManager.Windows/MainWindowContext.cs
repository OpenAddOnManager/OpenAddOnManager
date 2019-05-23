using Gear.ActiveQuery;
using Gear.Components;

namespace OpenAddOnManager.Windows
{
    public class MainWindowContext : SyncDisposablePropertyChangeNotifier
    {
        public MainWindowContext(AddOnManager addOnManager)
        {
            AddOnManager = addOnManager;
            SortedClients = addOnManager.WorldOfWarcraftInstallation.Clients.ActiveOrderBy(client => client.ReleaseChannelName);
        }

        string searchFor;
        bool showPrereleaseVersions;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                SortedClients?.Dispose();
        }

        public AddOnManager AddOnManager { get; }

        public string SearchFor
        {
            get => searchFor;
            set => SetBackedProperty(ref searchFor, in value);
        }

        public bool ShowPrereleaseVersions
        {
            get => showPrereleaseVersions;
            set => SetBackedProperty(ref showPrereleaseVersions, in value);
        }

        public IActiveEnumerable<IWorldOfWarcraftInstallationClient> SortedClients { get; }
    }
}
