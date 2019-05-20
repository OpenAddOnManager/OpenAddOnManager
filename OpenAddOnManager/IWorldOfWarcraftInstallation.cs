using System.Collections.Generic;
using System.IO;

namespace OpenAddOnManager
{
    public interface IWorldOfWarcraftInstallation
    {
        IReadOnlyDictionary<string, IWorldOfWarcraftInstallationClient> Clients { get; }
        DirectoryInfo Directory { get; }
    }
}
