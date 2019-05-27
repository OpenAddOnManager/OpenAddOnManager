using Gear.Components;
using LibGit2Sharp;
using Newtonsoft.Json;
using Nito.AsyncEx;
using OpenAddOnManager.Exceptions;
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
                isLicenseAgreed = state.IsLicenseAgreed;
                isPrereleaseVersion = state.IsPrereleaseVersion;
                license = state.License;
                name = state.Name;
                flavor = state.Flavor;
                savedVariablesAddOnNames = state.SavedVariablesAddOnNames?.ToImmutableArray();
                savedVariablesPerCharacterAddOnNames = state.SavedVariablesPerCharacterAddOnNames?.ToImmutableArray();
                sourceBranch = state.SourceBranch;
                sourceUrl = state.SourceUrl;
                supportUrl = state.SupportUrl;
                var worldOfWarcraftInstallation = this.addOnManager.WorldOfWarcraftInstallation;
                if (worldOfWarcraftInstallation != null && worldOfWarcraftInstallation.ClientByFlavor.TryGetValue(flavor, out var client))
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
            flavor = addOnManifestEntry.Flavor;
            sourceBranch = addOnManifestEntry.SourceBranch;
            sourceUrl = addOnManifestEntry.SourceUrl;
            supportUrl = addOnManifestEntry.SupportUrl;
            SaveState();
        }

        AddOnActionState actionState = AddOnActionState.Idle;
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
        Flavor flavor;
        readonly DirectoryInfo repositoryDirectory;
        IReadOnlyList<string> savedVariablesAddOnNames;
        IReadOnlyList<string> savedVariablesPerCharacterAddOnNames;
        readonly AsyncLock saveStateAccess = new AsyncLock();
        string sourceBranch;
        Uri sourceUrl;
        readonly FileInfo stateFile;
        Uri supportUrl;

        public void AgreeToLicense()
        {
            if (!isLicenseAgreed)
            {
                if (!IsDownloaded)
                    throw new InvalidOperationException();
                IsLicenseAgreed = true;
            }
        }

        public Task<bool> DeleteAsync(bool uninstall = true) => Task.Run(async () =>
        {
            if (uninstall)
                await UninstallAsync().ConfigureAwait(false);
            ActionState = AddOnActionState.Deleting;
            try
            {
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
            }
            finally
            {
                ActionState = AddOnActionState.Idle;
            }
        });

        public Task<bool> DownloadAsync() => Task.Run(async () =>
        {
            ActionState = AddOnActionState.Downloading;
            try
            {
                repositoryDirectory.Refresh();
                if (!repositoryDirectory.Exists)
                    repositoryDirectory.Create();
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
                    MergeStatus pullStatus;
                    using (var repository = new Repository(repositoryDirectory.FullName))
                        pullStatus = Commands.Pull(repository, new Signature(SignatureName, SignatureEmail, DateTimeOffset.Now), new PullOptions { MergeOptions = new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly } }).Status;
                    await LoadLicenseAsync().ConfigureAwait(false);
                    if (pullStatus != MergeStatus.UpToDate)
                        OnPropertyChanged(nameof(IsUpdateAvailable));
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
                ActionState = AddOnActionState.Idle;
            }
        });

        public Task InstallAsync() => Task.Run(async () =>
        {
            if (!IsDownloaded)
                throw new AddOnNotDownloadedException();
            ActionState = AddOnActionState.Installing;
            var worldOfWacraftInstallation = addOnManager.WorldOfWarcraftInstallation;
            if (worldOfWacraftInstallation == null)
                throw new WorldOfWarcraftInstallationUnavailableException();
            await worldOfWacraftInstallation.InitializationComplete.ConfigureAwait(false);
            if (!worldOfWacraftInstallation.ClientByFlavor.TryGetValue(flavor, out var client))
                throw new WorldOfWarcraftInstallationClientUnavailableException(flavor);
            if (IsLicensed && !isLicenseAgreed)
                throw new UserHasNotAgreedToLicenseException();
            await UninstallAsync(deleteSavedVariables: false).ConfigureAwait(false);
            ActionState = AddOnActionState.Installing;
            try
            {
                var clientInterfaceDirectory = new DirectoryInfo(Path.Combine(client.Directory.FullName, "Interface"));
                if (!clientInterfaceDirectory.Exists)
                    clientInterfaceDirectory.Create();
                var clientAddOnsDirectory = new DirectoryInfo(Path.Combine(clientInterfaceDirectory.FullName, "AddOns"));
                if (!clientAddOnsDirectory.Exists)
                    clientAddOnsDirectory.Create();
                var installedFiles = new List<FileInfo>();
                var addOnsDirectory = repositoryDirectory.GetDirectories("AddOns", SearchOption.AllDirectories).SingleOrDefault();
                if (addOnsDirectory == null)
                    addOnsDirectory = repositoryDirectory.GetFiles("*.toc", SearchOption.AllDirectories)
                        .Select(tocFile => (tocFile, stepsFromRoot: Utilities.GetStepsUpFromDirectory(tocFile, repositoryDirectory)))
                        .OrderBy(ts => ts.stepsFromRoot)
                        .FirstOrDefault()
                        .tocFile
                        .Directory
                        .Parent;
                else
                    foreach (var resourceDirectory in addOnsDirectory.Parent.GetDirectories().Except(new DirectoryInfo[] { addOnsDirectory }))
                    {
                        var clientResourceDirectory = new DirectoryInfo(Path.Combine(clientInterfaceDirectory.FullName, resourceDirectory.Name));
                        if (!clientResourceDirectory.Exists)
                            clientResourceDirectory.Create();
                        installedFiles.AddRange(resourceDirectory.CopyContentsTo(clientResourceDirectory, true));
                    }
                var savedVariablesAddOnNames = new List<string>();
                var savedVariablesPerCharacterAddOnNames = new List<string>();

                async Task installAddOnAsync(DirectoryInfo addOnDirectory)
                {
                    FileInfo tocFile;
                    if (addOnDirectory == repositoryDirectory)
                        tocFile = addOnDirectory.GetFiles($"*.toc").FirstOrDefault();
                    else
                        tocFile = addOnDirectory.GetFiles($"{addOnDirectory.Name}.toc").FirstOrDefault();
                    if (tocFile != default)
                    {
                        var toc = await AddOnTableOfContents.LoadFromAsync(tocFile).ConfigureAwait(false);
                        if (toc.SavedVariables?.Any() ?? false)
                            savedVariablesAddOnNames.Add(addOnDirectory.Name);
                        if (toc.SavedVariablesPerCharacter?.Any() ?? false)
                            savedVariablesPerCharacterAddOnNames.Add(addOnDirectory.Name);
                        var clientAddOnDirectory = new DirectoryInfo(Path.Combine(clientAddOnsDirectory.FullName, tocFile.Name.Substring(0, tocFile.Name.Length - tocFile.Extension.Length)));
                        if (!clientAddOnDirectory.Exists)
                            clientAddOnDirectory.Create();
                        installedFiles.AddRange(addOnDirectory.CopyContentsTo(clientAddOnDirectory, overwrite: true));
                    }
                }

                if (addOnsDirectory == repositoryDirectory.Parent)
                    await installAddOnAsync(repositoryDirectory).ConfigureAwait(false);
                else
                    foreach (var addOnDirectory in addOnsDirectory.GetDirectories())
                        await installAddOnAsync(addOnDirectory).ConfigureAwait(false);
                using (var repository = new Repository(repositoryDirectory.FullName))
                    installedSha = repository.Head.Tip.Sha;
                this.installedFiles = installedFiles.ToImmutableArray();
                this.savedVariablesAddOnNames = savedVariablesAddOnNames.ToImmutableArray();
                this.savedVariablesPerCharacterAddOnNames = savedVariablesPerCharacterAddOnNames.ToImmutableArray();
                OnPropertyChanged(nameof(IsInstalled));
                OnPropertyChanged(nameof(IsUpdateAvailable));
                SaveState();
            }
            finally
            {
                ActionState = AddOnActionState.Idle;
            }
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
                using (saveStateAccess.Lock())
                using (var streamWriter = File.CreateText(stateFile.FullName))
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                    JsonSerializer.CreateDefault().Serialize(jsonWriter, new AddOnState
                    {
                        AddOnPageUrl = addOnPageUrl,
                        AuthorEmail = authorEmail,
                        AuthorName = authorName,
                        AuthorPageUrl = authorPageUrl,
                        Description = description,
                        DonationsUrl = donationsUrl,
                        IconUrl = iconUrl,
                        InstalledFiles = installedFiles?.Select(installedFile => installedFile.FullName.Substring(addOnManager.WorldOfWarcraftInstallation.ClientByFlavor[flavor].Directory.FullName.Length + 1)).ToList(),
                        InstalledSha = installedSha,
                        IsLicenseAgreed = isLicenseAgreed,
                        IsPrereleaseVersion = isPrereleaseVersion,
                        License = license,
                        Name = name,
                        Flavor = flavor,
                        SavedVariablesAddOnNames = savedVariablesAddOnNames?.ToList(),
                        SavedVariablesPerCharacterAddOnNames = savedVariablesPerCharacterAddOnNames?.ToList(),
                        SourceBranch = sourceBranch,
                        SourceUrl = sourceUrl,
                        SupportUrl = supportUrl
                    });
        }

        public Task<bool> UninstallAsync(bool deleteSavedVariables = true) => Task.Run(async () =>
        {
            if (!IsInstalled)
                return false;
            ActionState = AddOnActionState.Uninstalling;
            try
            {
                var worldOfWacraftInstallation = addOnManager.WorldOfWarcraftInstallation;
                if (worldOfWacraftInstallation == null)
                    throw new WorldOfWarcraftInstallationUnavailableException();
                await worldOfWacraftInstallation.InitializationComplete.ConfigureAwait(false);
                if (!worldOfWacraftInstallation.ClientByFlavor.TryGetValue(flavor, out var client))
                    throw new WorldOfWarcraftInstallationClientUnavailableException(flavor);
                var clientInterfaceDirectory = new DirectoryInfo(Path.Combine(client.Directory.FullName, "Interface"));
                if (!clientInterfaceDirectory.Exists)
                    clientInterfaceDirectory.Create();
                var clientAddOnsDirectory = new DirectoryInfo(Path.Combine(clientInterfaceDirectory.FullName, "AddOns"));
                if (!clientAddOnsDirectory.Exists)
                    clientAddOnsDirectory.Create();
                var containerDirectories = new HashSet<DirectoryInfo>();
                foreach (var installedFile in installedFiles)
                {
                    installedFile.Refresh();
                    if (installedFile.Exists)
                    {
                        containerDirectories.Add(installedFile.Directory);
                        installedFile.Attributes &= ~FileAttributes.ReadOnly;
                        installedFile.Delete();
                    }
                }
                containerDirectories.Remove(clientInterfaceDirectory);
                containerDirectories.Remove(clientAddOnsDirectory);
                foreach (var containerDirectory in containerDirectories.OrderByDescending(containerDirectory => containerDirectory.FullName.Length))
                {
                    containerDirectory.Refresh();
                    if (containerDirectory.Exists && containerDirectory.GetFileSystemInfos().Length == 0)
                        containerDirectory.Delete();
                }
                installedSha = null;
                installedFiles = null;
                if (deleteSavedVariables)
                {
                    var wtfDirectory = new DirectoryInfo(Path.Combine(client.Directory.FullName, "WTF"));
                    if (wtfDirectory.Exists)
                    {
                        var accountsDirectory = new DirectoryInfo(Path.Combine(wtfDirectory.FullName, "Account"));
                        if (accountsDirectory.Exists)
                            foreach (var accountDirectory in accountsDirectory.GetDirectories())
                            {
                                var accountSavedVariablesDirectory = new DirectoryInfo(Path.Combine(accountDirectory.FullName, "SavedVariables"));
                                if (savedVariablesAddOnNames?.Count > 0 && accountSavedVariablesDirectory.Exists)
                                    foreach (var savedVariablesAddOnName in savedVariablesAddOnNames)
                                        foreach (var savedVariablesFile in accountSavedVariablesDirectory.GetFiles($"{savedVariablesAddOnName}.*"))
                                            savedVariablesFile.Delete();
                                if (savedVariablesPerCharacterAddOnNames?.Count > 0)
                                    foreach (var realmDirectory in accountDirectory.GetDirectories().Except(new DirectoryInfo[] { accountSavedVariablesDirectory }))
                                        foreach (var characterDirectory in realmDirectory.GetDirectories())
                                        {
                                            var characterSavedVariablesDirectory = new DirectoryInfo(Path.Combine(characterDirectory.FullName, "SavedVariables"));
                                            if (characterSavedVariablesDirectory.Exists)
                                                foreach (var savedVariablesPerCharacterAddOnName in savedVariablesPerCharacterAddOnNames)
                                                    foreach (var savedVariablesFile in characterSavedVariablesDirectory.GetFiles($"{savedVariablesPerCharacterAddOnName}.*"))
                                                        savedVariablesFile.Delete();
                                        }
                            }
                    }
                }
                savedVariablesAddOnNames = null;
                savedVariablesPerCharacterAddOnNames = null;
                OnPropertyChanged(nameof(IsInstalled));
                OnPropertyChanged(nameof(IsUpdateAvailable));
                SaveState();
                return true;
            }
            finally
            {
                ActionState = AddOnActionState.Idle;
            }
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

            if (flavor != addOnManifestEntry.Flavor || sourceBranch != addOnManifestEntry.SourceBranch || sourceUrl != addOnManifestEntry.SourceUrl)
            {
                var wasInstalled = await UninstallAsync(deleteSavedVariables: false).ConfigureAwait(false);
                var wasDownloaded = await DeleteAsync().ConfigureAwait(false);
                Flavor = addOnManifestEntry.Flavor;
                SourceBranch = addOnManifestEntry.SourceBranch;
                SourceUrl = addOnManifestEntry.SourceUrl;
                if (wasDownloaded)
                    await DownloadAsync().ConfigureAwait(false);
                if (wasInstalled)
                    await InstallAsync().ConfigureAwait(false);
            }

            SaveState();
        }

        public AddOnActionState ActionState
        {
            get => actionState;
            private set => SetBackedProperty(ref actionState, in value);
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

        public bool IsInstalled => installedSha != null;

        public bool IsLicenseAgreed
        {
            get => isLicenseAgreed;
            private set => SetBackedProperty(ref isLicenseAgreed, in value);
        }

        public bool IsLicensed => !string.IsNullOrWhiteSpace(license);

        public bool IsPrereleaseVersion
        {
            get => isPrereleaseVersion;
            private set => SetBackedProperty(ref isPrereleaseVersion, in value);
        }

        public bool IsUpdateAvailable
        {
            get
            {
                if (!IsDownloaded || !IsInstalled)
                    return false;
                using (var repository = new Repository(repositoryDirectory.FullName))
                    return installedSha != repository.Head.Tip.Sha;
            }
        }

        public Guid Key { get; }

        public string License
        {
            get => license;
            private set
            {
                if (SetBackedProperty(ref license, in value))
                    OnPropertyChanged(nameof(IsLicensed));
            }
        }

        public string Name
        {
            get => name;
            private set => SetBackedProperty(ref name, in value);
        }

        public Flavor Flavor
        {
            get => flavor;
            private set => SetBackedProperty(ref flavor, in value);
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
