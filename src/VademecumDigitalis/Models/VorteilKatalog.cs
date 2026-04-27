namespace VademecumDigitalis.Models;

/// <summary>
/// Eintrag im Vorteil-/Nachteilkatalog (DSA 5).
/// </summary>
public record VorteilKatalogEintrag(
    string Name,
    bool IstNachteil,
    int MaxStufe,
    int ApKostenProStufe,
    string Kategorie);

/// <summary>
/// Statischer Katalog mit gebräuchlichen DSA-5-Vorteilen und -Nachteilen.
/// Quelle: Das Schwarze Auge – Regelwerk (5. Edition).
/// </summary>
public static class VorteilKatalog
{
    private static readonly List<VorteilKatalogEintrag> _alle =
    [
        // ── Vorteile ──────────────────────────────────────────────────────────────
        new("Begabung",                  false, 4,  4,  "Allgemein"),
        new("Eisern",                    false, 2, 15,  "Allgemein"),
        new("Flink",                     false, 0,  5,  "Allgemein"),
        new("Geweiht",                   false, 0,  0,  "Übernatürlich"),
        new("Glück",                     false, 3, 15,  "Allgemein"),
        new("Gut Aussehend",             false, 0, 25,  "Sozial"),
        new("Herausragender Sinn",       false, 0, 10,  "Körperlich"),
        new("Hohe Lebensenergie",        false, 6,  4,  "Körperlich"),
        new("Immunität gegen Gifte",     false, 3, 20,  "Körperlich"),
        new("Kampfreflexe",              false, 0, 15,  "Körperlich"),
        new("Körperkontrolle",           false, 0,  8,  "Körperlich"),
        new("Magiegespür",               false, 0,  5,  "Übernatürlich"),
        new("Nimmermüde",                false, 0, 10,  "Körperlich"),
        new("Reich",                     false, 0,100,  "Sozial"),
        new("Schnelle Heilung",          false, 3,  5,  "Körperlich"),
        new("Soziale Anpassungsfähigkeit", false, 0, 15, "Sozial"),
        new("Vermögend",                 false, 0, 50,  "Sozial"),
        new("Wohlhabend",                false, 0, 25,  "Sozial"),
        new("Widerstandsfähigkeit",      false, 6,  4,  "Körperlich"),
        new("Zähigkeit",                 false, 2, 15,  "Körperlich"),
        new("Zauberer",                  false, 0,  0,  "Übernatürlich"),
        new("Zeitgefühl",                false, 0,  5,  "Allgemein"),

        // ── Nachteile ──────────────────────────────────────────────────────────────
        new("Aberglaube",                true,  3, 10,  "Allgemein"),
        new("Animosität",                true,  0,  3,  "Sozial"),
        new("Arroganz",                  true,  7,  5,  "Sozial"),
        new("Eingeschränkter Sinn",      true,  0, 15,  "Körperlich"),
        new("Feige",                     true,  7,  5,  "Allgemein"),
        new("Goldgier",                  true,  7,  5,  "Allgemein"),
        new("Jähzorn",                   true,  7, 10,  "Allgemein"),
        new("Körperbau",                 true,  0,  5,  "Körperlich"),
        new("Nachtblind",                true,  0, 15,  "Körperlich"),
        new("Neugier",                   true,  7,  5,  "Allgemein"),
        new("Pechvogel",                 true,  3, 15,  "Allgemein"),
        new("Prinzipientreue",           true,  7, 10,  "Allgemein"),
        new("Schulden",                  true,  0,  0,  "Sozial"),
        new("Unfähigkeit",               true,  0,  0,  "Allgemein"),
        new("Vorurteil",                 true,  0,  3,  "Sozial"),
    ];

    /// <summary>Alle Katalogeinträge (Vorteile und Nachteile).</summary>
    public static IReadOnlyList<VorteilKatalogEintrag> Alle => _alle.AsReadOnly();

    /// <summary>Nur Vorteile aus dem Katalog.</summary>
    public static IReadOnlyList<VorteilKatalogEintrag> Vorteile =>
        _alle.Where(e => !e.IstNachteil).ToList().AsReadOnly();

    /// <summary>Nur Nachteile aus dem Katalog.</summary>
    public static IReadOnlyList<VorteilKatalogEintrag> Nachteile =>
        _alle.Where(e => e.IstNachteil).ToList().AsReadOnly();

    /// <summary>Sortierte Namen aller Vorteile (für Autocomplete).</summary>
    public static IReadOnlyList<string> VorteilNamen =>
        Vorteile.Select(e => e.Name).OrderBy(n => n).ToList().AsReadOnly();

    /// <summary>Sortierte Namen aller Nachteile (für Autocomplete).</summary>
    public static IReadOnlyList<string> NachteilNamen =>
        Nachteile.Select(e => e.Name).OrderBy(n => n).ToList().AsReadOnly();

    /// <summary>Sucht einen Katalogeintrag nach Name (case-insensitive).</summary>
    public static VorteilKatalogEintrag? FindByName(string name)
        => _alle.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
}
