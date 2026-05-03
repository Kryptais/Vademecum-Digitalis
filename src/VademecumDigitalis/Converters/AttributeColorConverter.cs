using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VademecumDigitalis.Converters
{
    /// <summary>
    /// Bildet einen Hauptattribut-Kürzel ("MU","KL","IN","CH","FF","GE","KO","KK")
    /// auf die jeweilige Akzentfarbe ab. Eingabe darf auch ein Klartextname sein.
    /// </summary>
    public class AttributeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = NormalizeKey(value);
            return key switch
            {
                "MU" => Color.FromArgb("#FF6B6B"),
                "KL" => Color.FromArgb("#6BB5FF"),
                "IN" => Color.FromArgb("#C19BFF"),
                "CH" => Color.FromArgb("#FF8FB1"),
                "FF" => Color.FromArgb("#FFD93D"),
                "GE" => Color.FromArgb("#6BCF7F"),
                "KO" => Color.FromArgb("#FF9F43"),
                "KK" => Color.FromArgb("#D4A574"),
                _ => Color.FromArgb("#B0E0E6")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static string NormalizeKey(object? value)
        {
            var raw = value?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty;
            return raw switch
            {
                "MUT" => "MU",
                "KLUGHEIT" => "KL",
                "INTUITION" => "IN",
                "CHARISMA" => "CH",
                "FINGERFERTIGKEIT" => "FF",
                "GEWANDTHEIT" => "GE",
                "KONSTITUTION" => "KO",
                "KÖRPERKRAFT" => "KK",
                "KOERPERKRAFT" => "KK",
                _ => raw
            };
        }
    }
}
