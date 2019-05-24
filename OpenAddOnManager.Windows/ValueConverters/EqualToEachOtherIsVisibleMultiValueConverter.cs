using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class EqualToEachOtherIsVisibleMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values.Distinct().Count() == 1 ? Visibility.Visible : Visibility.Collapsed;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => new[] { DependencyProperty.UnsetValue };
    }
}
