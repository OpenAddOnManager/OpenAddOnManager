using System.Windows;

namespace OpenAddOnManager.Windows
{
    public partial class AddOnLicenseDialog : Window
    {
        public static void Present(Window owner, AddOn addOn)
        {
            if (new AddOnLicenseDialog { Owner = owner, DataContext = addOn }.ShowDialog() ?? false)
                addOn.AgreeToLicense();
        }

        public AddOnLicenseDialog() => InitializeComponent();

        void AcceptClickHandler(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
