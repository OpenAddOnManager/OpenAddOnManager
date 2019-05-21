using System;
using System.ComponentModel;
using System.IO;

namespace OpenAddOnManager
{
    public interface IWorldOfWarcraftInstallationClient : IDisposable, INotifyPropertyChanged, INotifyPropertyChanging
    {
        DirectoryInfo Directory { get; }
        IWorldOfWarcraftInstallation Installation { get; }
        string ReleaseChannelId { get; }
        string ReleaseChannelName { get; }
    }
}
