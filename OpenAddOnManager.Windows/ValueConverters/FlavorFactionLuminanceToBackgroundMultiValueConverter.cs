using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class FlavorFactionLuminanceToBackgroundMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values[0] is Flavor flavor && values[1] is bool themeIsHorde && values[2] is bool themeIsDark)
            {
                var graphicLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Graphics\\{flavor}_{(themeIsHorde ? "horde" : "alliance")}_{(themeIsDark ? "dark" : "light")}.jpg");
                if (File.Exists(graphicLocalPath))
                    return new BitmapImage(new Uri(graphicLocalPath));
                graphicLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Graphics\\{flavor}_{(themeIsDark ? "dark" : "light")}.jpg");
                if (File.Exists(graphicLocalPath))
                    return new BitmapImage(new Uri(graphicLocalPath));
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => new[] { DependencyProperty.UnsetValue };
    }
}
