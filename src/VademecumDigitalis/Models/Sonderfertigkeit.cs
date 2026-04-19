using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VademecumDigitalis.Models;

/// <summary>Unterkategorien für profane Sonderfertigkeiten nach DSA 5 Regelwiki.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpecialAbilityCategory
{
    AllgemeineSf,
    BefehlsSf,
    ErweiterteKampfstilSf,
    ErweiterteTalentSf,
    KampfSf,
    KampfstilSf,
    PruegelSf,
    SchicksalspunkteSf,
    SprachenSchriften,
    TalentstilSf
}

/// <summary>Lesbare Anzeige-Texte für Kategorien.</summary>
public static class SpecialAbilityCategoryExtensions
{
    public static string ToDisplayString(this SpecialAbilityCategory category) => category switch
    {
        SpecialAbilityCategory.AllgemeineSf => "Allgemeine SF",
        SpecialAbilityCategory.BefehlsSf => "Befehls-SF",
        SpecialAbilityCategory.ErweiterteKampfstilSf => "Erweiterte Kampfstil-SF",
        SpecialAbilityCategory.ErweiterteTalentSf => "Erweiterte Talent-SF",
        SpecialAbilityCategory.KampfSf => "Kampf-SF",
        SpecialAbilityCategory.KampfstilSf => "Kampfstil-SF",
        SpecialAbilityCategory.PruegelSf => "Prügel-SF",
        SpecialAbilityCategory.SchicksalspunkteSf => "Schicksalspunkte-SF",
        SpecialAbilityCategory.SprachenSchriften => "Sprachen & Schriften",
        SpecialAbilityCategory.TalentstilSf => "Talentstil-SF",
        _ => category.ToString()
    };

    /// <summary>Sortierreihenfolge für die gruppierte Darstellung.</summary>
    public static int SortOrder(this SpecialAbilityCategory category) => (int)category;
}

/// <summary>Typ einer einzelnen Voraussetzung.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequirementType
{
    Attribut,
    Talent,
    Kampftechnik,
    Sonderfertigkeit,
    Vorteil,
    Sonstiges
}

/// <summary>Eine Voraussetzung für eine Sonderfertigkeit (z.B. "KK 13" oder "Schwerter 10").</summary>
public record Requirement
{
    public RequirementType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MinValue { get; init; }

    /// <summary>Lesbare Darstellung, z.B. "KK 13".</summary>
    public override string ToString() => MinValue > 0 ? $"{Name} {MinValue}" : Name;
}

/// <summary>
/// Ein Sonderfertigkeit-Katalogeintrag aus profane_sf.json.
/// Definiert Regeldaten, Stufensystem und Voraussetzungen.
/// </summary>
public record SpecialAbility
{
    /// <summary>Eindeutige ID (slug), z.B. "wuchtschlag".</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string Beschreibung { get; init; } = string.Empty;
    public SpecialAbilityCategory Kategorie { get; init; }

    /// <summary>Maximale Stufe (1 = nicht stufenbasiert).</summary>
    public int MaxStufe { get; init; } = 1;

    /// <summary>AP-Kosten pro Stufe (Index 0 = Stufe I, Index 1 = Stufe II, …).</summary>
    public List<int> ApKostenProStufe { get; init; } = [];

    /// <summary>Voraussetzungen pro Stufe. Index 0 = Stufe I etc. Leere Liste = keine Voraussetzungen.</summary>
    public List<List<Requirement>> VoraussetzungenProStufe { get; init; } = [];

    /// <summary>Für Kampf-SFs: Mit welchen Kampftechniken ist die SF nutzbar.</summary>
    public List<string> ZielKampftechniken { get; init; } = [];

    /// <summary>Proben-Modifikatoren, die diese Sonderfertigkeit gewährt (pro Stufe multipliziert).</summary>
    public List<ProbenModifikator> ProbenModifikatoren { get; init; } = [];

    /// <summary>Redaktionelle Hinweise / spezifische Modifikatoren.</summary>
    public string Anmerkungen { get; init; } = string.Empty;

    /// <summary>True wenn vom Benutzer als Homebrew erstellt.</summary>
    public bool IsHomebrew { get; init; }

    /// <summary>True wenn die SF mehr als eine Stufe hat.</summary>
    [JsonIgnore]
    public bool IstStufenbasiert => MaxStufe > 1;
}

/// <summary>
/// Ein erworbener Sonderfertigkeit-Eintrag im Charakterbogen.
/// Referenziert einen Katalogeintrag über die ID und verfolgt die aktuelle Stufe.
/// </summary>
public class CharakterSonderfertigkeitEintrag : INotifyPropertyChanged
{
    private string _sfId = string.Empty;
    private string _name = string.Empty;
    private SpecialAbilityCategory _kategorie;
    private int _stufe = 1;
    private string _notiz = string.Empty;
    private bool _forceAdded;

    /// <summary>Referenz auf SpecialAbility.Id im Katalog.</summary>
    public string SfId
    {
        get => _sfId;
        set { if (_sfId != value) { _sfId = value; OnPropertyChanged(); } }
    }

    /// <summary>Name der Sonderfertigkeit (denormalisiert für Anzeige).</summary>
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); } }
    }

    /// <summary>Kategorie (denormalisiert für Gruppierung).</summary>
    public SpecialAbilityCategory Kategorie
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

    /// <summary>True wenn die SF ohne Voraussetzungsprüfung hinzugefügt wurde (Homebrew).</summary>
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

    /// <summary>Stufenanzeige, z.B. "II" (leer bei einstufigen SFs).</summary>
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
        _ => value.ToString()
    };
}

/// <summary>Gruppierung von Sonderfertigkeiten für gruppierte CollectionView.</summary>
public class SfGruppe : List<CharakterSonderfertigkeitEintrag>
{
    public string Kategorie { get; }

    public SfGruppe(string kategorie, IEnumerable<CharakterSonderfertigkeitEintrag> eintraege)
        : base(eintraege)
    {
        Kategorie = kategorie;
    }
}
