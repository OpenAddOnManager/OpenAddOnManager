using System;

namespace OpenAddOnManager.Exceptions
{
    public class WorldOfWarcraftInstallationClientUnavailableException : Exception
    {
        public WorldOfWarcraftInstallationClientUnavailableException(string releaseChannelId) => ReleaseChannelId = releaseChannelId;

        public override string Message => $"There is no available World of Warcraft installation client for the \"{ReleaseChannelId}\" release channel";

        public string ReleaseChannelId { get; }
    }
}
