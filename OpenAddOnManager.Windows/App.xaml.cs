using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenAddOnManager.Windows
{
    public partial class App : Application
    {
        static SynchronizationContext synchronizationContext;

        public static void ComposeEmail(string address) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{address}",
                UseShellExecute = true,
                Verb = "open"
            });

        public static async Task OnUiThreadAsync(Action action)
        {
            if (Current.Dispatcher.CheckAccess())
            {
                action();
                return;
            }
            await Current.Dispatcher.InvokeAsync(action);
        }

        public static async Task OnUiThreadAsync(Func<Task> asyncAction)
        {
            if (Current.Dispatcher.CheckAccess())
                await asyncAction().ConfigureAwait(false);
            else
                await Current.Dispatcher.InvokeAsync(async () => await asyncAction().ConfigureAwait(false));
        }

        public static async Task<T> OnUiThreadAsync<T>(Func<Task<T>> asyncFunc)
        {
            if (Current.Dispatcher.CheckAccess())
                return await asyncFunc().ConfigureAwait(false);
            T result = default;
            await Current.Dispatcher.InvokeAsync(async () => result = await asyncFunc().ConfigureAwait(false));
            return result;
        }

        public static async Task<T> OnUiThreadAsync<T>(Func<T> func) => Current.Dispatcher.CheckAccess() ? func() : await Current.Dispatcher.InvokeAsync(func);

        public static void OpenInBrowser(Uri url) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = url.ToString(),
                UseShellExecute = true,
                Verb = "open"
            });

        async void Initialize(object state)
        {
            var worldOfWarcraftInstallation = new WorldOfWarcraftInstallation(synchronizationContext: synchronizationContext);
            var addOnManager = new AddOnManager(await Utilities.GetCommonStorageDirectoryAsync().ConfigureAwait(false), worldOfWarcraftInstallation, synchronizationContext);
            await OnUiThreadAsync(() => new MainWindow { DataContext = new MainWindowContext(addOnManager) }.Show());
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            synchronizationContext = SynchronizationContext.Current;
            ThreadPool.QueueUserWorkItem(Initialize);
            base.OnStartup(e);
        }
    }
}
