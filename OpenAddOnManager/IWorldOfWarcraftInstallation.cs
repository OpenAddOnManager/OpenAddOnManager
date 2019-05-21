using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public interface IWorldOfWarcraftInstallation
    {
        IReadOnlyDictionary<string, IWorldOfWarcraftInstallationClient> Clients { get; }
        DirectoryInfo Directory { get; }
        Task InitializationComplete { get; }
    }
}
