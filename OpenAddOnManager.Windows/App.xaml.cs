using Gear.NamedPipesSingleInstance;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using OpenAddOnManager.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace OpenAddOnManager.Windows
{
    public partial class App : Application, INotifyPropertyChanged, INotifyPropertyChanging
    {
        static App() => SystemEvents.DisplaySettingsChanged += SystemEventsDisplaySettingsChangedHandler;

        static AddOnManager addOnManager;
        const string runKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string runValueName = "Open Add-On Manager";
        static SynchronizationContext synchronizationContext;
        static WorldOfWarcraftInstallation worldOfWarcraftInstallation;

        public static void ComposeEmail(string address) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{address}",
                UseShellExecute = true,
                Verb = "open"
            });

        static Task CreateMainWindow(bool openMinimized = false) => OnUiThreadAsync(() =>
        {
            var mainWindow = new MainWindow { DataContext = new MainWindowContext(addOnManager) };
            if (openMinimized)
                mainWindow.WindowState = WindowState.Minimized;
            if (MainWindowHeight != null && MainWindowLeft != null && MainWindowTop != null && MainWindowWidth != null)
            {
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                mainWindow.Height = MainWindowHeight.Value;
                mainWindow.Left = MainWindowLeft.Value;
                mainWindow.Top = MainWindowTop.Value;
                mainWindow.Width = MainWindowWidth.Value;
            }
            mainWindow.Show();
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

        public static void SafeguardWindowPosition(Window window)
        {
            var closestWorkingArea = Screen.GetWorkingArea(window);
            if (closestWorkingArea == Rect.Empty)
                return;
            if (window.Width > closestWorkingArea.Width)
                window.Width = closestWorkingArea.Width;
            if (window.Height > closestWorkingArea.Height)
                window.Height = closestWorkingArea.Height;
            if (window.Left < closestWorkingArea.Left)
                window.Left = closestWorkingArea.Left;
            if (window.Top < closestWorkingArea.Top)
                window.Top = closestWorkingArea.Top;
            if (window.Left + window.Width > closestWorkingArea.Right)
                window.Left = closestWorkingArea.Right - window.Width;
            if (window.Top + window.Height > closestWorkingArea.Bottom)
                window.Top = closestWorkingArea.Bottom - window.Height;
        }

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

        static async void SystemEventsDisplaySettingsChangedHandler(object sender, EventArgs e)
        {
            if (Current != null)
                await OnUiThreadAsync(() =>
                {
                    foreach (Window window in Current.Windows)
                        SafeguardWindowPosition(window);
                });
        }

        public static string ExecutablePath
        {
            get
            {
                var path = Uri.UnescapeDataString(new UriBuilder(Assembly.GetEntryAssembly().CodeBase).Path);
                return $"{Path.GetDirectoryName(path)}\\{Path.GetFileName(path).Replace(".dll", ".exe", StringComparison.OrdinalIgnoreCase)}";
            }
        }

        public static double? MainWindowHeight { get; set; }

        public static double? MainWindowLeft { get; set; }

        public static double? MainWindowTop { get; set; }

        public static double? MainWindowWidth { get; set; }

        public static bool ShowPrereleaseVersions { get; set; }

        public static Version Version => Assembly.GetEntryAssembly().GetName().Version;

        public static string VersionMoniker
        {
            get
            {
                var version = Version;
                var moniker = new StringBuilder($"Open Add-On Manager for Windows v{version.Major}.{version.Minor}");
                if (version.Build > 0)
                    moniker.Append($", patch {version.Build}");
                else if (version.Revision > 0)
                    moniker.Append($", revision {version.Revision}");
                return moniker.ToString();
            }
        }

        public App()
        {
            singleInstance = new SingleInstance("openaddonmanager", SecondaryInstanceMessageReceivedHandler);
            notifier = new Notifier(ConfigureNotifier);
        }

        Version availableVersion;
        readonly Notifier notifier;
        readonly SingleInstance singleInstance;
        FileInfo stateFile;
        bool themeIsDark;
        bool themeIsHorde;
        Timer updateAvailableVersion;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        void AddOnManagerAddOnUpdateAvailableHandler(object sender, AddOnEventArgs e)
        {
            var addOn = e.AddOn;
            notifier.Notify(() => new AddOnMessage($"update available {addOn.Key}", new MessageOptions { ShowCloseButton = false })
            {
                AddOn = addOn,
                MessageType = AddOnMessageType.UpdateAvailable
            });
        }

        void AddOnManagerAddOnAutomaticallyUpdatedHandler(object sender, AddOnEventArgs e)
        {
            var addOn = e.AddOn;
            notifier.Notify(() => new AddOnMessage($"updated {addOn.Key}", new MessageOptions { ShowCloseButton = false })
            {
                AddOn = addOn,
                MessageType = AddOnMessageType.UpdateInstalled
            });
        }

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

            updateAvailableVersion = new Timer(UpdateAvailableVersion, null, TimeSpan.Zero, TimeSpan.FromDays(1));

            try
            {
                worldOfWarcraftInstallation = new WorldOfWarcraftInstallation(synchronizationContext: synchronizationContext);
            }
            catch (WorldOfWarcraftInstallationUnavailableException)
            {
                await OnUiThreadAsync(() => MessageBox.Show("Dude and/or madame, you have World of Warcraft installed in an odd place or not at all. I can't manage add-ons for WoW without WoW!", "NOOOOOOOOPE", MessageBoxButton.OK, MessageBoxImage.Error)).ConfigureAwait(false);
                Environment.Exit(0);
            }

            addOnManager = new AddOnManager(await Utilities.GetCommonStorageDirectoryAsync().ConfigureAwait(false), worldOfWarcraftInstallation, synchronizationContext);
            addOnManager.AddOnAutomaticallyUpdated += AddOnManagerAddOnAutomaticallyUpdatedHandler;
            addOnManager.AddOnUpdateAvailable += AddOnManagerAddOnUpdateAvailableHandler;

            stateFile = new FileInfo(Path.Combine(addOnManager.StorageDirectory.FullName, "appState.json"));
            if (stateFile.Exists)
            {
                var appState = JsonConvert.DeserializeObject<AppState>(await File.ReadAllTextAsync(stateFile.FullName).ConfigureAwait(false));
                MainWindowHeight = appState.MainWindowHeight;
                MainWindowLeft = appState.MainWindowLeft;
                MainWindowTop = appState.MainWindowTop;
                MainWindowWidth = appState.MainWindowWidth;
                ShowPrereleaseVersions = appState.ShowPrereleaseVersions;
                themeIsDark = appState.ThemeIsDark;
                themeIsHorde = appState.ThemeIsHorde;
                await OnUiThreadAsync(() => SetTheme()).ConfigureAwait(false);
            }

            await CreateMainWindow(openMinimized: Environment.GetCommandLineArgs().Contains("-startMinimized", StringComparer.OrdinalIgnoreCase)).ConfigureAwait(false);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifier.Dispose();
            singleInstance.Dispose();
        }

        void ConfigureNotifier(NotifierConfiguration cfg)
        {
            cfg.Dispatcher = Dispatcher;
            cfg.DisplayOptions.Width = 400;
            cfg.DisplayOptions.TopMost = true;
            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(10), MaximumNotificationCount.FromCount(5));
            cfg.PositionProvider = new GdiPrimaryScreenPositionProvider(Corner.BottomRight, 0, 0);
        }

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

        public void Terminate() => ThreadPool.QueueUserWorkItem(async state =>
        {
            addOnManager.AddOnAutomaticallyUpdated -= AddOnManagerAddOnAutomaticallyUpdatedHandler;
            addOnManager.AddOnUpdateAvailable -= AddOnManagerAddOnUpdateAvailableHandler;
            addOnManager?.Dispose();
            worldOfWarcraftInstallation?.Dispose();

            await File.WriteAllTextAsync(stateFile.FullName, JsonConvert.SerializeObject(new AppState
            {
                MainWindowHeight = MainWindowHeight,
                MainWindowLeft = MainWindowLeft,
                MainWindowTop = MainWindowTop,
                MainWindowWidth = MainWindowWidth,
                ShowPrereleaseVersions = ShowPrereleaseVersions,
                ThemeIsDark = ThemeIsDark,
                ThemeIsHorde = ThemeIsHorde
            })).ConfigureAwait(false);

            singleInstance?.Dispose();

            await OnUiThreadAsync(() => Shutdown()).ConfigureAwait(false);
        });

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
                case "quit":
                    Terminate();
                    break;
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

        async void UpdateAvailableVersion(object state) => AvailableVersion = new Version(await AddOnManager.UsingHttpClient(httpClient => httpClient.GetStringAsync("https://raw.githubusercontent.com/OpenAddOnManager/OpenAddOnManager/master/OpenAddOnManager.Windows/VERSION")).ConfigureAwait(false));

        public Version AvailableVersion
        {
            get => availableVersion;
            private set => SetBackedProperty(ref availableVersion, in value);
        }

        public bool RunAtStartup
        {
            get
            {
                using (var run = Registry.CurrentUser.OpenSubKey(runKeyPath))
                {
                    var runValue = run?.GetValue(runValueName, null);
                    return runValue != null && runValue is string && ((string)runValue).IndexOf(ExecutablePath, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            set
            {
                var currentRunAtStartup = RunAtStartup;
                using (var runKey = Registry.CurrentUser.CreateSubKey(runKeyPath))
                {
                    if (value && !currentRunAtStartup)
                        runKey.SetValue(runValueName, $"\"{ExecutablePath}\" -startMinimized");
                    else if (!value && currentRunAtStartup)
                        runKey.DeleteValue(runValueName, false);
                }
            }
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
