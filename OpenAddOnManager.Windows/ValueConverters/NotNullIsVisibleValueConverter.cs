using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class NotNullIsVisibleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
