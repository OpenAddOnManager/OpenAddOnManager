using System.Windows;
using System.Windows.Controls;

namespace OpenAddOnManager.Windows
{
    public partial class AddOnLicenseDialog : UserControl
    {
        public AddOnLicenseDialog() => InitializeComponent();

        private void UserControlLoadedHandler(object sender, RoutedEventArgs e)
        {
            accept.CommandParameter = true;
            decline.CommandParameter = false;
        }
    }
}
