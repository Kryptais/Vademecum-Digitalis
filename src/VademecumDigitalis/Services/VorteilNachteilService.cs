using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>
/// Hilfsmethoden für den Zugriff auf den VorteilNachteil-Katalog
/// und für die Arbeit mit Charakter-Einträgen.
/// </summary>
public static class VorteilNachteilService
{
    /// <summary>Gibt alle Vorteile aus dem Katalog zurück.</summary>
    public static IReadOnlyList<VorteilNachteil> GetVorteile() =>
        VorteilNachteilKatalog.AlleVorteile;

    /// <summary>Gibt alle Nachteile aus dem Katalog zurück.</summary>
    public static IReadOnlyList<VorteilNachteil> GetNachteile() =>
        VorteilNachteilKatalog.AlleNachteile;

    /// <summary>Sucht einen Katalog-Eintrag nach Name.</summary>
    public static VorteilNachteil? FindByName(string name) =>
        VorteilNachteilKatalog.FindByName(name);

    /// <summary>
    /// Erstellt einen neuen <see cref="CharaktervorteilEintrag"/> basierend auf
    /// einem Katalog-Eintrag. Setzt Name, Typ, MaxStufe und AP-Kosten automatisch.
    /// </summary>
    public static CharaktervorteilEintrag EintragErstellen(VorteilNachteil katalogEintrag, int stufe = 1)
    {
        int effektiveStufe = Math.Clamp(stufe, 1, katalogEintrag.MaxStufe);
        return new CharaktervorteilEintrag
        {
            Name = katalogEintrag.Name,
            Typ = katalogEintrag.Typ,
            Stufe = effektiveStufe,
            MaxStufe = katalogEintrag.MaxStufe,
            ApKosten = katalogEintrag.ApKostenProStufe * effektiveStufe
        };
    }

    /// <summary>
    /// Berechnet die Gesamt-AP-Kosten einer Liste von Charakter-Einträgen.
    /// Positive Werte = ausgegebene AP (Vorteile),
    /// negative Werte = zurückerhaltene AP (Nachteile).
    /// </summary>
    public static int BerechneApGesamt(IEnumerable<CharaktervorteilEintrag> eintraege) =>
        eintraege.Sum(e => e.ApKosten);
}
