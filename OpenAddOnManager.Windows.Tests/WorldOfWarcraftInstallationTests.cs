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

        [TestMethod]
        public async Task AlamoReeburthFullLifecycleInRetail()
        {
            var alamoReeburthKey = Guid.Parse("945fad13-7ec7-4149-9541-3852bfad0673");
            var testStorageDirectory = GetTestStorageDirectory();
            using (var installation = await WorldOfWarcraftInstallation.GetAsync())
            using (var manager = await AddOnManager.StartAsync(testStorageDirectory, installation))
            {
                var alamoReeburth = manager.AddOns[alamoReeburthKey];
                Assert.IsFalse(alamoReeburth.IsDownloaded);
                Assert.IsFalse(alamoReeburth.IsLicensed);
                await alamoReeburth.DownloadAsync();
                Assert.IsTrue(alamoReeburth.IsDownloaded);
                Assert.IsTrue(alamoReeburth.IsLicensed);
                Assert.IsFalse(alamoReeburth.IsLicenseAgreed);
                alamoReeburth.AgreeToLicense();
                Assert.IsTrue(alamoReeburth.IsLicenseAgreed);
                Assert.IsFalse(alamoReeburth.IsInstalled);
                await alamoReeburth.InstallAsync();
                Assert.IsTrue(alamoReeburth.IsInstalled);
                await alamoReeburth.UninstallAsync();
                Assert.IsFalse(alamoReeburth.IsInstalled);
                await alamoReeburth.DeleteAsync();
                Assert.IsFalse(alamoReeburth.IsDownloaded);
            }
            CleanTestStorageDirectory(testStorageDirectory);
        }

        [TestMethod, ExpectedException(typeof(UserHasNotAgreedToLicenseException))]
        public async Task AlamoReeburthLicenseFailureInRetail()
        {
            var alamoReeburthKey = Guid.Parse("945fad13-7ec7-4149-9541-3852bfad0673");
            var testStorageDirectory = GetTestStorageDirectory();
            try
            {
                using (var installation = await WorldOfWarcraftInstallation.GetAsync())
                using (var manager = await AddOnManager.StartAsync(testStorageDirectory, installation))
                {
                    var alamoReeburth = manager.AddOns[alamoReeburthKey];
                    Assert.IsFalse(alamoReeburth.IsDownloaded);
                    Assert.IsFalse(alamoReeburth.IsLicensed);
                    await alamoReeburth.DownloadAsync();
                    Assert.IsTrue(alamoReeburth.IsDownloaded);
                    Assert.IsTrue(alamoReeburth.IsLicensed);
                    Assert.IsFalse(alamoReeburth.IsLicenseAgreed);
                    Assert.IsFalse(alamoReeburth.IsInstalled);
                    await alamoReeburth.InstallAsync();
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
            using (var installation = await WorldOfWarcraftInstallation.GetAsync())
                Assert.IsTrue(installation.Clients.ContainsKey("_retail_"));
        }
    }
}
