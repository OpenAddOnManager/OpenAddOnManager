using Gear.Components;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.IO;
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
                name = state.Name;
                releaseChannelId = state.ReleaseChannelId;
                sourceBranch = state.SourceBranch;
                sourceUrl = state.SourceUrl;
                supportUrl = state.SupportUrl;
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
        string name;
        string releaseChannelId;
        readonly DirectoryInfo repositoryDirectory;
        string sourceBranch;
        Uri sourceUrl;
        readonly FileInfo stateFile;
        Uri supportUrl;

        public Task DeleteAsync() => Task.Run(() =>
        {
            repositoryDirectory.Refresh();
            if (repositoryDirectory.Exists)
            {
                foreach (var fileSystemInfo in repositoryDirectory.GetFileSystemInfos("*.*", SearchOption.AllDirectories))
                    fileSystemInfo.Attributes &= ~FileAttributes.ReadOnly;
                repositoryDirectory.Delete(true);
                OnPropertyChanged(nameof(IsDownloaded));
            }
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
                    Repository.Clone(sourceUrl.ToString(), repositoryDirectory.FullName);
                    return true;
                }
                else
                    return Commands.Pull(new Repository(repositoryDirectory.FullName), new Signature(SignatureName, SignatureEmail, DateTimeOffset.Now), new PullOptions { MergeOptions = new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly } }).Status == MergeStatus.FastForward;
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
                        Name = name,
                        ReleaseChannelId = releaseChannelId,
                        SourceBranch = sourceBranch,
                        SourceUrl = sourceUrl,
                        SupportUrl = supportUrl
                    });
                }
        }

        internal void UpdatePropertiesFromManifestEntry(AddOnManifestEntry addOnManifestEntry)
        {
            AddOnPageUrl = addOnManifestEntry.AddOnPageUrl;
            AuthorEmail = addOnManifestEntry.AuthorEmail;
            AuthorName = addOnManifestEntry.AuthorName;
            AuthorPageUrl = addOnManifestEntry.AuthorPageUrl;
            Description = addOnManifestEntry.Description;
            DonationsUrl = addOnManifestEntry.DonationsUrl;
            IconUrl = addOnManifestEntry.IconUrl;
            Name = addOnManifestEntry.Name;
            ReleaseChannelId = addOnManifestEntry.ReleaseChannelId;
            SourceBranch = addOnManifestEntry.SourceBranch;
            SourceUrl = addOnManifestEntry.SourceUrl;
            SupportUrl = addOnManifestEntry.SupportUrl;
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

        public Guid Key { get; }

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
