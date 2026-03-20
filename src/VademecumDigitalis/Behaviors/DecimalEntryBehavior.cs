using System.Text.RegularExpressions;

namespace VademecumDigitalis.Behaviors;

/// <summary>
/// Behavior, das nur Dezimalzahlen (mit optionalem Punkt oder Komma) in einem Entry erlaubt.
/// Beispiel: "12.5", "0,75", "100"
/// </summary>
public partial class DecimalEntryBehavior : Behavior<Entry>
{
    // Erlaubt: optionales Minus, Ziffern, maximal ein Punkt oder Komma als Dezimaltrenner
    [GeneratedRegex(@"^-?\d*[.,]?\d*$")]
    private static partial Regex DecimalPattern();

    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.TextChanged += OnTextChanged;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(entry);
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry) return;
        if (string.IsNullOrEmpty(e.NewTextValue)) return;

        if (!DecimalPattern().IsMatch(e.NewTextValue))
        {
            // Nur erlaubte Zeichen behalten, maximal einen Dezimaltrenner
            bool hasSeparator = false;
            var cleaned = new List<char>();
            for (int i = 0; i < e.NewTextValue.Length; i++)
            {
                char c = e.NewTextValue[i];
                if (char.IsDigit(c))
                {
                    cleaned.Add(c);
                }
                else if (c == '-' && i == 0 && cleaned.Count == 0)
                {
                    cleaned.Add(c);
                }
                else if ((c == '.' || c == ',') && !hasSeparator)
                {
                    cleaned.Add(c);
                    hasSeparator = true;
                }
            }

            entry.Text = new string(cleaned.ToArray());
        }
    }
}
