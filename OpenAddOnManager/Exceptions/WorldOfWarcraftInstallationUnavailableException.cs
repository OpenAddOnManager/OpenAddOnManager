using System;

namespace OpenAddOnManager.Exceptions
{
    public class WorldOfWarcraftInstallationUnavailableException : Exception
    {
        public override string Message => "There is no available World of Warcraft installation";
    }
}
