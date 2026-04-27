using System.Globalization;

namespace VademecumDigitalis.Converters;

public class StatusButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int stufe || parameter is not string paramStr)
            return Color.FromArgb("#1a1a2e");

        if (!int.TryParse(paramStr, out int buttonLevel))
            return Color.FromArgb("#1a1a2e");

        return stufe == buttonLevel 
            ? Color.FromArgb("#4DD0E1") 
            : Color.FromArgb("#1a1a2e");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
