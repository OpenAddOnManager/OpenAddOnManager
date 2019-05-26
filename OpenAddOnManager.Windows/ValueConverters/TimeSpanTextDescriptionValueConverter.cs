using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace OpenAddOnManager.Windows.ValueConverters
{
    public class TimeSpanTextDescriptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
            {
                var parts = new List<string>();
                if (ts.Days > 1)
                    parts.Add($"{ts.Days} days");
                else if (ts.Days > 0)
                    parts.Add("1 day");
                if (ts.Hours > 1)
                    parts.Add($"{ts.Hours} hours");
                else if (ts.Hours > 0)
                    parts.Add("1 hour");
                if (ts.Minutes > 1)
                    parts.Add($"{ts.Minutes} minutes");
                else if (ts.Minutes > 0)
                    parts.Add("1 minute");
                if (ts.Seconds > 1)
                    parts.Add($"{ts.Seconds} seconds");
                else if (ts.Seconds > 0)
                    parts.Add("1 second");
                if (ts.Milliseconds > 1)
                    parts.Add($"{ts.Milliseconds} milliseconds");
                else if (ts.Milliseconds > 0)
                    parts.Add("1 millisecond");
                return string.Join(", ", parts);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
