using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class GreaterThanIsVisibleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Version valueVersion && parameter is Version parameterVersion)
                return valueVersion > parameterVersion ? Visibility.Visible : Visibility.Collapsed;
            try
            {
                return System.Convert.ToDouble(value) > System.Convert.ToDouble(parameter) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
