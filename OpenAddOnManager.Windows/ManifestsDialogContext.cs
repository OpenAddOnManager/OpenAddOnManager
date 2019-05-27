using Gear.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenAddOnManager.Windows
{
    public class ManifestsDialogContext : PropertyChangeNotifier
    {
        public ManifestsDialogContext(IReadOnlyList<Uri> manifestUrls) => ManifestUrls = new ObservableCollection<string>(manifestUrls.Select(uri => uri.ToString()));

        string newManifestUrl;

        public ObservableCollection<string> ManifestUrls { get; }

        public string NewManifestUrl
        {
            get => newManifestUrl;
            set => SetBackedProperty(ref newManifestUrl, in value);
        }
    }
}
