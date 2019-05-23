using Gear.ActiveQuery;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenAddOnManager.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private void ClosedHandler(object sender, EventArgs e)
        {
            var worldOfWarcraftInstallation = Context.AddOnManager?.WorldOfWarcraftInstallation;
            Context.AddOnManager?.Dispose();
            Context.Dispose();
            (worldOfWarcraftInstallation as IDisposable)?.Dispose();
            Application.Current.Shutdown();
        }

        void CleanUpClientTab(Panel clientTabPanel)
        {
            ((IDisposable)((ItemsControl)clientTabPanel.FindName("addOnsList"))?.ItemsSource)?.Dispose();
            ((IDisposable)clientTabPanel.Resources["clientAddOns"])?.Dispose();
            clientTabPanel.Resources.Remove("clientAddOns");
        }

        void ClientTabDataContextChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            var clientTabPanel = (Panel)sender;
            CleanUpClientTab(clientTabPanel);
            InitializeClientTab(clientTabPanel);
        }

        void ClientTabLoadedHandler(object sender, RoutedEventArgs e) => InitializeClientTab((Panel)sender);

        void ClientTabUnloadedHandler(object sender, RoutedEventArgs e) => CleanUpClientTab((Panel)sender);

        void DonateClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).DonationsUrl);

        void EmailAddOnAuthorClickHandler(object sender, RoutedEventArgs e) => App.ComposeEmail(((AddOn)((Button)sender).DataContext).AuthorEmail);

        void InitializeClientTab(Panel clientTabPanel)
        {
            var worldOfWarcraftInstallationClient = (WorldOfWarcraftInstallationClient)clientTabPanel.DataContext;
            var clientAddOns = Context.AddOnManager.AddOns.ActiveWhere
            (
                addOn
                =>
                addOn.ReleaseChannelId == worldOfWarcraftInstallationClient.ReleaseChannelId
                &&
                (
                    addOn.IsInstalled
                    ||
                    !addOn.IsPrereleaseVersion
                    ||
                    Context.ShowPrereleaseVersions
                )
                &&
                (
                    string.IsNullOrWhiteSpace(Context.SearchFor)
                    ||
                    addOn.AuthorName.Contains(Context.SearchFor.Trim(), StringComparison.OrdinalIgnoreCase)
                    ||
                    addOn.Description.Contains(Context.SearchFor.Trim(), StringComparison.OrdinalIgnoreCase)
                    ||
                    addOn.Name.Contains(Context.SearchFor.Trim(), StringComparison.OrdinalIgnoreCase)
                )
            );
            clientTabPanel.Resources.Add("clientAddOns", clientAddOns);
            ((ItemsControl)clientTabPanel.FindName("addOnsList")).ItemsSource = clientAddOns.ActiveOrderBy(new ActiveOrderingKeySelector<AddOn>(addOn => addOn.IsInstalled, true), new ActiveOrderingKeySelector<AddOn>(addOn => addOn.Name), new ActiveOrderingKeySelector<AddOn>(addOn => addOn.IsPrereleaseVersion));
        }

        async void InstallClickHandler(object sender, RoutedEventArgs e)
        {
            var addOn = (AddOn)((Button)sender).DataContext;
            await addOn.DownloadAsync();
            if (addOn.IsLicensed && !addOn.IsLicenseAgreed)
                AddOnLicenseDialog.Present(this, addOn);
            if (!addOn.IsLicensed || addOn.IsLicenseAgreed)
                await addOn.InstallAsync();
        }

        async void UninstallClickHandler(object sender, RoutedEventArgs e)
        {
            var addOn = (AddOn)((Button)sender).DataContext;
            await addOn.DeleteAsync();
        }

        async void UpdateClickHandler(object sender, RoutedEventArgs e) => await ((AddOn)((Button)sender).DataContext).InstallAsync();

        void VisitAuthorPageClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).AuthorPageUrl);

        void VisitAddOnPageClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).AddOnPageUrl);

        void VisitSupportPageClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).SupportUrl);

        MainWindowContext Context => DataContext as MainWindowContext;
    }
}
