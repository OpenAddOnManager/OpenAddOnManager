using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAddOnManager.Tests
{
    [TestClass]
    public class AddOnManagerTests
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
        public async Task AddOnManifestLoadsAsync()
        {
            using (var manager = new AddOnManager(null, null))
            {
                await manager.InitializationComplete;
                Assert.IsTrue(manager.AddOns.Count > 0);
            }
        }

        [TestMethod]
        public async Task AddOnStatesPersistAsync()
        {
            var blankAddOnKey = Guid.Parse("945fad13-7ec7-4149-9541-3852bfad0673");
            var testStorageDirectory = GetTestStorageDirectory();
            using (var manager = new AddOnManager(testStorageDirectory, null))
            {
                await manager.InitializationComplete;
                Assert.IsTrue(await manager.AddOns[blankAddOnKey].DownloadAsync());
            }
            using (var manager = new AddOnManager(testStorageDirectory, null))
            {
                await manager.InitializationComplete;
                var blankAddOn = manager.AddOns[blankAddOnKey];
                Assert.IsTrue(blankAddOn.IsDownloaded);
                await blankAddOn.DeleteAsync();
            }
            CleanTestStorageDirectory(testStorageDirectory);
        }
    }
}
