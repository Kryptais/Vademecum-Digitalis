using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// Mutables Formular-Model zum Erstellen/Bearbeiten eines Vorteils oder Nachteils.
/// Implementiert INotifyPropertyChanged für XAML-Binding.
/// </summary>
public class VorteilNachteilEditModel : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _beschreibung = string.Empty;
    private VorteilNachteilKategorie _kategorie = VorteilNachteilKategorie.AllgemeineVorteile;
    private int _maxStufe = 1;
    private string _apKostenText = string.Empty;
    private string _anmerkungen = string.Empty;
    private TalentGebundenerTyp _talentTyp = TalentGebundenerTyp.Keiner;
    private bool _isHomebrew = true;

    public string Id
    {
        get => _id;
        set { if (_id != value) { _id = value; Notify(); } }
    }

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; Notify(); } }
    }

    public string Beschreibung
    {
        get => _beschreibung;
        set { if (_beschreibung != value) { _beschreibung = value; Notify(); } }
    }

    public VorteilNachteilKategorie Kategorie
    {
        get => _kategorie;
        set { if (_kategorie != value) { _kategorie = value; Notify(); } }
    }

    public int MaxStufe
    {
        get => _maxStufe;
        set { if (_maxStufe != value) { _maxStufe = Math.Max(1, value); Notify(); } }
    }

    /// <summary>AP-Kosten als Komma-getrennter Text, z.B. "20,20,20".</summary>
    public string ApKostenText
    {
        get => _apKostenText;
        set { if (_apKostenText != value) { _apKostenText = value; Notify(); } }
    }

    public string Anmerkungen
    {
        get => _anmerkungen;
        set { if (_anmerkungen != value) { _anmerkungen = value; Notify(); } }
    }

    public TalentGebundenerTyp TalentTyp
    {
        get => _talentTyp;
        set { if (_talentTyp != value) { _talentTyp = value; Notify(); } }
    }

    public bool IsHomebrew
    {
        get => _isHomebrew;
        set { if (_isHomebrew != value) { _isHomebrew = value; Notify(); } }
    }

    /// <summary>Editierbare Effektliste.</summary>
    public ObservableCollection<RuleEffectEditModel> Effects { get; } = [];

    // --- Statische Listen für Picker-Binding ---

    public static VorteilNachteilKategorie[] KategorieOptionen { get; } =
        Enum.GetValues<VorteilNachteilKategorie>();

    public static TalentGebundenerTyp[] TalentTypOptionen { get; } =
        Enum.GetValues<TalentGebundenerTyp>();

    // --- Konvertierung ---

    /// <summary>Erstellt ein EditModel aus einem Katalogeintrag.</summary>
    public static VorteilNachteilEditModel FromCatalog(VorteilNachteil vn)
    {
        var model = new VorteilNachteilEditModel
        {
            Id = vn.Id,
            Name = vn.Name,
            Beschreibung = vn.Beschreibung,
            Kategorie = vn.Kategorie,
            MaxStufe = vn.MaxStufe,
            ApKostenText = string.Join(", ", vn.ApKostenProStufe),
            Anmerkungen = vn.Anmerkungen,
            TalentTyp = vn.TalentTyp,
            IsHomebrew = vn.IsHomebrew
        };

        foreach (var effect in vn.ExplicitEffects)
            model.Effects.Add(RuleEffectEditModel.FromEffect(effect));

        return model;
    }

    /// <summary>Konvertiert das EditModel zurück in einen immutablen VorteilNachteil-Record.</summary>
    public VorteilNachteil ToCatalogEntry()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[VorteilNachteilEditModel.ToCatalogEntry] Converting {Effects.Count} effects...");

            var effects = new List<RuleEffect>();
            foreach (var effect in Effects)
            {
                try
                {
                    var converted = effect.ToRuleEffect();
                    System.Diagnostics.Debug.WriteLine($"  ✓ Effect converted: Kind={converted.Kind}, Target={converted.Target}");
                    effects.Add(converted);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  ✗ Effect conversion failed: {ex.Message}");
                    throw;
                }
            }

            var entry = new VorteilNachteil
            {
                Id = string.IsNullOrWhiteSpace(Id) ? GenerateId(Name) : Id,
                Name = Name.Trim(),
                Beschreibung = Beschreibung.Trim(),
                Kategorie = Kategorie,
                MaxStufe = MaxStufe,
                ApKostenProStufe = ParseApKosten(ApKostenText),
                Anmerkungen = Anmerkungen.Trim(),
                TalentTyp = TalentTyp,
                IsHomebrew = true,
                ExplicitEffects = effects
            };

            System.Diagnostics.Debug.WriteLine($"[VorteilNachteilEditModel.ToCatalogEntry] Entry created: Id={entry.Id}, Name={entry.Name}, Effects={entry.ExplicitEffects.Count}");
            return entry;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VorteilNachteilEditModel.ToCatalogEntry] CRITICAL ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[VorteilNachteilEditModel.ToCatalogEntry] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>Validiert das Model. Gibt true zurück wenn gültig, sonst false mit Fehlermeldung.</summary>
    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Name darf nicht leer sein.";
            return false;
        }

        var apKosten = ParseApKosten(ApKostenText);
        if (apKosten.Count == 0 && TalentTyp == TalentGebundenerTyp.Keiner)
        {
            error = "AP-Kosten müssen angegeben werden (z.B. '20' oder '10, 15, 20').";
            return false;
        }

        if (apKosten.Count > 0 && apKosten.Count < MaxStufe)
        {
            error = $"AP-Kosten müssen für alle {MaxStufe} Stufen angegeben werden.";
            return false;
        }

        // Effekte validieren
        foreach (var effect in Effects)
        {
            if (effect.Kind == EffectKind.Modifier && string.IsNullOrWhiteSpace(effect.Target))
            {
                error = "Jeder Modifier-Effekt braucht einen Zielwert.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static List<int> ParseApKosten(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
            .ToList();
    }

    private static string GenerateId(string name)
    {
        var slug = string.Concat(name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        return $"hb_{slug}_{DateTime.UtcNow.Ticks % 100000}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ===========================================================================
// Effekt-Zielwert-System (Target Picker)
// ===========================================================================

/// <summary>Kategorie eines Effekt-Zielwerts für den Picker.</summary>
public enum EffectTargetCategory
{
    Eigenschaft,
    AbgeleiteterWert,
    TalentFw,
    Talentprobe,
    Talentgruppe,
    KampftechnikKtw,
    KampftechnikAtFk,
    KampftechnikPa
}

/// <summary>Eine wählbare Option im Zielwert-Picker.</summary>
public sealed class EffectTargetOption
{
    public string Label { get; init; } = string.Empty;
    public string TargetString { get; init; } = string.Empty;

    public override string ToString() => Label;
}

/// <summary>Stellt die vordefinierten Zielwert-Optionen für den Effekt-Editor bereit.</summary>
public static class EffectTargetOptions
{
    // --- Kategorie-Anzeigenamen ---

    public static string[] KategorieLabels { get; } =
    [
        "Eigenschaft",
        "Abgeleiteter Wert",
        "Talent (FW-Bonus)",
        "Talentprobe (±)",
        "Talentgruppe (±)",
        "Kampftechnik (KTW)",
        "Kampftechnik (AT/FK)",
        "Kampftechnik (PA)"
    ];

    public static EffectTargetCategory[] Kategorien { get; } = Enum.GetValues<EffectTargetCategory>();

    // --- Vordefinierte Optionen je Kategorie ---

    public static EffectTargetOption[] Eigenschaften { get; } =
    [
        new() { Label = "Mut (MU)", TargetString = "attribute.MU" },
        new() { Label = "Klugheit (KL)", TargetString = "attribute.KL" },
        new() { Label = "Intuition (IN)", TargetString = "attribute.IN" },
        new() { Label = "Charisma (CH)", TargetString = "attribute.CH" },
        new() { Label = "Fingerfertigkeit (FF)", TargetString = "attribute.FF" },
        new() { Label = "Gewandtheit (GE)", TargetString = "attribute.GE" },
        new() { Label = "Konstitution (KO)", TargetString = "attribute.KO" },
        new() { Label = "Körperkraft (KK)", TargetString = "attribute.KK" }
    ];

    public static EffectTargetOption[] AbgeleiteteWerte { get; } =
    [
        new() { Label = "Lebensenergie (LeP)", TargetString = "derived.LEP" },
        new() { Label = "Astralenergie (AsP)", TargetString = "derived.ASP" },
        new() { Label = "Karmaenergie (KaP)", TargetString = "derived.KAP" },
        new() { Label = "Seelenkraft (SK)", TargetString = "derived.SK" },
        new() { Label = "Zähigkeit (ZK)", TargetString = "derived.ZK" },
        new() { Label = "Ausweichen (AW)", TargetString = "derived.AW" },
        new() { Label = "Initiative (INI)", TargetString = "derived.INI" },
        new() { Label = "Geschwindigkeit (GS)", TargetString = "derived.GS" },
        new() { Label = "Schicksalspunkte (SchiP)", TargetString = "derived.SchiP" },
        new() { Label = "Wundschwelle (WS)", TargetString = "derived.WS" }
    ];

    public static EffectTargetOption[] Talente { get; } =
    [
        // Körpertalente
        new() { Label = "Fliegen", TargetString = "Fliegen" },
        new() { Label = "Gaukeleien", TargetString = "Gaukeleien" },
        new() { Label = "Klettern", TargetString = "Klettern" },
        new() { Label = "Körperbeherrschung", TargetString = "Körperbeherrschung" },
        new() { Label = "Kraftakt", TargetString = "Kraftakt" },
        new() { Label = "Reiten", TargetString = "Reiten" },
        new() { Label = "Schwimmen", TargetString = "Schwimmen" },
        new() { Label = "Selbstbeherrschung", TargetString = "Selbstbeherrschung" },
        new() { Label = "Singen", TargetString = "Singen" },
        new() { Label = "Sinnesschärfe", TargetString = "Sinnesschärfe" },
        new() { Label = "Tanzen", TargetString = "Tanzen" },
        new() { Label = "Taschendiebstahl", TargetString = "Taschendiebstahl" },
        new() { Label = "Verbergen", TargetString = "Verbergen" },
        new() { Label = "Zechen", TargetString = "Zechen" },
        // Gesellschaftstalente
        new() { Label = "Bekehren & Überzeugen", TargetString = "Bekehren & Überzeugen" },
        new() { Label = "Betören", TargetString = "Betören" },
        new() { Label = "Einschüchtern", TargetString = "Einschüchtern" },
        new() { Label = "Etikette", TargetString = "Etikette" },
        new() { Label = "Gassenwissen", TargetString = "Gassenwissen" },
        new() { Label = "Menschenkenntnis", TargetString = "Menschenkenntnis" },
        new() { Label = "Überreden", TargetString = "Überreden" },
        new() { Label = "Verkleiden", TargetString = "Verkleiden" },
        new() { Label = "Willenskraft", TargetString = "Willenskraft" },
        // Naturtalente
        new() { Label = "Fährtensuchen", TargetString = "Fährtensuchen" },
        new() { Label = "Fesseln", TargetString = "Fesseln" },
        new() { Label = "Fischen & Angeln", TargetString = "Fischen & Angeln" },
        new() { Label = "Orientierung", TargetString = "Orientierung" },
        new() { Label = "Pflanzenkunde", TargetString = "Pflanzenkunde" },
        new() { Label = "Tierkunde", TargetString = "Tierkunde" },
        new() { Label = "Wildnisleben", TargetString = "Wildnisleben" },
        // Wissenstalente
        new() { Label = "Brett- & Glücksspiel", TargetString = "Brett- & Glücksspiel" },
        new() { Label = "Geographie", TargetString = "Geographie" },
        new() { Label = "Geschichtswissen", TargetString = "Geschichtswissen" },
        new() { Label = "Götter & Kulte", TargetString = "Götter & Kulte" },
        new() { Label = "Kriegskunst", TargetString = "Kriegskunst" },
        new() { Label = "Magiekunde", TargetString = "Magiekunde" },
        new() { Label = "Mechanik", TargetString = "Mechanik" },
        new() { Label = "Rechnen", TargetString = "Rechnen" },
        new() { Label = "Rechtskunde", TargetString = "Rechtskunde" },
        new() { Label = "Sagen & Legenden", TargetString = "Sagen & Legenden" },
        new() { Label = "Sphärenkunde", TargetString = "Sphärenkunde" },
        new() { Label = "Sternkunde", TargetString = "Sternkunde" },
        // Handwerkstalente
        new() { Label = "Alchemie", TargetString = "Alchemie" },
        new() { Label = "Boote & Schiffe", TargetString = "Boote & Schiffe" },
        new() { Label = "Fahrzeuge", TargetString = "Fahrzeuge" },
        new() { Label = "Handel", TargetString = "Handel" },
        new() { Label = "Heilkunde Gift", TargetString = "Heilkunde Gift" },
        new() { Label = "Heilkunde Krankheiten", TargetString = "Heilkunde Krankheiten" },
        new() { Label = "Heilkunde Seele", TargetString = "Heilkunde Seele" },
        new() { Label = "Heilkunde Wunden", TargetString = "Heilkunde Wunden" },
        new() { Label = "Holzbearbeitung", TargetString = "Holzbearbeitung" },
        new() { Label = "Lebensmittelbearbeitung", TargetString = "Lebensmittelbearbeitung" },
        new() { Label = "Lederbearbeitung", TargetString = "Lederbearbeitung" },
        new() { Label = "Malen & Zeichnen", TargetString = "Malen & Zeichnen" },
        new() { Label = "Musizieren", TargetString = "Musizieren" },
        new() { Label = "Schlösserknacken", TargetString = "Schlösserknacken" },
        new() { Label = "Steinbearbeitung", TargetString = "Steinbearbeitung" },
        new() { Label = "Stoffbearbeitung", TargetString = "Stoffbearbeitung" },
        new() { Label = "Erdbearbeitung", TargetString = "Erdbearbeitung" },
        new() { Label = "Metallbearbeitung", TargetString = "Metallbearbeitung" }
    ];

    public static EffectTargetOption[] Talentgruppen { get; } =
    [
        new() { Label = "Körpertalente", TargetString = "Körpertalente" },
        new() { Label = "Gesellschaftstalente", TargetString = "Gesellschaftstalente" },
        new() { Label = "Naturtalente", TargetString = "Naturtalente" },
        new() { Label = "Wissenstalente", TargetString = "Wissenstalente" },
        new() { Label = "Handwerkstalente", TargetString = "Handwerkstalente" }
    ];

    public static EffectTargetOption[] Kampftechniken { get; } =
    [
        new() { Label = "Armbrüste", TargetString = "Armbrüste" },
        new() { Label = "Bögen", TargetString = "Bögen" },
        new() { Label = "Dolche", TargetString = "Dolche" },
        new() { Label = "Fechtwaffen", TargetString = "Fechtwaffen" },
        new() { Label = "Hiebwaffen", TargetString = "Hiebwaffen" },
        new() { Label = "Kettenwaffen", TargetString = "Kettenwaffen" },
        new() { Label = "Lanzen", TargetString = "Lanzen" },
        new() { Label = "Raufen", TargetString = "Raufen" },
        new() { Label = "Schilde", TargetString = "Schilde" },
        new() { Label = "Schwerter", TargetString = "Schwerter" },
        new() { Label = "Stangenwaffen", TargetString = "Stangenwaffen" },
        new() { Label = "Wurfwaffen", TargetString = "Wurfwaffen" },
        new() { Label = "Zweihandhiebwaffen", TargetString = "Zweihandhiebwaffen" },
        new() { Label = "Zweihandschwerter", TargetString = "Zweihandschwerter" }
    ];

    /// <summary>Gibt die Optionen für die gewählte Kategorie zurück.</summary>
    public static EffectTargetOption[] GetOptionsForCategory(EffectTargetCategory category) => category switch
    {
        EffectTargetCategory.Eigenschaft => Eigenschaften,
        EffectTargetCategory.AbgeleiteterWert => AbgeleiteteWerte,
        EffectTargetCategory.TalentFw => Talente,
        EffectTargetCategory.Talentprobe => Talente,
        EffectTargetCategory.Talentgruppe => Talentgruppen,
        EffectTargetCategory.KampftechnikKtw => Kampftechniken,
        EffectTargetCategory.KampftechnikAtFk => Kampftechniken,
        EffectTargetCategory.KampftechnikPa => Kampftechniken,
        _ => []
    };

    /// <summary>Erzeugt den finalen Target-String aus Kategorie + gewählter Option.</summary>
    public static string BuildTargetString(EffectTargetCategory category, EffectTargetOption? option)
    {
        if (option is null) return string.Empty;

        return category switch
        {
            EffectTargetCategory.Eigenschaft => option.TargetString,       // "attribute.MU"
            EffectTargetCategory.AbgeleiteterWert => option.TargetString,  // "derived.GS"
            EffectTargetCategory.TalentFw => $"fw.{option.TargetString}",
            EffectTargetCategory.Talentprobe => $"check.talent.{option.TargetString}",
            EffectTargetCategory.Talentgruppe => $"check.talentGroup.{option.TargetString}",
            EffectTargetCategory.KampftechnikKtw => $"combat.{option.TargetString}.KTW",
            EffectTargetCategory.KampftechnikAtFk => $"combat.{option.TargetString}.AT",
            EffectTargetCategory.KampftechnikPa => $"combat.{option.TargetString}.PA",
            _ => string.Empty
        };
    }

    /// <summary>Parst einen Target-String zurück in Kategorie + Option (für Edit-Modus).</summary>
    public static (EffectTargetCategory category, EffectTargetOption? option) ParseTargetString(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return (EffectTargetCategory.AbgeleiteterWert, null);

        // attribute.XX
        if (target.StartsWith("attribute."))
        {
            var opt = Eigenschaften.FirstOrDefault(o => o.TargetString == target);
            return (EffectTargetCategory.Eigenschaft, opt);
        }

        // derived.XX
        if (target.StartsWith("derived."))
        {
            var opt = AbgeleiteteWerte.FirstOrDefault(o => o.TargetString == target);
            return (EffectTargetCategory.AbgeleiteterWert, opt);
        }

        // fw.TalentName
        if (target.StartsWith("fw."))
        {
            var name = target["fw.".Length..];
            var opt = Talente.FirstOrDefault(o => o.TargetString == name);
            return (EffectTargetCategory.TalentFw, opt);
        }

        // check.talent.TalentName
        if (target.StartsWith("check.talent."))
        {
            var name = target["check.talent.".Length..];
            var opt = Talente.FirstOrDefault(o => o.TargetString == name);
            return (EffectTargetCategory.Talentprobe, opt);
        }

        // check.talentGroup.GroupName
        if (target.StartsWith("check.talentGroup."))
        {
            var name = target["check.talentGroup.".Length..];
            var opt = Talentgruppen.FirstOrDefault(o => o.TargetString == name);
            return (EffectTargetCategory.Talentgruppe, opt);
        }

        // combat.Technique.KTW / .AT / .PA
        if (target.StartsWith("combat."))
        {
            var rest = target["combat.".Length..];
            if (rest.EndsWith(".PA"))
            {
                var name = rest[..^3];
                var opt = Kampftechniken.FirstOrDefault(o => o.TargetString == name);
                return (EffectTargetCategory.KampftechnikPa, opt);
            }
            if (rest.EndsWith(".AT"))
            {
                var name = rest[..^3];
                var opt = Kampftechniken.FirstOrDefault(o => o.TargetString == name);
                return (EffectTargetCategory.KampftechnikAtFk, opt);
            }
            if (rest.EndsWith(".KTW"))
            {
                var name = rest[..^4];
                var opt = Kampftechniken.FirstOrDefault(o => o.TargetString == name);
                return (EffectTargetCategory.KampftechnikKtw, opt);
            }
        }

        return (EffectTargetCategory.AbgeleiteterWert, null);
    }
}

// ===========================================================================
// RuleEffectEditModel
// ===========================================================================

/// <summary>
/// Mutables Formular-Model für einen einzelnen RuleEffect.
/// </summary>
public class RuleEffectEditModel : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private EffectKind _kind = EffectKind.Modifier;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _target = string.Empty;
    private ModifierOp _operation = ModifierOp.Add;
    private decimal _value;
    private string _valueText = "0";
    private bool _perLevel;
    private ModifierPhase _phase = ModifierPhase.DerivedValues;
    private StackingRule _stacking = StackingRule.Stack;
    private EffectTargetCategory _targetCategory = EffectTargetCategory.AbgeleiteterWert;
    private EffectTargetOption? _targetOption;
    private EffectTargetOption[] _targetSubOptions = EffectTargetOptions.AbgeleiteteWerte;

    public string Id
    {
        get => _id;
        set { if (_id != value) { _id = value; Notify(); } }
    }

    public EffectKind Kind
    {
        get => _kind;
        set { if (_kind != value) { _kind = value; Notify(); Notify(nameof(IsModifier)); Notify(nameof(IsNarrative)); } }
    }

    public string Title
    {
        get => _title;
        set { if (_title != value) { _title = value; Notify(); } }
    }

    public string Description
    {
        get => _description;
        set { if (_description != value) { _description = value; Notify(); } }
    }

    /// <summary>
    /// Der endgültige Target-String im Punkt-Format (z.B. "derived.GS").
    /// Wird automatisch aus TargetCategory + TargetOption berechnet.
    /// </summary>
    public string Target
    {
        get => _target;
        set { if (_target != value) { _target = value; Notify(); } }
    }

    /// <summary>Gewählte Kategorie im Ziel-Picker.</summary>
    public EffectTargetCategory TargetCategory
    {
        get => _targetCategory;
        set
        {
            if (_targetCategory != value)
            {
                _targetCategory = value;
                Notify();
                Notify(nameof(TargetCategoryIndex));
                // Sub-Optionen aktualisieren
                var subOptions = EffectTargetOptions.GetOptionsForCategory(value);
                TargetSubOptions = subOptions;
                // Option nur zurücksetzen wenn noch keine gesetzt oder nicht mehr in den SubOptions
                if (_targetOption == null || !subOptions.Any(o => o.TargetString == _targetOption.TargetString))
                {
                    TargetOption = subOptions.Length > 0 ? subOptions[0] : null;
                }
            }
        }
    }

    /// <summary>Index-basiertes Binding für den Kategorie-Picker (string-Array).</summary>
    public int TargetCategoryIndex
    {
        get => (int)_targetCategory;
        set
        {
            var cat = (EffectTargetCategory)Math.Clamp(value, 0, EffectTargetOptions.Kategorien.Length - 1);
            TargetCategory = cat;
        }
    }

    /// <summary>Gewählte Unter-Option im Ziel-Picker.</summary>
    public EffectTargetOption? TargetOption
    {
        get => _targetOption;
        set
        {
            if (_targetOption != value)
            {
                _targetOption = value;
                Notify();
                // Target-String automatisch aktualisieren
                Target = EffectTargetOptions.BuildTargetString(_targetCategory, value);
            }
        }
    }

    /// <summary>Verfügbare Unter-Optionen basierend auf der gewählten Kategorie.</summary>
    public EffectTargetOption[] TargetSubOptions
    {
        get => _targetSubOptions;
        private set { if (_targetSubOptions != value) { _targetSubOptions = value; Notify(); } }
    }

    public ModifierOp Operation
    {
        get => _operation;
        set { if (_operation != value) { _operation = value; Notify(); } }
    }

    public decimal Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                Notify();
                // ValueText synchron halten (ohne Endlosschleife)
                var newText = value.ToString("G");
                if (_valueText != newText)
                {
                    _valueText = newText;
                    Notify(nameof(ValueText));
                }
            }
        }
    }

    /// <summary>
    /// Sicheres String-Binding für den Wert (verhindert Crash bei leerem Entry).
    /// </summary>
    public string ValueText
    {
        get => _valueText;
        set
        {
            if (_valueText != value)
            {
                _valueText = value;
                Notify();
                // Versuche zu parsen, bei Fehler bleibt Value unverändert
                if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    if (_value != parsed)
                    {
                        _value = parsed;
                        Notify(nameof(Value));
                    }
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    _value = 0;
                    Notify(nameof(Value));
                }
            }
        }
    }

    public bool PerLevel
    {
        get => _perLevel;
        set { if (_perLevel != value) { _perLevel = value; Notify(); } }
    }

    public ModifierPhase Phase
    {
        get => _phase;
        set { if (_phase != value) { _phase = value; Notify(); } }
    }

    public StackingRule Stacking
    {
        get => _stacking;
        set { if (_stacking != value) { _stacking = value; Notify(); } }
    }

    // --- Computed UI-Properties ---

    public bool IsModifier => Kind == EffectKind.Modifier;
    public bool IsNarrative => Kind == EffectKind.Narrative;

    // --- Statische Listen für Picker-Binding ---

    public static EffectKind[] KindOptionen { get; } = Enum.GetValues<EffectKind>();
    public static ModifierOp[] OperationOptionen { get; } = Enum.GetValues<ModifierOp>();
    public static ModifierPhase[] PhaseOptionen { get; } = Enum.GetValues<ModifierPhase>();
    public static StackingRule[] StackingOptionen { get; } = Enum.GetValues<StackingRule>();
    public static string[] TargetKategorieOptionen { get; } = EffectTargetOptions.KategorieLabels;

    // --- Konvertierung ---

    public static RuleEffectEditModel FromEffect(RuleEffect effect)
    {
        var model = new RuleEffectEditModel
        {
            Id = effect.Id,
            Kind = effect.Kind,
            Title = effect.Title ?? string.Empty,
            Description = effect.Description ?? string.Empty,
            Operation = effect.Operation ?? ModifierOp.Add,
            Value = effect.Value ?? 0,
            PerLevel = effect.PerLevel,
            Phase = effect.Phase,
            Stacking = effect.Stacking
        };

        // Target-String parsen und Picker-State setzen
        var (category, option) = EffectTargetOptions.ParseTargetString(effect.Target);
        model.TargetCategory = category;
        model.TargetOption = option;
        // TargetSubOptions wird automatisch in der TargetCategory Property gesetzt
        model.Target = effect.Target ?? string.Empty;

        return model;
    }

    public RuleEffect ToRuleEffect()
    {
        try
        {
            return new RuleEffect
            {
                Id = string.IsNullOrWhiteSpace(Id) ? $"eff_{Guid.NewGuid():N}"[..16] : Id,
                Kind = Kind,
                Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Target = IsModifier && !string.IsNullOrWhiteSpace(Target) ? Target.Trim() : null,
                Operation = IsModifier ? Operation : null,
                Value = IsModifier ? Value : null,
                PerLevel = IsModifier && PerLevel,
                Phase = Phase,
                Stacking = Stacking
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RuleEffectEditModel.ToRuleEffect] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[RuleEffectEditModel.ToRuleEffect] Kind={Kind}, Target={Target}, Value={Value}, Operation={Operation}");
            throw;
        }
    }

    /// <summary>Kurztext-Zusammenfassung für die Listendarstellung.</summary>
    public string Zusammenfassung => Kind switch
    {
        EffectKind.Narrative => $"[Narrativ] {Title}",
        EffectKind.Modifier => $"{Target} {Operation} {Value}{(PerLevel ? " /Stufe" : "")}",
        _ => "?"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
