using System;

namespace OpenAddOnManager.Exceptions
{
    public class WorldOfWarcraftInstallationClientExecutableNotFoundException : Exception
    {
        public override string Message => "There were no executable files found for this installation client matching known names";
    }
}
