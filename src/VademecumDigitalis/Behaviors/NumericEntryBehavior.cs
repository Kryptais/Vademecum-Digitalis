using System.Text.RegularExpressions;

namespace VademecumDigitalis.Behaviors;

/// <summary>
/// Behavior, das nur ganzzahlige Eingaben (optional mit Minus) in einem Entry erlaubt.
/// Alle nicht-numerischen Zeichen werden sofort entfernt.
/// </summary>
public partial class NumericEntryBehavior : Behavior<Entry>
{
    // Erlaubt: optionales Minus am Anfang, gefolgt von Ziffern
    [GeneratedRegex(@"^-?\d*$")]
    private static partial Regex NumericPattern();

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

        if (!NumericPattern().IsMatch(e.NewTextValue))
        {
            // Nur erlaubte Zeichen behalten
            var cleaned = new string(e.NewTextValue
                .Where((c, i) => char.IsDigit(c) || (c == '-' && i == 0))
                .ToArray());

            entry.Text = cleaned;
        }
    }
}
