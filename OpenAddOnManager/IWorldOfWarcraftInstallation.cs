using System.Collections.Generic;
using System.IO;

namespace OpenAddOnManager
{
    public interface IWorldOfWarcraftInstallation
    {
        IReadOnlyList<IWorldOfWarcraftInstallationClient> Clients { get; }
        DirectoryInfo Directory { get; }
    }
}
