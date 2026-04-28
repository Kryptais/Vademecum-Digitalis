namespace VademecumDigitalis.Models.RuleEngine;

/// <summary>
/// Definiert eine in der UI auswählbare Bedingung, die beim Erstellen eines Modifikators
/// an eine Regel gebunden werden kann.
/// </summary>
public class RuleConditionDefinition
{
    /// <summary>Eindeutiger Schlüssel, z. B. "context.environment.mountain".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Benutzerfreundlicher Anzeigename.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Beschreibung für Tooltips/Hilfe in der UI.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gruppe zur Filterung (Umgebung, Zustand, Berechnet, ...).</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// True, wenn die Condition manuell auf Seiten wie Hauptblatt/Talente/Kampf schaltbar ist.
    /// False bei berechneten/gesperrten Conditions aus Ausrüstung/Regeln.
    /// </summary>
    public bool IsUserToggleable { get; set; } = true;

    /// <summary>
    /// Optionaler Hinweis auf die Quelle der Condition, z. B. "Ausrüstung", "Zauberstatus".
    /// </summary>
    public string SourceHint { get; set; } = string.Empty;
}
