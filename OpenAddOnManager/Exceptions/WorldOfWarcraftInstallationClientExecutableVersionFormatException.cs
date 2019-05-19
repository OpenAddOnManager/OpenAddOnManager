using System;

namespace OpenAddOnManager.Exceptions
{
    public class WorldOfWarcraftInstallationClientExecutableVersionFormatException : Exception
    {
        public override string Message => "The format of the version of this client executable is not understood";
    }
}
