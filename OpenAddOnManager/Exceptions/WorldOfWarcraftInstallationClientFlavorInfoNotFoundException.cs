using System;

namespace OpenAddOnManager.Exceptions
{
    public class WorldOfWarcraftInstallationClientFlavorInfoNotFoundException : Exception
    {
        public override string Message => "The flavor info of this client was not found or not understood";
    }
}
