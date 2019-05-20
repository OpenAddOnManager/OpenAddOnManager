using Gear.Components;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public class AddOn : PropertyChangeNotifier
    {
        public static string SignatureEmail { get; } = "no-one@no-where.com";

        public static string SignatureName { get; } = "Open Add-On Manager";

        internal AddOn(AddOnManager addOnManager, Guid key, bool loadState)
        {
            Key = key;
            this.addOnManager = addOnManager;
            if (this.addOnManager.AddOnsDirectory != null)
            {
                var path = Path.Combine(this.addOnManager.AddOnsDirectory.FullName, Key.ToString("N"));
                stateFile = new FileInfo($"{path}.json");
                repositoryDirectory = new DirectoryInfo(path);
            }
            if (loadState)
            {
                AddOnState state;
                using (var streamReader = File.OpenText(stateFile.FullName))
                using (var jsonReader = new JsonTextReader(streamReader))
                    state = JsonSerializer.CreateDefault().Deserialize<AddOnState>(jsonReader);
                addOnPageUrl = state.AddOnPageUrl;
                authorEmail = state.AuthorEmail;
                authorName = state.AuthorName;
                authorPageUrl = state.AuthorPageUrl;
                description = state.Description;
                donationsUrl = state.DonationsUrl;
                iconUrl = state.IconUrl;
                isPrereleaseVersion = state.IsPrereleaseVersion;
                license = state.License;
                name = state.Name;
                releaseChannelId = state.ReleaseChannelId;
                sourceBranch = state.SourceBranch;
                sourceUrl = state.SourceUrl;
                supportUrl = state.SupportUrl;
                var worldOfWarcraftInstallation = this.addOnManager.WorldOfWarcraftInstallation;
                if (worldOfWarcraftInstallation != null && worldOfWarcraftInstallation.Clients.TryGetValue(releaseChannelId, out var client))
                {
                    var clientPath = client.Directory.FullName;
                    installedFiles = state.InstalledFiles?.Select(installedFile => new FileInfo(Path.Combine(clientPath, installedFile))).ToImmutableArray();
                    installedSha = state.InstalledSha;
                }
            }
        }

        internal AddOn(AddOnManager addOnManager, Guid key, AddOnManifestEntry addOnManifestEntry) : this(addOnManager, key, false)
        {
            addOnPageUrl = addOnManifestEntry.AddOnPageUrl;
            authorEmail = addOnManifestEntry.AuthorEmail;
            authorName = addOnManifestEntry.AuthorName;
            authorPageUrl = addOnManifestEntry.AuthorPageUrl;
            description = addOnManifestEntry.Description;
            donationsUrl = addOnManifestEntry.DonationsUrl;
            iconUrl = addOnManifestEntry.IconUrl;
            isPrereleaseVersion = addOnManifestEntry.IsPrereleaseVersion;
            name = addOnManifestEntry.Name;
            releaseChannelId = addOnManifestEntry.ReleaseChannelId;
            sourceBranch = addOnManifestEntry.SourceBranch;
            sourceUrl = addOnManifestEntry.SourceUrl;
            supportUrl = addOnManifestEntry.SupportUrl;
            SaveState();
        }

        readonly AddOnManager addOnManager;
        Uri addOnPageUrl;
        string authorEmail;
        string authorName;
        Uri authorPageUrl;
        string description;
        Uri donationsUrl;
        Uri iconUrl;
        IReadOnlyList<FileInfo> installedFiles;
        string installedSha;
        bool isLicenseAgreed;
        bool isPrereleaseVersion;
        string license;
        string name;
        string releaseChannelId;
        readonly DirectoryInfo repositoryDirectory;
        string sourceBranch;
        Uri sourceUrl;
        readonly FileInfo stateFile;
        Uri supportUrl;

        public void AgreeToLicense()
        {
        }

        public Task<bool> DeleteAsync() => Task.Run(async () =>
        {
            await UninstallAsync().ConfigureAwait(false);
            repositoryDirectory.Refresh();
            if (repositoryDirectory.Exists)
            {
                foreach (var fileSystemInfo in repositoryDirectory.GetFileSystemInfos("*.*", SearchOption.AllDirectories))
                    fileSystemInfo.Attributes &= ~FileAttributes.ReadOnly;
                repositoryDirectory.Delete(true);
                OnPropertyChanged(nameof(IsDownloaded));
                return true;
            }
            return false;
        });

        public Task<bool> DownloadAsync() => Task.Run(async () =>
        {
            repositoryDirectory.Refresh();
            if (!repositoryDirectory.Exists)
                repositoryDirectory.Create();
            try
            {
                if (repositoryDirectory.GetFileSystemInfos().Length == 0)
                {
                    if (sourceBranch == null)
                        Repository.Clone(sourceUrl.ToString(), repositoryDirectory.FullName);
                    else
                        Repository.Clone(sourceUrl.ToString(), repositoryDirectory.FullName, new CloneOptions { BranchName = sourceBranch });
                    await LoadLicenseAsync().ConfigureAwait(false);
                    return true;
                }
                else
                {
                    var pullStatus = Commands.Pull(new Repository(repositoryDirectory.FullName), new Signature(SignatureName, SignatureEmail, DateTimeOffset.Now), new PullOptions { MergeOptions = new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly } }).Status;
                    await LoadLicenseAsync().ConfigureAwait(false);
                    return pullStatus == MergeStatus.FastForward;
                }
            }
            catch (Exception ex)
            {
                await DeleteAsync().ConfigureAwait(false);
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
            finally
            {
                OnPropertyChanged(nameof(IsDownloaded));
            }
        });

        public Task<bool> InstallAsync() => Task.Run(async () =>
        {
            var worldOfWacraftInstallation = addOnManager.WorldOfWarcraftInstallation;
            if (worldOfWacraftInstallation == null || !worldOfWacraftInstallation.Clients.TryGetValue(releaseChannelId, out var client) || !IsLicenseAgreed)
                return false;
            await UninstallAsync().ConfigureAwait(false);
            // TODO: do the work
            return true;
        });

        async Task LoadLicenseAsync()
        {
            string license = null;
            if (IsDownloaded)
            {
                var licenseFile = new FileInfo(Path.Combine(repositoryDirectory.FullName, "LICENSE"));
                if (licenseFile.Exists)
                    license = await File.ReadAllTextAsync(licenseFile.FullName).ConfigureAwait(false);
            }
            License = license;
        }

        void SaveState()
        {
            if (stateFile != null)
                using (var streamWriter = File.CreateText(stateFile.FullName))
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                {
                    JsonSerializer.CreateDefault().Serialize(jsonWriter, new AddOnState
                    {
                        AddOnPageUrl = addOnPageUrl,
                        AuthorEmail = authorEmail,
                        AuthorName = authorName,
                        AuthorPageUrl = authorPageUrl,
                        Description = description,
                        DonationsUrl = donationsUrl,
                        IconUrl = iconUrl,
                        InstalledFiles = installedFiles?.Select(installedFile => installedFile.FullName.Substring(addOnManager.WorldOfWarcraftInstallation.Clients[releaseChannelId].Directory.FullName.Length + 1)).ToList(),
                        InstalledSha = installedSha,
                        IsPrereleaseVersion = isPrereleaseVersion,
                        License = license,
                        Name = name,
                        ReleaseChannelId = releaseChannelId,
                        SourceBranch = sourceBranch,
                        SourceUrl = sourceUrl,
                        SupportUrl = supportUrl
                    });
                }
        }

        public Task<bool> UninstallAsync() => Task.Run(() =>
        {
            if (!IsInstalled)
                return false;
            // TODO: do the work
            return true;
        });

        internal async Task UpdatePropertiesFromManifestEntryAsync(AddOnManifestEntry addOnManifestEntry)
        {
            AddOnPageUrl = addOnManifestEntry.AddOnPageUrl;
            AuthorEmail = addOnManifestEntry.AuthorEmail;
            AuthorName = addOnManifestEntry.AuthorName;
            AuthorPageUrl = addOnManifestEntry.AuthorPageUrl;
            Description = addOnManifestEntry.Description;
            DonationsUrl = addOnManifestEntry.DonationsUrl;
            IconUrl = addOnManifestEntry.IconUrl;
            IsPrereleaseVersion = addOnManifestEntry.IsPrereleaseVersion;
            Name = addOnManifestEntry.Name;
            SupportUrl = addOnManifestEntry.SupportUrl;

            if (releaseChannelId != addOnManifestEntry.ReleaseChannelId || sourceBranch != addOnManifestEntry.SourceBranch || sourceUrl != addOnManifestEntry.SourceUrl)
            {
                var wasInstalled = await UninstallAsync().ConfigureAwait(false);
                var wasDownloaded = await DeleteAsync().ConfigureAwait(false);
                ReleaseChannelId = addOnManifestEntry.ReleaseChannelId;
                SourceBranch = addOnManifestEntry.SourceBranch;
                SourceUrl = addOnManifestEntry.SourceUrl;
                if (wasDownloaded)
                    await DownloadAsync().ConfigureAwait(false);
                if (wasInstalled)
                    await InstallAsync().ConfigureAwait(false);
            }
        }

        public Uri AddOnPageUrl
        {
            get => addOnPageUrl;
            private set => SetBackedProperty(ref addOnPageUrl, in value);
        }

        public string AuthorEmail
        {
            get => authorEmail;
            private set => SetBackedProperty(ref authorEmail, in value);
        }

        public string AuthorName
        {
            get => authorName;
            private set => SetBackedProperty(ref authorName, in value);
        }

        public Uri AuthorPageUrl
        {
            get => authorPageUrl;
            private set => SetBackedProperty(ref authorPageUrl, in value);
        }

        public string Description
        {
            get => description;
            private set => SetBackedProperty(ref description, in value);
        }

        public Uri DonationsUrl
        {
            get => donationsUrl;
            private set => SetBackedProperty(ref donationsUrl, in value);
        }

        public Uri IconUrl
        {
            get => iconUrl;
            private set => SetBackedProperty(ref iconUrl, in value);
        }

        public bool IsDownloaded
        {
            get
            {
                repositoryDirectory.Refresh();
                return repositoryDirectory.Exists;
            }
        }

        public bool IsInstalled => installedFiles != null;

        public bool IsLicenseAgreed
        {
            get => isLicenseAgreed;
            private set => SetBackedProperty(ref isLicenseAgreed, in value);
        }

        public bool IsPrereleaseVersion
        {
            get => isPrereleaseVersion;
            private set => SetBackedProperty(ref isPrereleaseVersion, in value);
        }

        public Guid Key { get; }

        public string License
        {
            get => license;
            private set => SetBackedProperty(ref license, in value);
        }

        public string Name
        {
            get => name;
            private set => SetBackedProperty(ref name, in value);
        }

        public string ReleaseChannelId
        {
            get => releaseChannelId;
            private set => SetBackedProperty(ref releaseChannelId, in value);
        }

        public string SourceBranch
        {
            get => sourceBranch;
            private set => SetBackedProperty(ref sourceBranch, in value);
        }

        public Uri SourceUrl
        {
            get => sourceUrl;
            private set => SetBackedProperty(ref sourceUrl, in value);
        }

        public Uri SupportUrl
        {
            get => supportUrl;
            private set => SetBackedProperty(ref supportUrl, in value);
        }
    }
}
