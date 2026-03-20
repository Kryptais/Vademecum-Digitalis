namespace VademecumDigitalis.Models;

/// <summary>
/// Statische DSA-5-Spezies-Daten (Grundregelwerk).
/// Enthält die Basiswerte, die bei der Spezieswahl in die Berechnung einfließen.
/// </summary>
public record SpeziesData
{
    /// <summary>Anzeigename (z. B. "Mensch")</summary>
    public required string Name { get; init; }

    /// <summary>LeP-Grundwert der Spezies (wird zu 2×KO addiert)</summary>
    public int LePBasis { get; init; }

    /// <summary>Seelenkraft-Modifikator der Spezies (wird auf (MU+KL+IN)/6 aufaddiert)</summary>
    public int SeelenkraftMod { get; init; }

    /// <summary>Zähigkeit-Modifikator der Spezies (wird auf (KO+KO+KK)/6 aufaddiert)</summary>
    public int ZähigkeitMod { get; init; }

    /// <summary>Grundgeschwindigkeit der Spezies</summary>
    public int Geschwindigkeit { get; init; }

    /// <summary>Schicksalspunkte-Grundwert</summary>
    public int SchicksalspunkteMax { get; init; }

    /// <summary>
    /// Alle im DSA-5-Grundregelwerk verfügbaren Spezies.
    /// Werte aus dem offiziellen DSA-5-Grundregelwerk (Ulisses Spiele).
    /// </summary>
    public static IReadOnlyList<SpeziesData> Alle { get; } =
    [
        new SpeziesData
        {
            Name = "Mensch",
            LePBasis = 5,
            SeelenkraftMod = -5,
            ZähigkeitMod = -5,
            Geschwindigkeit = 8,
            SchicksalspunkteMax = 3
        },
        new SpeziesData
        {
            Name = "Elf",
            LePBasis = 2,
            SeelenkraftMod = -4,
            ZähigkeitMod = -6,
            Geschwindigkeit = 8,
            SchicksalspunkteMax = 2
        },
        new SpeziesData
        {
            Name = "Halbelf",
            LePBasis = 5,
            SeelenkraftMod = -4,
            ZähigkeitMod = -6,
            Geschwindigkeit = 8,
            SchicksalspunkteMax = 3
        },
        new SpeziesData
        {
            Name = "Zwerg",
            LePBasis = 8,
            SeelenkraftMod = -4,
            ZähigkeitMod = -4,
            Geschwindigkeit = 6,
            SchicksalspunkteMax = 3
        },
        new SpeziesData
        {
            Name = "Halbork (Ork)",
            LePBasis = 8,
            SeelenkraftMod = -6,
            ZähigkeitMod = -4,
            Geschwindigkeit = 8,
            SchicksalspunkteMax = 2 // Errata / Regelbuch je nach Ausgabe 2 oder 3
        }
    ];

    /// <summary>Liefert die Spezies anhand des Namens oder null.</summary>
    public static SpeziesData? FindByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return Alle.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
