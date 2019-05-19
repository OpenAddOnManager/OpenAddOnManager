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
            new Uri("https://raw.githubusercontent.com/BigBadBleuCheese/OpenAddOnManager/master/addOns.json")
        }.ToImmutableArray();

        public static TimeSpan UpdateAvailableAddOnsTimerDuration { get; } = TimeSpan.FromDays(1);

        public static Task<AddOnManager> StartAsync(DirectoryInfo storageDirectory, IWorldOfWarcraftInstallation worldOfWarcraftInstallation) => Task.Run(async () =>
        {
            var httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };
            var manager = new AddOnManager(storageDirectory, httpClientHandler, worldOfWarcraftInstallation);
            await manager.UpdateAvailableAddOnsAsync().ConfigureAwait(false);
            manager.InitializeUpdateAvailableAddOnsTimer();
            return manager;
        });

        AddOnManager(DirectoryInfo storageDirectory, HttpClientHandler httpClientHandler, IWorldOfWarcraftInstallation worldOfWarcraftInstallation)
        {
            this.httpClientHandler = httpClientHandler;
            this.worldOfWarcraftInstallation = worldOfWarcraftInstallation;
            AddOns = new ReadOnlyObservableRangeDictionary<Guid, AddOn>(addOns);
            ManifestUrls = new ReadOnlyObservableCollection<Uri>(manifestUrls);
            StorageDirectory = storageDirectory;
            if (StorageDirectory != null)
            {
                AddOnsDirectory = new DirectoryInfo(Path.Combine(StorageDirectory.FullName, "AddOnRepositories"));
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
        }

        readonly ObservableDictionary<Guid, AddOn> addOns = new ObservableDictionary<Guid, AddOn>();
        readonly AsyncLock addOnsAccess = new AsyncLock();
        readonly HttpClientHandler httpClientHandler;
        readonly ObservableCollection<Uri> manifestUrls = new ObservableCollection<Uri>(DefaultManifestUrls);
        readonly AsyncLock manifestUrlsAccess = new AsyncLock();
        Timer updateAvailableAddOnsTimer;
        readonly IWorldOfWarcraftInstallation worldOfWarcraftInstallation;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                updateAvailableAddOnsTimer?.Dispose();
        }

        HttpClient CreateHttpClient()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var httpClient = new HttpClient(httpClientHandler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"OpenAddOnManager/{assembly.Major}.{assembly.Minor}");
            return httpClient;
        }

        void InitializeUpdateAvailableAddOnsTimer() => updateAvailableAddOnsTimer = new Timer(UpdateAvailableAddOnsTimerCallback, null, UpdateAvailableAddOnsTimerDuration, UpdateAvailableAddOnsTimerDuration);

        public Task UpdateAvailableAddOnsAsync() => Task.Run(async () =>
        {
            IReadOnlyList<Uri> manifestUrls;
            using (await manifestUrlsAccess.LockAsync().ConfigureAwait(false))
                manifestUrls = this.manifestUrls.ToImmutableArray();
            var jsonSerializer = JsonSerializer.CreateDefault();
            using (await addOnsAccess.LockAsync().ConfigureAwait(false))
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
                                addOn.UpdatePropertiesFromManifestEntry(entry);
                            else
                                addOns.Add(addOnsKey, new AddOn(this, addOnsKey, entry));
                        }
                    }
        });

        async void UpdateAvailableAddOnsTimerCallback(object state) => await UpdateAvailableAddOnsAsync().ConfigureAwait(false);

        public DirectoryInfo AddOnsDirectory { get; }

        public ReadOnlyObservableRangeDictionary<Guid, AddOn> AddOns { get; }

        public ReadOnlyObservableCollection<Uri> ManifestUrls { get; }

        public DirectoryInfo StorageDirectory { get; }
    }
}
