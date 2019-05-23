using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenAddOnManager.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAddOnManager.Windows.Tests
{
    [TestClass]
    public class WorldOfWarcraftInstallationTests
    {
        #region Helper Methods

        void CleanTestStorageDirectory(DirectoryInfo testStorageDirectory)
        {
            testStorageDirectory.Refresh();
            if (testStorageDirectory.Exists)
            {
                foreach (var fileSystemInfo in testStorageDirectory.GetFileSystemInfos("*.*", SearchOption.AllDirectories))
                    fileSystemInfo.Attributes &= ~FileAttributes.ReadOnly;
                testStorageDirectory.Delete(true);
            }
        }

        DirectoryInfo GetTestStorageDirectory()
        {
            var testStorageDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"Test Storage {Guid.NewGuid():N}"));
            testStorageDirectory.Create();
            return testStorageDirectory;
        }

        #endregion Helper Methods

        static readonly Guid blankAddOnKey = Guid.Parse("945fad13-7ec7-4149-9541-3852bfad0673");

        [TestMethod]
        public async Task FullLifecycleInRetail()
        {
            var testStorageDirectory = GetTestStorageDirectory();
            using (var installation = new WorldOfWarcraftInstallation())
            using (var manager = new AddOnManager(testStorageDirectory, installation))
            {
                await manager.InitializationComplete;
                var blankAddOn = manager.AddOns[blankAddOnKey];
                Assert.IsFalse(blankAddOn.IsDownloaded);
                Assert.IsFalse(blankAddOn.IsLicensed);
                await blankAddOn.DownloadAsync();
                Assert.IsTrue(blankAddOn.IsDownloaded);
                Assert.IsTrue(blankAddOn.IsLicensed);
                Assert.IsFalse(blankAddOn.IsLicenseAgreed);
                blankAddOn.AgreeToLicense();
                Assert.IsTrue(blankAddOn.IsLicenseAgreed);
                Assert.IsFalse(blankAddOn.IsInstalled);
                await blankAddOn.InstallAsync();
                Assert.IsTrue(blankAddOn.IsInstalled);
                await blankAddOn.UninstallAsync();
                Assert.IsFalse(blankAddOn.IsInstalled);
                await blankAddOn.DeleteAsync();
                Assert.IsFalse(blankAddOn.IsDownloaded);
            }
            CleanTestStorageDirectory(testStorageDirectory);
        }

        [TestMethod, ExpectedException(typeof(UserHasNotAgreedToLicenseException))]
        public async Task LicenseFailureInRetail()
        {
            var testStorageDirectory = GetTestStorageDirectory();
            try
            {
                using (var installation = new WorldOfWarcraftInstallation())
                using (var manager = new AddOnManager(testStorageDirectory, installation))
                {
                    await manager.InitializationComplete;
                    var blankAddOn = manager.AddOns[blankAddOnKey];
                    Assert.IsFalse(blankAddOn.IsDownloaded);
                    Assert.IsFalse(blankAddOn.IsLicensed);
                    await blankAddOn.DownloadAsync();
                    Assert.IsTrue(blankAddOn.IsDownloaded);
                    Assert.IsTrue(blankAddOn.IsLicensed);
                    Assert.IsFalse(blankAddOn.IsLicenseAgreed);
                    Assert.IsFalse(blankAddOn.IsInstalled);
                    await blankAddOn.InstallAsync();
                }
            }
            finally
            {
                CleanTestStorageDirectory(testStorageDirectory);
            }
        }

        [TestMethod]
        public async Task RetailIsInstalledAsync()
        {
            using (var installation = new WorldOfWarcraftInstallation())
            {
                await installation.InitializationComplete;
                Assert.IsTrue(installation.ClientByReleaseChannelId.ContainsKey("_retail_"));
            }
        }
    }
}
