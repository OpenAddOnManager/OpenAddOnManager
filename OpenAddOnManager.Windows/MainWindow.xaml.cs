using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenAddOnManager.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private void ClosedHandler(object sender, EventArgs e)
        {
            var worldOfWarcraftInstallation = AddOnManager?.WorldOfWarcraftInstallation;
            AddOnManager?.Dispose();
            (worldOfWarcraftInstallation as IDisposable)?.Dispose();
            Application.Current.Shutdown();
        }

        AddOnManager AddOnManager => DataContext as AddOnManager;
    }
}
