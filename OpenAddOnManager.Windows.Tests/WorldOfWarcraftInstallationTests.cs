using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAddOnManager.Windows.Tests
{
    [TestClass]
    public class WorldOfWarcraftInstallationTests
    {
        [TestMethod]
        public async Task RetailIsInstalledAsync()
        {
            using (var installation = await WorldOfWarcraftInstallation.CreateAsync(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "World of Warcraft")), null))
                Assert.IsTrue(installation.Clients.Any(client => client.ReleaseChannelName.Equals("Retail", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
