using System.ComponentModel;
using System.Text.Json.Serialization;
using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.Models;

/// <summary>Bestimmt ob ein VN an ein bestimmtes Talent gebunden ist (Begabung/Unfähigkeit).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TalentGebundenerTyp
{
    Keiner,
    Begabung,
    Unfaehigkeit
}

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

    /// <summary>
    /// Legacy-Proben-Modifikatoren (alte JSON-Struktur), werden automatisch in
    /// <see cref="Effects"/> einbezogen.
    /// </summary>
    public List<ProbenModifikator> ProbenModifikatoren { get; init; } = [];

    /// <summary>
    /// Explizite Effekte im neuen Format, direkt aus JSON oder Homebrew-Erstellung.
    /// </summary>
    public List<RuleEffect> ExplicitEffects { get; init; } = [];

    /// <summary>
    /// Alle aktiven Effekte: Legacy-Modifikatoren (migriert) + explizite Effekte.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<RuleEffect> Effects =>
        ProbenModifikatoren.Select(pm => pm.ToRuleEffect())
            .Concat(ExplicitEffects)
            .ToList();

    /// <summary>Redaktionelle Hinweise / spezifische Modifikatoren.</summary>
    public string Anmerkungen { get; init; } = string.Empty;

    /// <summary>True wenn vom Benutzer als Homebrew erstellt.</summary>
    public bool IsHomebrew { get; init; }

    /// <summary>Wenn gesetzt, ist dieser VN talent-gebunden (Begabung/Unfähigkeit).</summary>
    public TalentGebundenerTyp TalentTyp { get; init; } = TalentGebundenerTyp.Keiner;

    /// <summary>True wenn der VN mehr als eine Stufe hat.</summary>
    [JsonIgnore]
    public bool IstStufenbasiert => MaxStufe > 1;

    /// <summary>Lesbare Kategoriebezeichnung.</summary>
    [JsonIgnore]
    public string KategorieAnzeige => Kategorie.ToDisplayString();

    /// <summary>Lesbare AP-Kosten-Anzeige.</summary>
    [JsonIgnore]
    public string ApKostenAnzeige => TalentTyp != TalentGebundenerTyp.Keiner
        ? "variabel"
        : ApKostenProStufe.Count == 1
            ? $"{ApKostenProStufe[0]} AP"
            : ApKostenProStufe.Count > 1
                ? $"{ApKostenProStufe[0]}-{ApKostenProStufe[^1]} AP"
                : "k.A.";

    /// <summary>Kurzbeschreibung der mechanischen Effekte.</summary>
    [JsonIgnore]
    public string EffektKurztext
    {
        get
        {
            if (TalentTyp == TalentGebundenerTyp.Begabung)
                return "Erleichterung +1 auf alle Proben";
            if (TalentTyp == TalentGebundenerTyp.Unfaehigkeit)
                return "Besten Wurf neu würfeln, schlechteren nehmen";
            if (ProbenModifikatoren.Count > 0)
                return string.Join(", ", ProbenModifikatoren.Select(pm =>
                    $"{pm.Ziel} {(pm.Wert > 0 ? "+" : "")}{pm.Wert}"));
            return string.Empty;
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
    private string _apKostenDisplay = string.Empty;

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
        set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); OnPropertyChanged(nameof(DisplayName)); } }
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
        set { if (_notiz != value) { _notiz = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); OnPropertyChanged(nameof(DisplayName)); } }
    }

    /// <summary>True wenn der VN ohne Voraussetzungsprüfung hinzugefügt wurde (Homebrew).</summary>
    public bool ForceAdded
            {
        get => _forceAdded;
        set { if (_forceAdded != value) { _forceAdded = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Tatsächliche AP-Kosten für diesen Eintrag (überschreibt Katalogwert,
    /// z. B. bei talent-gebundenen VNs wie Begabung/Unfähigkeit).
    /// </summary>
    public int ApKosten
    {
        get => _apKosten;
        set { if (_apKosten != value) { _apKosten = value; OnPropertyChanged(); } }
    }

    /// <summary>Maximale Stufe aus dem Katalog (wird beim Laden gesetzt, nicht persistiert).</summary>
    [JsonIgnore]
    public int MaxStufe { get; set; } = 1;

    /// <summary>
    /// Anzeigename inkl. Notiz, z. B. "Begabung Kraftakt" oder "Glück".
    /// </summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(_notiz)
        ? _name
        : $"{_name} {_notiz}";

    /// <summary>
    /// AP-Kosten als lesbarer Text, wird von außen (ViewModel) gesetzt.
    /// </summary>
    [JsonIgnore]
    public string ApKostenDisplay
    {
        get => _apKostenDisplay;
        set { if (_apKostenDisplay != value) { _apKostenDisplay = value; OnPropertyChanged(); } }
    }

    /// <summary>True wenn dieser Eintrag ein Nachteil ist.</summary>
    [JsonIgnore]
    public bool IstNachteil => Kategorie.IstNachteil();

    /// <summary>True wenn die nächste Stufe verfügbar wäre.</summary>
    [JsonIgnore]
    public bool KannAufsteigen => Stufe < MaxStufe;

    /// <summary>Lesbare Kategorie-Anzeige.</summary>
    [JsonIgnore]
    public string KategorieAnzeige => Kategorie.ToDisplayString();

    /// <summary>Zeigt "Vorteil" oder "Nachteil".</summary>
    [JsonIgnore]
    public string TypAnzeige => Kategorie.IstNachteil() ? "Nachteil" : "Vorteil";

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
