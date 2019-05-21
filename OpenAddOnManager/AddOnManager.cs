using Gear.ActiveQuery;
using Gear.Components;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
            AddOns = new ReadOnlySynchronizedObservableRangeDictionary<Guid, AddOn>(addOns);
            addOnsForBinding = addOns.ToActiveEnumerable();
            AddOnsForBinding = synchronizationContext == null ? addOnsForBinding : addOnsForBinding.SwitchContext(synchronizationContext);
            ManifestUrls = new ReadOnlyObservableCollection<Uri>(manifestUrls);
            StorageDirectory = storageDirectory;
            if (StorageDirectory != null)
                AddOnsDirectory = new DirectoryInfo(Path.Combine(StorageDirectory.FullName, "AddOnRepositories"));

            initializationCompleteTaskCompletionSource = new TaskCompletionSource<object>();
            InitializationComplete = initializationCompleteTaskCompletionSource.Task;
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        readonly SynchronizedObservableDictionary<Guid, AddOn> addOns;
        readonly IActiveEnumerable<AddOn> addOnsForBinding;
        readonly HttpClientHandler httpClientHandler;
        readonly TaskCompletionSource<object> initializationCompleteTaskCompletionSource;
        readonly ObservableCollection<Uri> manifestUrls = new ObservableCollection<Uri>(DefaultManifestUrls);
        readonly AsyncLock manifestUrlsAccess = new AsyncLock();
        Timer updateAvailableAddOnsTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AddOnsForBinding?.Dispose();
                addOnsForBinding?.Dispose();
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
                if (AddOnsDirectory != null)
                {
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

        void InitializeUpdateAvailableAddOnsTimer() => updateAvailableAddOnsTimer = new Timer(UpdateAvailableAddOnsTimerCallback, null, UpdateAvailableAddOnsTimerDuration, UpdateAvailableAddOnsTimerDuration);

        public Task UpdateAvailableAddOnsAsync() => Task.Run(async () =>
        {
            IReadOnlyList<Uri> manifestUrls;
            using (await manifestUrlsAccess.LockAsync().ConfigureAwait(false))
                manifestUrls = this.manifestUrls.ToImmutableArray();
            var jsonSerializer = JsonSerializer.CreateDefault();
            using (var httpClient = CreateHttpClient())
                foreach (var manifestUrl in manifestUrls)
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
        });

        async void UpdateAvailableAddOnsTimerCallback(object state) => await UpdateAvailableAddOnsAsync().ConfigureAwait(false);

        public ReadOnlySynchronizedObservableRangeDictionary<Guid, AddOn> AddOns { get; }

        public DirectoryInfo AddOnsDirectory { get; }

        public IActiveEnumerable<AddOn> AddOnsForBinding { get; }

        public Task InitializationComplete { get; }

        public ReadOnlyObservableCollection<Uri> ManifestUrls { get; }

        public DirectoryInfo StorageDirectory { get; }

        public IWorldOfWarcraftInstallation WorldOfWarcraftInstallation { get; }
    }
}
