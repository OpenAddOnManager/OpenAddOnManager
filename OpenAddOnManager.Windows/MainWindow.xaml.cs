using Gear.ActiveQuery;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenAddOnManager.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        void ClosedHandler(object sender, EventArgs e)
        {
            Context.Dispose();
            Application.Current.Shutdown();
        }

        void CleanUpClientTab(Panel clientTabPanel)
        {
            ((IDisposable)((ItemsControl)clientTabPanel.FindName("addOnsList"))?.ItemsSource)?.Dispose();
            ((IDisposable)clientTabPanel.Resources["clientAddOns"])?.Dispose();
            clientTabPanel.Resources.Remove("clientAddOns");
        }

        void ClientTabCheckedHandler(object sender, RoutedEventArgs e) => Context.SelectedClient = (IWorldOfWarcraftInstallationClient)((RadioButton)sender).DataContext;

        void ClientTabLoadedHandler(object sender, RoutedEventArgs e) => InitializeClientTab((Panel)sender);

        void ClientTabUnloadedHandler(object sender, RoutedEventArgs e) => CleanUpClientTab((Panel)sender);

        void DonateClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).DonationsUrl);

        void EmailAddOnAuthorClickHandler(object sender, RoutedEventArgs e) => App.ComposeEmail(((AddOn)((Button)sender).DataContext).AuthorEmail);

        void InitializeClientTab(Panel clientTabPanel)
        {
            var worldOfWarcraftInstallationClient = (WorldOfWarcraftInstallationClient)clientTabPanel.DataContext;
            var context = Context;
            var clientAddOns = context.AddOnManager.AddOns.ActiveWhere
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
                    context.ShowPrereleaseVersions
                )
                &&
                (
                    string.IsNullOrWhiteSpace(context.SearchFor)
                    ||
                    addOn.AuthorName.Contains(context.SearchFor.Trim(), StringComparison.OrdinalIgnoreCase)
                    ||
                    addOn.Description.Contains(context.SearchFor.Trim(), StringComparison.OrdinalIgnoreCase)
                    ||
                    addOn.Name.Contains(context.SearchFor.Trim(), StringComparison.OrdinalIgnoreCase)
                )
            );
            clientTabPanel.Resources.Add("clientAddOns", clientAddOns);
            ((ItemsControl)clientTabPanel.FindName("addOnsList")).ItemsSource = clientAddOns.ActiveOrderBy(new ActiveOrderingKeySelector<AddOn>(addOn => addOn.IsUpdateAvailable, true), new ActiveOrderingKeySelector<AddOn>(addOn => addOn.IsInstalled, true), new ActiveOrderingKeySelector<AddOn>(addOn => addOn.Name), new ActiveOrderingKeySelector<AddOn>(addOn => addOn.IsPrereleaseVersion));
        }

        async void InstallClickHandler(object sender, RoutedEventArgs e)
        {
            var addOn = (AddOn)((Button)sender).DataContext;
            await addOn.DownloadAsync();
            if (addOn.IsLicensed && !addOn.IsLicenseAgreed && (bool)await DialogHost.Show(new AddOnLicenseDialog { DataContext = addOn }))
                addOn.AgreeToLicense();
            if (!addOn.IsLicensed || addOn.IsLicenseAgreed)
                await addOn.InstallAsync();
        }

        async void RefreshListingsClickHandler(object sender, RoutedEventArgs e) => await Context.AddOnManager.UpdateAvailableAddOnsAsync();

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
