using Gear.ActiveQuery;
using Gear.Components;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public class AddOnManager : SyncDisposablePropertyChangeNotifier
    {
        static readonly TimeSpan never = TimeSpan.FromMilliseconds(-1);

        public static IReadOnlyList<Uri> DefaultManifestUrls { get; } = new Uri[]
        {
            new Uri("https://raw.githubusercontent.com/OpenAddOnManager/OpenAddOnManager/master/addOns.json")
        }.ToImmutableArray();

        public static TimeSpan MinimumManifestsCheckFrequency { get; } = TimeSpan.FromMinutes(5);

        public AddOnManager(DirectoryInfo storageDirectory, IWorldOfWarcraftInstallation worldOfWarcraftInstallation, SynchronizationContext synchronizationContext = null)
        {
            manifestsCheckTimer = new Timer(ManifestsCheckTimerCallback);

            WorldOfWarcraftInstallation = worldOfWarcraftInstallation;
            addOns = new SynchronizedObservableDictionary<Guid, AddOn>();
            addOnsWithUpdateAvailable = addOns.ActiveCount((addOnKey, addOn) => addOn.IsUpdateAvailable);
            addOnsWithUpdateAvailable.PropertyChanged += AddOnsWithUpdateAvailablePropertyChanged;
            addOnsWithUpdateAvailable.PropertyChanging += AddOnsWithUpdateAvailablePropertyChanging;
            addOnsActiveEnumerable = addOns.ToActiveEnumerable();
            ManifestUrls = new SynchronizedRangeObservableCollection<Uri>(DefaultManifestUrls);
            AddOns = synchronizationContext == null ? addOnsActiveEnumerable : addOnsActiveEnumerable.SwitchContext(synchronizationContext);
            StorageDirectory = storageDirectory;
            if (StorageDirectory != null)
            {
                stateFile = new FileInfo(Path.Combine(StorageDirectory.FullName, "addOnManagerState.json"));
                AddOnsDirectory = new DirectoryInfo(Path.Combine(StorageDirectory.FullName, "AddOnRepositories"));
            }

            initializationCompleteTaskCompletionSource = new TaskCompletionSource<object>();
            InitializationComplete = initializationCompleteTaskCompletionSource.Task;
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        AddOnManagerActionState actionState = AddOnManagerActionState.Idle;
        readonly SynchronizedObservableDictionary<Guid, AddOn> addOns;
        readonly IActiveEnumerable<AddOn> addOnsActiveEnumerable;
        readonly IActiveValue<int> addOnsWithUpdateAvailable;
        bool automaticallyUpdateAddOns;
        readonly TaskCompletionSource<object> initializationCompleteTaskCompletionSource;
        DateTimeOffset lastUpdatesCheck;
        TimeSpan manifestsCheckFrequency = TimeSpan.FromDays(1);
        readonly Timer manifestsCheckTimer;
        readonly AsyncLock saveStateAccess = new AsyncLock();
        readonly FileInfo stateFile;

        void AddOnsWithUpdateAvailablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IActiveValue<int>.Value))
                OnPropertyChanged(nameof(AddOnsWithUpdateAvailable));
        }

        void AddOnsWithUpdateAvailablePropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(IActiveValue<int>.Value))
                OnPropertyChanged(nameof(AddOnsWithUpdateAvailable));
        }

        HttpClient CreateHttpClient()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                CookieContainer = new CookieContainer(),
                UseCookies = true
            });
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"OpenAddOnManager/{assembly.Major}.{assembly.Minor}");
            return httpClient;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                manifestsCheckTimer?.Dispose();
                ManifestUrls.GenericCollectionChanged -= ManifestUrlsGenericCollectionChangedHandler;
                AddOns?.Dispose();
                addOnsActiveEnumerable?.Dispose();
                addOnsWithUpdateAvailable?.Dispose();
            }
        }

        void Initialize(object state)
        {
            try
            {
                if (stateFile != null)
                {
                    stateFile.Refresh();
                    if (stateFile.Exists)
                    {
                        AddOnManagerState addOnManagerState;
                        using (var streamReader = File.OpenText(stateFile.FullName))
                        using (var jsonReader = new JsonTextReader(streamReader))
                            addOnManagerState = JsonSerializer.CreateDefault().Deserialize<AddOnManagerState>(jsonReader);
                        automaticallyUpdateAddOns = addOnManagerState.AutomaticallyUpdateAddOns;
                        lastUpdatesCheck = addOnManagerState.LastUpdatesCheck;
                        if (addOnManagerState.ManifestsCheckFrequency >= MinimumManifestsCheckFrequency)
                            manifestsCheckFrequency = addOnManagerState.ManifestsCheckFrequency;
                        ManifestUrls.Reset(addOnManagerState.ManifestUrls);
                        ManifestUrls.GenericCollectionChanged += ManifestUrlsGenericCollectionChangedHandler;
                    }
                }
                if (AddOnsDirectory != null)
                {
                    AddOnsDirectory.Refresh();
                    if (!AddOnsDirectory.Exists)
                        AddOnsDirectory.Create();
                    addOns.AddRange
                    (
                        AddOnsDirectory.GetFiles("*.json")
                            .Select(file => Guid.TryParse(file.Name[0..^5], out var key) ? key : default)
                            .Where(key => key != default)
                            .Select(key => new KeyValuePair<Guid, AddOn>(key, new AddOn(this, key, true)))
                    );
                }
                ScheduleNextManifestsCheck();
                initializationCompleteTaskCompletionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                initializationCompleteTaskCompletionSource.SetException(ex);
            }
        }

        async void ManifestsCheckTimerCallback(object state)
        {
            try
            {
                await UpdateAvailableAddOnsAsync().ConfigureAwait(false);
                if (automaticallyUpdateAddOns && addOnsWithUpdateAvailable.Value > 0)
                    await UpdateAllAddOns().ConfigureAwait(false);
            }
            finally
            {
                ScheduleNextManifestsCheck();
            }
        }

        void ManifestUrlsGenericCollectionChangedHandler(object sender, INotifyGenericCollectionChangedEventArgs<Uri> e) => SaveState();

        void SaveState() => ThreadPool.QueueUserWorkItem(async state => await SaveStateAsync());

        async Task SaveStateAsync()
        {
            if (stateFile != null)
                using (await saveStateAccess.LockAsync().ConfigureAwait(false))
                using (var streamWriter = File.CreateText(stateFile.FullName))
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                    JsonSerializer.CreateDefault().Serialize(jsonWriter, new AddOnManagerState
                    {
                        AutomaticallyUpdateAddOns = automaticallyUpdateAddOns,
                        LastUpdatesCheck = lastUpdatesCheck,
                        ManifestsCheckFrequency = manifestsCheckFrequency,
                        ManifestUrls = (await ManifestUrls.GetAllAsync().ConfigureAwait(false)).ToList()
                    });
        }

        void ScheduleNextManifestsCheck()
        {
            var nextCheckIn = manifestsCheckFrequency - (DateTimeOffset.Now - lastUpdatesCheck);
            if (nextCheckIn < TimeSpan.Zero)
                nextCheckIn = TimeSpan.Zero;
            manifestsCheckTimer.Change(nextCheckIn, never);
        }

        public Task UpdateAllAddOns() => Task.Run(async () =>
        {
            ActionState = AddOnManagerActionState.UpdatingAllAddOns;
            try
            {
                var updatingTasks = new List<Task>();
                foreach (var addOnKey in await addOns.GetAllKeysAsync().ConfigureAwait(false))
                {
                    var (addOnRetrieved, addOn) = await addOns.TryGetValueAsync(addOnKey).ConfigureAwait(false);
                    if (addOnRetrieved && addOn.IsUpdateAvailable)
                        updatingTasks.Add(addOn.InstallAsync());
                }
                await Task.WhenAll(updatingTasks).ConfigureAwait(false);
            }
            finally
            {
                ActionState = AddOnManagerActionState.Idle;
            }
        });

        public Task UpdateAvailableAddOnsAsync() => Task.Run(async () =>
        {
            ActionState = AddOnManagerActionState.CheckingForAddOnUpdates;
            try
            {
                var addOnKeysInManifests = new HashSet<Guid>();
                var jsonSerializer = JsonSerializer.CreateDefault();
                using (var httpClient = CreateHttpClient())
                    foreach (var manifestUrl in await ManifestUrls.GetAllAsync().ConfigureAwait(false))
                    {
                        try
                        {
                            using (var responseStream = await httpClient.GetStreamAsync(manifestUrl).ConfigureAwait(false))
                            using (var responseStreamReader = new StreamReader(responseStream))
                            using (var responseJsonTextReader = new JsonTextReader(responseStreamReader))
                            {
                                await responseJsonTextReader.ReadAsync().ConfigureAwait(false);
                                if (responseJsonTextReader.TokenType != JsonToken.StartObject)
                                    throw new FormatException();
                                while ((await responseJsonTextReader.ReadAsync().ConfigureAwait(false)) && responseJsonTextReader.TokenType == JsonToken.PropertyName)
                                {
                                    if (!Guid.TryParse((string)responseJsonTextReader.Value, out var addOnKey))
                                        throw new FormatException();
                                    await responseJsonTextReader.ReadAsync().ConfigureAwait(false);
                                    var entry = jsonSerializer.Deserialize<AddOnManifestEntry>(responseJsonTextReader);
                                    addOnKeysInManifests.Add(addOnKey);
                                    if (addOns.TryGetValue(addOnKey, out var addOn))
                                        await addOn.UpdatePropertiesFromManifestEntryAsync(entry).ConfigureAwait(false);
                                    else
                                        addOns.Add(addOnKey, new AddOn(this, addOnKey, entry));
                                }
                            }
                        }
                        catch (HttpRequestException)
                        {
                            // TODO: tell user manifest is bad
                        }
                    }
                await addOns.RemoveAllAsync((addOnKey, addOn) => !addOnKeysInManifests.Contains(addOnKey)).ConfigureAwait(false);
                var downloadingTasks = new List<Task>();
                foreach (var addOnKey in await addOns.GetAllKeysAsync().ConfigureAwait(false))
                {
                    var (addOnRetrieved, addOn) = await addOns.TryGetValueAsync(addOnKey).ConfigureAwait(false);
                    if (addOnRetrieved && addOn.IsDownloaded)
                        downloadingTasks.Add(addOn.DownloadAsync());
                }
                await Task.WhenAll(downloadingTasks).ConfigureAwait(false);
            }
            finally
            {
                LastUpdatesCheck = DateTimeOffset.Now;
                ActionState = AddOnManagerActionState.Idle;
            }
        });

        public AddOnManagerActionState ActionState
        {
            get => actionState;
            private set => SetBackedProperty(ref actionState, in value);
        }

        public IActiveEnumerable<AddOn> AddOns { get; }

        public DirectoryInfo AddOnsDirectory { get; }

        public int AddOnsWithUpdateAvailable => addOnsWithUpdateAvailable.Value;

        public bool AutomaticallyUpdateAddOns
        {
            get => automaticallyUpdateAddOns;
            set
            {
                if (SetBackedProperty(ref automaticallyUpdateAddOns, in value))
                    SaveState();
            }
        }

        public Task InitializationComplete { get; }

        public DateTimeOffset LastUpdatesCheck
        {
            get => lastUpdatesCheck;
            private set
            {
                if (SetBackedProperty(ref lastUpdatesCheck, in value))
                    SaveState();
            }
        }

        public TimeSpan ManifestsCheckFrequency
        {
            get => manifestsCheckFrequency;
            set
            {
                if (value < MinimumManifestsCheckFrequency)
                    throw new ArgumentOutOfRangeException();
                if (SetBackedProperty(ref manifestsCheckFrequency, in value))
                {
                    SaveState();
                    ScheduleNextManifestsCheck();
                }
            }
        }

        public SynchronizedRangeObservableCollection<Uri> ManifestUrls { get; }

        public DirectoryInfo StorageDirectory { get; }

        public IWorldOfWarcraftInstallation WorldOfWarcraftInstallation { get; }
    }
}
