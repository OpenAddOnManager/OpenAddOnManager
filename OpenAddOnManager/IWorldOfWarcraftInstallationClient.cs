using System;
using System.ComponentModel;
using System.IO;

namespace OpenAddOnManager
{
    public interface IWorldOfWarcraftInstallationClient : IDisposable, INotifyPropertyChanged, INotifyPropertyChanging
    {
        DirectoryInfo Directory { get; }
        FileInfo Executible { get; }
        IWorldOfWarcraftInstallation Installation { get; }
        Flavor Flavor { get; }
        string FlavorName { get; }
    }
}
