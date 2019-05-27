using System;

namespace OpenAddOnManager.Exceptions
{
    public class WorldOfWarcraftInstallationClientUnavailableException : Exception
    {
        public WorldOfWarcraftInstallationClientUnavailableException(Flavor flavor) => Flavor = flavor;

        public override string Message => $"There is no available World of Warcraft \"{Utilities.GetFlavorName(Flavor)}\" installation client";

        public Flavor Flavor { get; }
    }
}
