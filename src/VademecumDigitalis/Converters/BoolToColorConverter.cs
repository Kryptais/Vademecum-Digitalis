using System.Globalization;

namespace VademecumDigitalis.Converters;

/// <summary>
/// Gibt den TrueValue zurück wenn der bool-Wert true ist, sonst den FalseValue.
/// Verwendbar für Farben, Opacity etc.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public Color? TrueColor { get; set; }
    public Color? FalseColor { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return TrueColor;
        return FalseColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
