namespace VademecumDigitalis.Models;

/// <summary>
/// DTO für die Persistierung aller Charakterbogen-Daten (Hauptblatt, Vorteile, Talente, Kampftalente).
/// </summary>
public class CharacterSheetData
{
    public CharacterSheet Sheet { get; set; } = new();

    /// <summary>
    /// Gespeicherte Talentwerte (FW + Anmerkung) indexiert nach Talent-Name.
    /// </summary>
    public List<TalentSaveEntry> TalentValues { get; set; } = new();

    /// <summary>Gespeicherte Kampftechnik-Werte (KTW + Boni).</summary>
    public List<KampftechnikSaveEntry> KampftechnikValues { get; set; } = new();

    /// <summary>Charakter-Ereignisse (Altersveränderungen, Stat-Boni usw.).</summary>
    public List<CharakterEreignis> Ereignisse { get; set; } = [];

    /// <summary>Sonderfertigkeiten des Charakters.</summary>
    public List<CharakterSonderfertigkeitEintrag> SonderfertigkeitListe { get; set; } = [];

    /// <summary>Vorteile und Nachteile des Charakters.</summary>
    public List<CharakterVorteilNachteilEintrag> VorteilNachteilListe { get; set; } = [];
}

/// <summary>
/// Kompakte Speicherung eines einzelnen Talentwerts.
/// </summary>
public class TalentSaveEntry
{
    public string Talent { get; set; } = string.Empty;
    public string Fw { get; set; } = string.Empty;
    public string Anmerkung { get; set; } = string.Empty;
}

/// <summary>
/// Kompakte Speicherung eines Kampftechnik-Werts (KTW + Boni).
/// </summary>
public class KampftechnikSaveEntry
{
    public string Kampftechnik { get; set; } = string.Empty;
    public string Ktw { get; set; } = string.Empty;
    public int Boni { get; set; } = 0;
}
