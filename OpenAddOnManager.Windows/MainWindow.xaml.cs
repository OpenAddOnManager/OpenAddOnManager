using Gear.ActiveQuery;
using MaterialDesignThemes.Wpf;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenAddOnManager.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        void AppWebsiteClickHandler(object sender, RoutedEventArgs e)
        {
            menuPopupBox.IsPopupOpen = false;
            App.OpenInBrowser(new Uri("https://github.com/OpenAddOnManager/OpenAddOnManager"));
        }

        async void CheckForAddOnUpdatesClickHandler(object sender, RoutedEventArgs e)
        {
            var addOnManager = Context.AddOnManager;
            if (addOnManager.ActionState != AddOnManagerActionState.Idle)
                return;
            await addOnManager.UpdateAvailableAddOnsAsync();
        }

        void ClosedHandler(object sender, EventArgs e)
        {
            Context.Dispose();
            ((App)Application.Current).Terminate();
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

        void DownloadAppUpdateClickHandler(object sender, RoutedEventArgs e)
        {
            menuPopupBox.IsPopupOpen = false;
            App.OpenInBrowser(new Uri("https://github.com/OpenAddOnManager/OpenAddOnManager/releases"));
        }

        void EmailAddOnAuthorClickHandler(object sender, RoutedEventArgs e) => App.ComposeEmail(((AddOn)((Button)sender).DataContext).AuthorEmail);

        void InitializeClientTab(Panel clientTabPanel)
        {
            var worldOfWarcraftInstallationClient = (WorldOfWarcraftInstallationClient)clientTabPanel.DataContext;
            var context = Context;
            var clientAddOns = context.AddOnManager.AddOns.ActiveWhere
            (
                addOn
                =>
                addOn.Flavor == worldOfWarcraftInstallationClient.Flavor
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
            if (addOn.ActionState != AddOnActionState.Idle)
                return;
            await addOn.DownloadAsync();
            if (addOn.IsLicensed && !addOn.IsLicenseAgreed)
            {
                if (!(bool)await DialogHost.Show(new AddOnLicenseDialog { DataContext = addOn }))
                {
                    await addOn.DeleteAsync();
                    return;
                }
                addOn.AgreeToLicense();
            }
            if (!addOn.IsLicensed || addOn.IsLicenseAgreed)
                await addOn.InstallAsync();
        }

        async void ListingSourcesClickHandler(object sender, RoutedEventArgs e)
        {
            menuPopupBox.IsPopupOpen = false;
            var addOnManager = Context.AddOnManager;
            var context = new ManifestsDialogContext(addOnManager.ManifestUrls);
            if (await DialogHost.Show(new ManifestsDialog { DataContext = context }) is bool dialogResult && dialogResult)
            {
                var newManifestUrls = context.ManifestUrls.Distinct(StringComparer.OrdinalIgnoreCase).Select(manifestUrl => Uri.TryCreate(manifestUrl, UriKind.Absolute, out var manifestUri) ? manifestUri : null).Where(manifestUri => manifestUri != null);
                ThreadPool.QueueUserWorkItem(async state =>
                {
                    while (addOnManager.ActionState != AddOnManagerActionState.Idle)
                        await Task.Delay(250);
                    await addOnManager.ManifestUrls.ReplaceAllAsync(newManifestUrls);
                    await addOnManager.UpdateAvailableAddOnsAsync();
                });
            }
        }

        void LoadedHandler(object sender, RoutedEventArgs e) => App.SafeguardWindowPosition(this);

        void LocationChangedHandler(object sender, EventArgs e) => ThreadPool.QueueUserWorkItem(async state =>
        {
            await Task.Delay(25).ConfigureAwait(false);
            await App.OnUiThreadAsync(() =>
            {
                if (WindowState == WindowState.Normal)
                {
                    App.MainWindowLeft = Left;
                    App.MainWindowTop = Top;
                }
            });
        });

        void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                App.MainWindowHeight = e.NewSize.Height;
                App.MainWindowWidth = e.NewSize.Width;
            }
        }

        async void UninstallClickHandler(object sender, RoutedEventArgs e)
        {
            var addOn = (AddOn)((Button)sender).DataContext;
            if (addOn.ActionState != AddOnActionState.Idle)
                return;
            await addOn.DeleteAsync();
        }

        async void UpdateAllAddOnsClickHandler(object sender, RoutedEventArgs e)
        {
            var addOnManager = Context.AddOnManager;
            if (addOnManager.ActionState != AddOnManagerActionState.Idle)
                return;
            await addOnManager.UpdateAllAddOnsAsync();
        }

        async void UpdateClickHandler(object sender, RoutedEventArgs e)
        {
            var addOn = (AddOn)((Button)sender).DataContext;
            if (addOn.ActionState != AddOnActionState.Idle)
                return;
            await addOn.InstallAsync();
        }

        void VisitAuthorPageClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).AuthorPageUrl);

        void VisitAddOnPageClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).AddOnPageUrl);

        void VisitSupportPageClickHandler(object sender, RoutedEventArgs e) => App.OpenInBrowser(((AddOn)((Button)sender).DataContext).SupportUrl);

        MainWindowContext Context => DataContext as MainWindowContext;
    }
}
