using Gear.NamedPipesSingleInstance;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenAddOnManager.Windows
{
    public partial class App : Application, INotifyPropertyChanged, INotifyPropertyChanging
    {
        static AddOnManager addOnManager;
        static SynchronizationContext synchronizationContext;
        static WorldOfWarcraftInstallation worldOfWarcraftInstallation;

        public static void ComposeEmail(string address) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{address}",
                UseShellExecute = true,
                Verb = "open"
            });

        static Task CreateMainWindow() => OnUiThreadAsync(() => new MainWindow { DataContext = new MainWindowContext(addOnManager) }.Show());

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

        public static Task ShowMainWindowAsync() => OnUiThreadAsync(() =>
        {
            var mainWindow = Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null)
            {
                if (!mainWindow.IsVisible)
                    mainWindow.Show();
                else if (mainWindow.WindowState == WindowState.Minimized)
                    mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        });

        public App() => singleInstance = new SingleInstance("openaddonmanager", SecondaryInstanceMessageReceivedHandler);

        readonly SingleInstance singleInstance;
        bool themeIsDark;
        bool themeIsHorde;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e) => PropertyChanging?.Invoke(this, e);

        protected void OnPropertyChanging([CallerMemberName] string propertyName = null) => OnPropertyChanging(new PropertyChangingEventArgs(propertyName));

        async void Initialize(object state)
        {
            if (!singleInstance.IsFirstInstance)
            {
                await singleInstance.SendMessageAsync("showmainwindow");
                Environment.Exit(0);
            }

            worldOfWarcraftInstallation = new WorldOfWarcraftInstallation(synchronizationContext: synchronizationContext);
            addOnManager = new AddOnManager(await Utilities.GetCommonStorageDirectoryAsync().ConfigureAwait(false), worldOfWarcraftInstallation, synchronizationContext);

            await CreateMainWindow().ConfigureAwait(false);
        }

        protected override void OnExit(ExitEventArgs e) => singleInstance.Dispose();

        protected override void OnStartup(StartupEventArgs e)
        {
            SetTheme();

            var wnd = new Window
            {
                Height = 0,
                ShowInTaskbar = false,
                Width = 0,
                WindowStyle = WindowStyle.None
            };
            wnd.Show();
            wnd.Hide();

            synchronizationContext = SynchronizationContext.Current;
            ThreadPool.QueueUserWorkItem(Initialize);
            base.OnStartup(e);
        }

        void ScheduleSetTheme() => ThreadPool.QueueUserWorkItem(async state => await OnUiThreadAsync(() => SetTheme()));

        protected bool SetBackedProperty<TValue>(ref TValue backingField, in TValue value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TValue>.Default.Equals(backingField, value))
            {
                OnPropertyChanging(propertyName);
                backingField = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        async Task SecondaryInstanceMessageReceivedHandler(object message)
        {
            switch (message)
            {
                case "showmainwindow":
                    await ShowMainWindowAsync();
                    break;
            }
        }

        void SetTheme()
        {
            Color primary = default, accent = default;
            if (themeIsHorde)
            {
                primary = SwatchHelper.Lookup[MaterialDesignColor.Red900];
                accent = Colors.Black;
            }
            else
            {
                primary = SwatchHelper.Lookup[MaterialDesignColor.Blue800];
                accent = SwatchHelper.Lookup[MaterialDesignColor.Yellow];
            }
            var theme = Theme.Create(themeIsDark ? (IBaseTheme)new MaterialDesignDarkTheme() : new MaterialDesignLightTheme(), primary, accent);
            Current.Resources.SetTheme(theme);
        }

        public bool ThemeIsDark
        {
            get => themeIsDark;
            set
            {
                if (SetBackedProperty(ref themeIsDark, in value))
                    ScheduleSetTheme();
            }
        }

        public bool ThemeIsHorde
        {
            get => themeIsHorde;
            set
            {
                if (SetBackedProperty(ref themeIsHorde, in value))
                    ScheduleSetTheme();
            }
        }
    }
}
