using Gear.ActiveQuery;
using Gear.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public static IReadOnlyList<Uri> DefaultManifestUrls { get; } = new Uri[]
        {
            new Uri("https://raw.githubusercontent.com/OpenAddOnManager/OpenAddOnManager/master/addOns.json")
        }.ToImmutableArray();

        public static TimeSpan UpdateAvailableAddOnsTimerDuration { get; } = TimeSpan.FromDays(1);

        public AddOnManager(DirectoryInfo storageDirectory, IWorldOfWarcraftInstallation worldOfWarcraftInstallation, SynchronizationContext synchronizationContext = null)
        {
            httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };
            WorldOfWarcraftInstallation = worldOfWarcraftInstallation;
            addOns = new SynchronizedObservableDictionary<Guid, AddOn>();
            addOnsActiveEnumerable = addOns.ToActiveEnumerable();
            ManifestUrls = new SynchronizedRangeObservableCollection<Uri>(DefaultManifestUrls);
            AddOns = synchronizationContext == null ? addOnsActiveEnumerable : addOnsActiveEnumerable.SwitchContext(synchronizationContext);
            StorageDirectory = storageDirectory;
            if (StorageDirectory != null)
            {
                stateFile = new FileInfo(Path.Combine(StorageDirectory.FullName, "state.json"));
                AddOnsDirectory = new DirectoryInfo(Path.Combine(StorageDirectory.FullName, "AddOnRepositories"));
            }

            initializationCompleteTaskCompletionSource = new TaskCompletionSource<object>();
            InitializationComplete = initializationCompleteTaskCompletionSource.Task;
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        readonly SynchronizedObservableDictionary<Guid, AddOn> addOns;
        readonly IActiveEnumerable<AddOn> addOnsActiveEnumerable;
        readonly HttpClientHandler httpClientHandler;
        readonly TaskCompletionSource<object> initializationCompleteTaskCompletionSource;
        readonly FileInfo stateFile;
        Timer updateAvailableAddOnsTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ManifestUrls.GenericCollectionChanged -= ManifestUrlsGenericCollectionChangedHandler;
                AddOns?.Dispose();
                addOnsActiveEnumerable?.Dispose();
                updateAvailableAddOnsTimer?.Dispose();
            }
        }

        HttpClient CreateHttpClient()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var httpClient = new HttpClient(httpClientHandler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"OpenAddOnManager/{assembly.Major}.{assembly.Minor}");
            return httpClient;
        }

        async void Initialize(object state)
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
                await UpdateAvailableAddOnsAsync().ConfigureAwait(false);
                InitializeUpdateAvailableAddOnsTimer();
                initializationCompleteTaskCompletionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                initializationCompleteTaskCompletionSource.SetException(ex);
            }
        }

        void ManifestUrlsGenericCollectionChangedHandler(object sender, INotifyGenericCollectionChangedEventArgs<Uri> e) => ThreadPool.QueueUserWorkItem(async state => await SaveStateAsync());

        void InitializeUpdateAvailableAddOnsTimer() => updateAvailableAddOnsTimer = new Timer(UpdateAvailableAddOnsTimerCallback, null, UpdateAvailableAddOnsTimerDuration, UpdateAvailableAddOnsTimerDuration);

        async Task SaveStateAsync()
        {
            if (stateFile != null)
                using (var streamWriter = File.CreateText(stateFile.FullName))
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                    JsonSerializer.CreateDefault().Serialize(jsonWriter, new AddOnManagerState
                    {
                        ManifestUrls = (await ManifestUrls.GetAllAsync().ConfigureAwait(false)).ToList()
                    });
        }

        public Task UpdateAvailableAddOnsAsync() => Task.Run(async () =>
        {
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
                                if (!Guid.TryParse((string)responseJsonTextReader.Value, out var addOnsKey))
                                    throw new FormatException();
                                await responseJsonTextReader.ReadAsync().ConfigureAwait(false);
                                var entry = jsonSerializer.Deserialize<AddOnManifestEntry>(responseJsonTextReader);
                                if (addOns.TryGetValue(addOnsKey, out var addOn))
                                    await addOn.UpdatePropertiesFromManifestEntryAsync(entry).ConfigureAwait(false);
                                else
                                    addOns.Add(addOnsKey, new AddOn(this, addOnsKey, entry));
                            }
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // TODO: tell user manifest is bad
                    }
                }
        });

        async void UpdateAvailableAddOnsTimerCallback(object state) => await UpdateAvailableAddOnsAsync().ConfigureAwait(false);

        public IActiveEnumerable<AddOn> AddOns { get; }

        public DirectoryInfo AddOnsDirectory { get; }

        public Task InitializationComplete { get; }

        public SynchronizedRangeObservableCollection<Uri> ManifestUrls { get; }

        public DirectoryInfo StorageDirectory { get; }

        public IWorldOfWarcraftInstallation WorldOfWarcraftInstallation { get; }
    }
}
