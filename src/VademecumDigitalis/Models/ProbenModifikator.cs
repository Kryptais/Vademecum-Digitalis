using System.Text.Json.Serialization;

namespace VademecumDigitalis.Models;

/// <summary>Art des Proben-Modifikators: auf welcher Ebene wirkt er.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModifikatorTyp
{
    /// <summary>Wirkt auf alle Proben, die ein bestimmtes Attribut enthalten (z.B. alle KK-Proben +1).</summary>
    Attribut,

    /// <summary>Wirkt auf alle Proben eines bestimmten Talents (z.B. Kraftakt +1 auf alle drei Proben).</summary>
    Talent,

    /// <summary>Wirkt auf alle Talente einer Gruppe (z.B. alle Körpertalente −1).</summary>
    TalentGruppe
}

/// <summary>
/// Ein einzelner Proben-Modifikator, der in Vorteilen, Nachteilen oder Sonderfertigkeiten definiert wird.
/// Positive Werte erleichtern die Probe (effektiver Attributwert steigt), negative erschweren sie.
/// </summary>
public record ProbenModifikator
{
    /// <summary>Art des Modifikators.</summary>
    public ModifikatorTyp Typ { get; init; }

    /// <summary>
    /// Ziel des Modifikators, abhängig vom Typ:
    /// - Attribut: Kürzel (MU, KL, IN, CH, FF, GE, KO, KK)
    /// - Talent: exakter Talentname (z.B. "Kraftakt")
    /// - TalentGruppe: Gruppenname (z.B. "Körpertalente")
    /// </summary>
    public string Ziel { get; init; } = string.Empty;

    /// <summary>
    /// Modifikatorwert pro Stufe. Positiv = Erleichterung, negativ = Erschwernis.
    /// Bei stufenbasierten VN/SF wird dieser Wert mit der Stufe multipliziert.
    /// </summary>
    public int Wert { get; init; }
}
