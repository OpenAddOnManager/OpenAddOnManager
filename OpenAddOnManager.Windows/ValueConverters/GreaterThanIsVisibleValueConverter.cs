using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class GreaterThanIsVisibleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => System.Convert.ToDouble(value) > System.Convert.ToDouble(parameter) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
