using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class NewVersionIsRefreshIconValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Version version && version > App.Version ? new PackIcon { Kind = PackIconKind.Update } : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
