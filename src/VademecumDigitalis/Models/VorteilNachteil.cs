using System.ComponentModel;
using System.Text.Json.Serialization;
using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.Models;

/// <summary>Kategorien für Vorteile und Nachteile nach DSA 5 Regelwiki.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VorteilNachteilKategorie
{
    AllgemeineVorteile,
    Kampfvorteile,
    MagischeVorteile,
    KarmaleVorteile,
    AllgemeineNachteile,
    Kampfnachteile,
    MagischeNachteile,
    KarmaleNachteile,
    SchlechteEigenschaften
}

/// <summary>Lesbare Anzeige-Texte für VN-Kategorien.</summary>
public static class VorteilNachteilKategorieExtensions
{
    public static string ToDisplayString(this VorteilNachteilKategorie kategorie) => kategorie switch
    {
        VorteilNachteilKategorie.AllgemeineVorteile => "Allgemeine Vorteile",
        VorteilNachteilKategorie.Kampfvorteile => "Kampfvorteile",
        VorteilNachteilKategorie.MagischeVorteile => "Magische Vorteile",
        VorteilNachteilKategorie.KarmaleVorteile => "Karmale Vorteile",
        VorteilNachteilKategorie.AllgemeineNachteile => "Allgemeine Nachteile",
        VorteilNachteilKategorie.Kampfnachteile => "Kampfnachteile",
        VorteilNachteilKategorie.MagischeNachteile => "Magische Nachteile",
        VorteilNachteilKategorie.KarmaleNachteile => "Karmale Nachteile",
        VorteilNachteilKategorie.SchlechteEigenschaften => "Schlechte Eigenschaften",
        _ => kategorie.ToString()
    };

    /// <summary>Sortierreihenfolge für die gruppierte Darstellung.</summary>
    public static int SortOrder(this VorteilNachteilKategorie kategorie) => (int)kategorie;

    /// <summary>True wenn die Kategorie ein Nachteil ist.</summary>
    public static bool IstNachteil(this VorteilNachteilKategorie kategorie) => kategorie switch
    {
        VorteilNachteilKategorie.AllgemeineNachteile => true,
        VorteilNachteilKategorie.Kampfnachteile => true,
        VorteilNachteilKategorie.MagischeNachteile => true,
        VorteilNachteilKategorie.KarmaleNachteile => true,
        VorteilNachteilKategorie.SchlechteEigenschaften => true,
        _ => false
    };
}

    /// <summary>
/// Ein Vorteil-/Nachteil-Katalogeintrag aus vorteile_nachteile.json.
/// Definiert Regeldaten, Stufensystem und AP-Kosten.
    /// </summary>
public record VorteilNachteil
{
    /// <summary>Eindeutige ID (slug), z.B. "glueck".</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string Beschreibung { get; init; } = string.Empty;
    public VorteilNachteilKategorie Kategorie { get; init; }

    /// <summary>Maximale Stufe (1 = nicht stufenbasiert).</summary>
    public int MaxStufe { get; init; } = 1;

    /// <summary>AP-Kosten pro Stufe (Index 0 = Stufe I, Index 1 = Stufe II, …).</summary>
    public List<int> ApKostenProStufe { get; init; } = [];

    /// <summary>Voraussetzungen pro Stufe. Index 0 = Stufe I etc.</summary>
    public List<List<Requirement>> VoraussetzungenProStufe { get; init; } = [];

    /// <summary>Proben-Modifikatoren, die dieser Vorteil/Nachteil gewährt (pro Stufe multipliziert).</summary>
    public List<ProbenModifikator> ProbenModifikatoren { get; init; } = [];

    /// <summary>
    /// Neue generische Effekte. Narrative Effekte beschreiben nur, Modifier-Effekte
    /// können über die RuleEngine konkrete Werte verändern.
    /// </summary>
    public List<RuleEffect> Effects { get; init; } = [];

    /// <summary>Redaktionelle Hinweise / spezifische Modifikatoren.</summary>
    public string Anmerkungen { get; init; } = string.Empty;

    /// <summary>True wenn vom Benutzer als Homebrew erstellt.</summary>
    public bool IsHomebrew { get; init; }

    /// <summary>True wenn der VN mehr als eine Stufe hat.</summary>
    [JsonIgnore]
    public bool IstStufenbasiert => MaxStufe > 1;

    [JsonIgnore]
    public string KategorieAnzeige => Kategorie.ToDisplayString();

    [JsonIgnore]
    public string ApKostenAnzeige
    {
        get
        {
            if (ApKostenProStufe.Count == 0)
                return "AP n/a";

            return MaxStufe > 1
                ? $"{string.Join("/", ApKostenProStufe)} AP"
                : $"{ApKostenProStufe[0]} AP";
        }
    }

    [JsonIgnore]
    public string EffektKurztext
    {
        get
        {
            var mechanical = Effects.Count(e => e.IsMechanical);
            if (mechanical > 0)
                return $"{mechanical} mechanische Effekte";

            return ProbenModifikatoren.Count > 0
                ? $"{ProbenModifikatoren.Count} Probenmodifikatoren"
                : "Narrativ / manuell";
        }
    }
}

/// <summary>
/// Ein erworbener Vorteil/Nachteil-Eintrag im Charakterbogen.
/// Referenziert einen Katalogeintrag über die ID und verfolgt die aktuelle Stufe.
/// </summary>
public class CharakterVorteilNachteilEintrag : INotifyPropertyChanged
{
    private string _vnId = string.Empty;
    private string _name = string.Empty;
    private VorteilNachteilKategorie _kategorie;
    private int _stufe = 1;
    private int _apKosten = 0;
    private string _notiz = string.Empty;
    private bool _forceAdded;

    /// <summary>Referenz auf VorteilNachteil.Id im Katalog.</summary>
    public string VnId
    {
        get => _vnId;
        set { if (_vnId != value) { _vnId = value; OnPropertyChanged(); } }
    }

    /// <summary>Name des Vorteils/Nachteils (denormalisiert für Anzeige).</summary>
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); } }
    }

    /// <summary>Kategorie (denormalisiert für Gruppierung).</summary>
    public VorteilNachteilKategorie Kategorie
            {
        get => _kategorie;
        set { if (_kategorie != value) { _kategorie = value; OnPropertyChanged(); OnPropertyChanged(nameof(KategorieAnzeige)); } }
    }

    /// <summary>Aktuelle Stufe (1-basiert).</summary>
    public int Stufe
    {
        get => _stufe;
        set { if (_stufe != value) { _stufe = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); OnPropertyChanged(nameof(StufeAnzeige)); } }
    }

    /// <summary>Optionale Notiz / Anmerkung des Spielers.</summary>
    public string Notiz
            {
        get => _notiz;
        set { if (_notiz != value) { _notiz = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); } }
    }

    /// <summary>True wenn der VN ohne Voraussetzungsprüfung hinzugefügt wurde (Homebrew).</summary>
    public bool ForceAdded
            {
        get => _forceAdded;
        set { if (_forceAdded != value) { _forceAdded = value; OnPropertyChanged(); } }
    }

    /// <summary>Maximale Stufe aus dem Katalog (wird beim Laden gesetzt, nicht persistiert).</summary>
    [JsonIgnore]
    public int MaxStufe { get; set; } = 1;

    /// <summary>True wenn die nächste Stufe verfügbar wäre.</summary>
    [JsonIgnore]
    public bool KannAufsteigen => Stufe < MaxStufe;

    /// <summary>Lesbare Kategorie-Anzeige.</summary>
    [JsonIgnore]
    public string KategorieAnzeige => Kategorie.ToDisplayString();

    /// <summary>Stufenanzeige, z.B. "II" (leer bei einstufigen VNs).</summary>
    [JsonIgnore]
    public string StufeAnzeige => MaxStufe > 1 ? ToRoman(Stufe) : string.Empty;

    /// <summary>Zusammenfassung für Listendarstellung.</summary>
    [JsonIgnore]
    public string Anzeige
        {
        get
            {
            var text = Name;
            if (MaxStufe > 1)
                text += $" {ToRoman(Stufe)}";
            if (!string.IsNullOrWhiteSpace(Notiz))
                text += $" ({Notiz})";
            return text;
            }
        }

    [JsonIgnore]
    public bool IstNachteil => Kategorie.IstNachteil();

    [JsonIgnore]
    public string TypAnzeige => IstNachteil ? "Nachteil" : "Vorteil";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string ToRoman(int value) => value switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        _ => value.ToString()
    };
}

/// <summary>Gruppierung von Vorteilen/Nachteilen für gruppierte CollectionView.</summary>
public class VnGruppe : List<CharakterVorteilNachteilEintrag>
{
    public string Kategorie { get; }

    public VnGruppe(string kategorie, IEnumerable<CharakterVorteilNachteilEintrag> eintraege)
        : base(eintraege)
    {
        Kategorie = kategorie;
    }
}
