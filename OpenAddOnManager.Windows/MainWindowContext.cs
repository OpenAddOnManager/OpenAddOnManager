using Gear.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAddOnManager.Windows
{
    public class MainWindowContext : PropertyChangeNotifier
    {
        public MainWindowContext(AddOnManager addOnManager) => AddOnManager = addOnManager;

        string searchFor;
        bool showPrereleaseVersions;

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
    }
}
