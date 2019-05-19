using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAddOnManager.Tests
{
    [TestClass]
    public class AddOnManagerTests
    {
        [TestMethod]
        public async Task AddOnManifestLoads()
        {
            using (var manager = await AddOnManager.StartAsync(null, null))
                Assert.IsTrue(manager.AddOns.Count > 0);
        }

        [TestMethod]
        public async Task AddOnStatesPersist()
        {
            var alamoReeburthKey = Guid.Parse("945fad13-7ec7-4149-9541-3852bfad0673");
            var testStorageDirectory = GetTestStorageDirectory();
            using (var manager = await AddOnManager.StartAsync(testStorageDirectory, null))
                Assert.IsTrue(await manager.AddOns[alamoReeburthKey].DownloadAsync());
            using (var manager = await AddOnManager.StartAsync(testStorageDirectory, null))
            {
                var alamoReeburth = manager.AddOns[alamoReeburthKey];
                Assert.IsTrue(alamoReeburth.IsDownloaded);
                await alamoReeburth.DeleteAsync();
            }
            CleanTestStorageDirectory(testStorageDirectory);
        }

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
    }
}
