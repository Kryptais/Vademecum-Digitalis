using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VademecumDigitalis.Converters
{
    public class IntStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string) ?? string.Empty;
            if (int.TryParse(s, out var v)) return v;
            return 0;
        }
    }
}
