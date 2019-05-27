using System.Windows;
using System.Windows.Controls;

namespace OpenAddOnManager.Windows
{
    public partial class ManifestsDialog : UserControl
    {
        public ManifestsDialog() => InitializeComponent();

        void AddNewManifestUrlClickHandler(object sender, RoutedEventArgs e)
        {
            var context = Context;
            context.ManifestUrls.Add(context.NewManifestUrl);
            context.NewManifestUrl = string.Empty;
        }

        void UserControlLoadedHandler(object sender, RoutedEventArgs e)
        {
            ok.CommandParameter = true;
            cancel.CommandParameter = false;
        }

        ManifestsDialogContext Context => DataContext as ManifestsDialogContext;
    }
}
