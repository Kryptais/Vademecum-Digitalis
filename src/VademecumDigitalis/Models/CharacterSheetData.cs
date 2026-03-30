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

    /// <summary>Charakter-Ereignisse (Altersveränderungen, Stat-Boni usw.).</summary>
    public List<CharakterEreignis> Ereignisse { get; set; } = [];
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
