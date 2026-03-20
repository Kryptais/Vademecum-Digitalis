namespace VademecumDigitalis.Models;

/// <summary>
/// Aventurischer Kalender nach DSA-5 (Bosparanische Zeitrechnung / BF).
/// 12 Götter-Monate à 30 Tage + 5 Namenlose Tage = 365 Tage pro Jahr.
/// </summary>
public static class BoronKalender
{
    /// <summary>Ein aventurischer Monat.</summary>
    public record Monat(int Index, string Name, string Gottheit, int Tage);

    /// <summary>
    /// Die 13 aventurischen Monate (12 Göttermonate + Namenlose Tage).
    /// Reihenfolge und Namen nach dem DSA-5-Grundregelwerk.
    /// </summary>
    public static IReadOnlyList<Monat> Monate { get; } =
    [
        new(1,  "Praios",      "Praios",      30),
        new(2,  "Rondra",      "Rondra",      30),
        new(3,  "Efferd",      "Efferd",      30),
        new(4,  "Travia",      "Travia",      30),
        new(5,  "Boron",       "Boron",       30),
        new(6,  "Hesinde",     "Hesinde",     30),
        new(7,  "Firun",       "Firun",       30),
        new(8,  "Tsa",         "Tsa",         30),
        new(9,  "Phex",        "Phex",        30),
        new(10, "Peraine",     "Peraine",     30),
        new(11, "Ingerimm",    "Ingerimm",    30),
        new(12, "Rahja",       "Rahja",       30),
        new(13, "Namenlose Tage", "—",         5),
    ];

    /// <summary>Gesamttage pro Jahr.</summary>
    public const int TageProJahr = 365;

    /// <summary>Monatsnamen für Picker.</summary>
    public static IReadOnlyList<string> MonatsNamen { get; } =
        Monate.Select(m => m.Name).ToList();

    /// <summary>Liefert den Monat anhand seines 1-basierten Index.</summary>
    public static Monat? GetMonat(int index) =>
        Monate.FirstOrDefault(m => m.Index == index);

    /// <summary>Liefert den Monat anhand seines Namens.</summary>
    public static Monat? GetMonat(string name) =>
        Monate.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Ein aventurisches Datum im Bosparanischen Kalender.
/// Tag ist 1-basiert, Monat ist 1-basiert (1–13), Jahr ist BF.
/// </summary>
public record struct BoronDatum(int Tag, int Monat, int Jahr)
{
    /// <summary>Standard-Startdatum vieler DSA-5-Kampagnen.</summary>
    public static BoronDatum Default => new(1, 1, 1040);

    /// <summary>Prüft ob das Datum gültig ist.</summary>
    public readonly bool IsValid
    {
        get
        {
            var m = BoronKalender.GetMonat(Monat);
            return m != null && Tag >= 1 && Tag <= m.Tage && Jahr > 0;
        }
    }

    /// <summary>Formatierung: "12. Rahja 1038 BF"</summary>
    public override readonly string ToString()
    {
        var m = BoronKalender.GetMonat(Monat);
        return m != null ? $"{Tag}. {m.Name} {Jahr} BF" : $"{Tag}.{Monat}.{Jahr} BF";
    }

    /// <summary>Versucht einen String wie "12. Rahja 1038 BF" oder "12.6.1038" zu parsen.</summary>
    public static bool TryParse(string? input, out BoronDatum result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Format: "12. Rahja 1038 BF" oder "12. Rahja 1038"
        var trimmed = input.Trim().Replace(" BF", "", StringComparison.OrdinalIgnoreCase);
        var parts = trimmed.Split([' ', '.'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[0], out int tag))
            {
                // Versuche Monatsname
                var monat = BoronKalender.GetMonat(parts[1]);
                if (monat != null && int.TryParse(parts[2], out int jahr))
                {
                    result = new BoronDatum(tag, monat.Index, jahr);
                    return result.IsValid;
                }

                // Versuche numerisch: "12.6.1038"
                if (int.TryParse(parts[1], out int monatNum) && int.TryParse(parts[2], out int jahr2))
                {
                    result = new BoronDatum(tag, monatNum, jahr2);
                    return result.IsValid;
                }
            }
        }

        return false;
    }

    /// <summary>Berechnet den Tag des Jahres (1–365).</summary>
    public readonly int TagDesJahres
    {
        get
        {
            int total = 0;
            for (int i = 1; i < Monat; i++)
            {
                var m = BoronKalender.GetMonat(i);
                if (m != null) total += m.Tage;
            }
            return total + Tag;
        }
    }

    /// <summary>Berechnet die Differenz in Tagen zwischen zwei Daten.</summary>
    public readonly int DifferenzInTagen(BoronDatum other)
    {
        int thisDays = (Jahr * BoronKalender.TageProJahr) + TagDesJahres;
        int otherDays = (other.Jahr * BoronKalender.TageProJahr) + other.TagDesJahres;
        return otherDays - thisDays;
    }

    /// <summary>Addiert eine Anzahl Tage zum Datum.</summary>
    public readonly BoronDatum AddTage(int tage)
    {
        int totalDays = (Jahr - 1) * BoronKalender.TageProJahr + TagDesJahres - 1 + tage;
        if (totalDays < 0) totalDays = 0;

        int newJahr = totalDays / BoronKalender.TageProJahr + 1;
        int restTage = totalDays % BoronKalender.TageProJahr;

        int newMonat = 1;
        int newTag = 1;
        foreach (var m in BoronKalender.Monate)
        {
            if (restTage < m.Tage)
            {
                newMonat = m.Index;
                newTag = restTage + 1;
                break;
            }
            restTage -= m.Tage;
        }

        return new BoronDatum(newTag, newMonat, newJahr);
    }
}
