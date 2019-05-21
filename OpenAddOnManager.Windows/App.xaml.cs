using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenAddOnManager.Windows
{
    public partial class App : Application
    {
        static SynchronizationContext synchronizationContext;

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

        async void Initialize(object state)
        {
            var worldOfWarcraftInstallation = new WorldOfWarcraftInstallation(synchronizationContext: synchronizationContext);
            var addOnManager = new AddOnManager(await Utilities.GetCommonStorageDirectoryAsync().ConfigureAwait(false), worldOfWarcraftInstallation, synchronizationContext);
            await OnUiThreadAsync(() => new MainWindow { DataContext = addOnManager }.Show());
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            synchronizationContext = SynchronizationContext.Current;
            ThreadPool.QueueUserWorkItem(Initialize);
            base.OnStartup(e);
        }
    }
}
