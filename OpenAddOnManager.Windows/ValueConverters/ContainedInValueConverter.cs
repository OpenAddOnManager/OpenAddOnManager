using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class ContainedInValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Array.IndexOf((Array)parameter, value) >= 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
