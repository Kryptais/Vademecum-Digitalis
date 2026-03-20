using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly CharacterSheet _sheet = new();
    private readonly PersistenceService _persistence;
    private CancellationTokenSource? _saveCts;

    public MainPageViewModel() : this(new PersistenceService())
    {
    }

    public MainPageViewModel(PersistenceService persistence)
    {
        _persistence = persistence;
        TalentGruppen = BuildTalentGruppen();
        SubscribeToTalentChanges();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TalentGroup> TalentGruppen { get; }

    // --- Spezies-Auswahl ---

    /// <summary>Alle verfügbaren Spezies-Namen für den Picker.</summary>
    public IReadOnlyList<string> SpeziesListe { get; } =
        SpeziesData.Alle.Select(s => s.Name).ToList();

    /// <summary>Die aktuell gewählte Spezies-Daten (oder null bei unbekannter Spezies).</summary>
    public SpeziesData? AktuelleSpezies => SpeziesData.FindByName(_sheet.Spezies);

    // --- Laden / Speichern ---

    public async Task LoadDataAsync()
    {
        var data = await _persistence.LoadCharacterSheetAsync();
        if (data == null) return;

        var s = data.Sheet;
        // Charakterinfos
        _sheet.Name = s.Name;
        _sheet.Spieler = s.Spieler;
        _sheet.Spezies = s.Spezies;
        _sheet.Kultur = s.Kultur;
        _sheet.Profession = s.Profession;
        _sheet.Geschlecht = s.Geschlecht;
        _sheet.Geburtstag = s.Geburtstag;
        _sheet.Alter = s.Alter;
        _sheet.Größe = s.Größe;
        _sheet.Gewicht = s.Gewicht;
        _sheet.Haarfarbe = s.Haarfarbe;
        _sheet.Augenfarbe = s.Augenfarbe;
        _sheet.Sozialstatus = s.Sozialstatus;

        // Hauptattribute
        _sheet.Mut = s.Mut;
        _sheet.Klugheit = s.Klugheit;
        _sheet.Intuition = s.Intuition;
        _sheet.Charisma = s.Charisma;
        _sheet.Fingerfertigkeit = s.Fingerfertigkeit;
        _sheet.Gewandtheit = s.Gewandtheit;
        _sheet.Konstitution = s.Konstitution;
        _sheet.Körperkraft = s.Körperkraft;

        // Migration: Wenn Zugekauft-Felder 0 sind aber Legacy-Felder Werte haben,
        // berechne den Zugekauft-Anteil als Differenz zum Formelwert.
        MigrateZugekauft(s);

        // Zugekaufte Modifikatoren
        _sheet.LebensenergieZugekauft = s.LebensenergieZugekauft;
        _sheet.AstralenergieZugekauft = s.AstralenergieZugekauft;
        _sheet.KarmaenergieZugekauft = s.KarmaenergieZugekauft;
        _sheet.SeelenkraftZugekauft = s.SeelenkraftZugekauft;
        _sheet.ZähigkeitZugekauft = s.ZähigkeitZugekauft;

        // AP / SchiP
        _sheet.AbenteuerpunkteGesamt = s.AbenteuerpunkteGesamt;
        _sheet.AbenteuerpunkteVerfuegbar = s.AbenteuerpunkteVerfuegbar;
        _sheet.AbenteuerpunkteAusgegeben = s.AbenteuerpunkteAusgegeben;
        _sheet.SchicksalspunkteGesamt = s.SchicksalspunkteGesamt;
        _sheet.SchicksalspunkteVerfuegbar = s.SchicksalspunkteVerfuegbar;

        // Freitext
        _sheet.Vorteile = s.Vorteile;
        _sheet.Nachteile = s.Nachteile;
        _sheet.Talente = s.Talente;
        _sheet.Kampftalente = s.Kampftalente;

        // Talentwerte (FW + Anmerkung) auf TalentRows mappen
        if (data.TalentValues != null)
        {
            var lookup = data.TalentValues.ToDictionary(t => t.Talent, t => t);
            foreach (var group in TalentGruppen)
            {
                foreach (var row in group.Eintraege)
                {
                    if (lookup.TryGetValue(row.Talent, out var saved))
                    {
                        row.Fw = saved.Fw;
                        row.Anmerkung = saved.Anmerkung;
                    }
                }
            }
        }

        // Alle Properties der UI melden
        NotifyAllProperties();
    }

    /// <summary>
    /// Migration: Alte Savegames haben nur Lebensenergie/Seelenkraft etc. als Gesamtwerte.
    /// Wenn ZugekauftFelder alle 0 sind und ein Legacy-Wert vorhanden ist,
    /// berechne Zugekauft = LegacyWert - Formelwert.
    /// </summary>
    private void MigrateZugekauft(CharacterSheet s)
    {
        bool hasLegacy = s.Lebensenergie > 0 || s.Seelenkraft > 0 || s.Zähigkeit > 0;
        bool hasZugekauft = s.LebensenergieZugekauft != 0 || s.SeelenkraftZugekauft != 0 || s.ZähigkeitZugekauft != 0;

        if (hasLegacy && !hasZugekauft)
        {
            var spez = SpeziesData.FindByName(s.Spezies);
            int lepBasis = spez?.LePBasis ?? 5;
            int skMod = spez?.SeelenkraftMod ?? -5;
            int zkMod = spez?.ZähigkeitMod ?? -5;

            int lepFormel = 2 * s.Konstitution + lepBasis;
            s.LebensenergieZugekauft = s.Lebensenergie - lepFormel;

            int skFormel = (int)Math.Ceiling((s.Mut + s.Klugheit + s.Intuition) / 6.0) + skMod;
            s.SeelenkraftZugekauft = s.Seelenkraft - skFormel;

            int zkFormel = (int)Math.Ceiling((s.Konstitution + s.Konstitution + s.Körperkraft) / 6.0) + zkMod;
            s.ZähigkeitZugekauft = s.Zähigkeit - zkFormel;

            s.AstralenergieZugekauft = s.Astralenergie; // AsP hat keine Grundformel ohne Vorteil
            s.KarmaenergieZugekauft = s.Karmaenergie;   // KaP hat keine Grundformel ohne Vorteil
        }
    }

    private CharacterSheetData BuildSaveData()
    {
        var talentValues = new List<TalentSaveEntry>();
        foreach (var group in TalentGruppen)
        {
            foreach (var row in group.Eintraege)
            {
                if (!string.IsNullOrEmpty(row.Fw) || !string.IsNullOrEmpty(row.Anmerkung))
                {
                    talentValues.Add(new TalentSaveEntry
                    {
                        Talent = row.Talent,
                        Fw = row.Fw,
                        Anmerkung = row.Anmerkung
                    });
                }
            }
        }

        return new CharacterSheetData
        {
            Sheet = new CharacterSheet
            {
                Name = _sheet.Name,
                Spieler = _sheet.Spieler,
                Spezies = _sheet.Spezies,
                Kultur = _sheet.Kultur,
                Profession = _sheet.Profession,
                Geschlecht = _sheet.Geschlecht,
                Geburtstag = _sheet.Geburtstag,
                Alter = _sheet.Alter,
                Größe = _sheet.Größe,
                Gewicht = _sheet.Gewicht,
                Haarfarbe = _sheet.Haarfarbe,
                Augenfarbe = _sheet.Augenfarbe,
                Sozialstatus = _sheet.Sozialstatus,
                Mut = _sheet.Mut,
                Klugheit = _sheet.Klugheit,
                Intuition = _sheet.Intuition,
                Charisma = _sheet.Charisma,
                Fingerfertigkeit = _sheet.Fingerfertigkeit,
                Gewandtheit = _sheet.Gewandtheit,
                Konstitution = _sheet.Konstitution,
                Körperkraft = _sheet.Körperkraft,
                // Speichere die Zugekauft-Werte
                LebensenergieZugekauft = _sheet.LebensenergieZugekauft,
                AstralenergieZugekauft = _sheet.AstralenergieZugekauft,
                KarmaenergieZugekauft = _sheet.KarmaenergieZugekauft,
                SeelenkraftZugekauft = _sheet.SeelenkraftZugekauft,
                ZähigkeitZugekauft = _sheet.ZähigkeitZugekauft,
                // Speichere auch die berechneten Gesamtwerte für Abwärtskompatibilität
                Lebensenergie = Lebensenergie,
                Astralenergie = Astralenergie,
                Karmaenergie = Karmaenergie,
                Seelenkraft = Seelenkraft,
                Zähigkeit = Zähigkeit,
                InitiativeBasis = InitiativeBasis,
                Geschwindigkeit = Geschwindigkeit,
                AbenteuerpunkteGesamt = _sheet.AbenteuerpunkteGesamt,
                AbenteuerpunkteVerfuegbar = _sheet.AbenteuerpunkteVerfuegbar,
                AbenteuerpunkteAusgegeben = _sheet.AbenteuerpunkteAusgegeben,
                SchicksalspunkteGesamt = _sheet.SchicksalspunkteGesamt,
                SchicksalspunkteVerfuegbar = _sheet.SchicksalspunkteVerfuegbar,
                Vorteile = _sheet.Vorteile,
                Nachteile = _sheet.Nachteile,
                Talente = _sheet.Talente,
                Kampftalente = _sheet.Kampftalente
            },
            TalentValues = talentValues
        };
    }

    private async Task SaveDataAsync()
    {
        try
        {
            var data = BuildSaveData();
            await _persistence.SaveCharacterSheetAsync(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving character sheet: {ex.Message}");
        }
    }

    private void RequestDelayedSave()
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000, token);
                if (token.IsCancellationRequested) return;

                await SaveDataAsync();
            }
            catch (TaskCanceledException) { /* expected */ }
        });
    }

    private void SubscribeToTalentChanges()
    {
        foreach (var group in TalentGruppen)
        {
            foreach (var row in group.Eintraege)
            {
                row.PropertyChanged += (_, _) => RequestDelayedSave();
            }
        }
    }

    /// <summary>
    /// Benachrichtigt die UI über alle berechneten Basiswerte, die sich bei
    /// Attribut- oder Speziesänderung ändern können.
    /// </summary>
    private void NotifyDerivedValues()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lebensenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seelenkraft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zähigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geschwindigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AktuelleSpezies)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeziesInfoText)));
    }

    private void NotifyAllProperties()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Spieler)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Spezies)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kultur)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Profession)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geschlecht)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geburtstag)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alter)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Größe)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gewicht)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Haarfarbe)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Augenfarbe)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sozialstatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mut)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Klugheit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Intuition)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Charisma)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fingerfertigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gewandtheit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Konstitution)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Körperkraft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieZugekauft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralenergieZugekauft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieZugekauft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftZugekauft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitZugekauft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteVerfuegbar)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteAusgegeben)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteVerfuegbar)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Vorteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Nachteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Talente)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kampftalente)));
        NotifyDerivedValues();
    }

    // --- Properties ---

    public string Name
    {
        get => _sheet.Name;
        set => SetProperty(_sheet.Name, value, v => _sheet.Name = v);
    }

    public string Spieler
    {
        get => _sheet.Spieler;
        set => SetProperty(_sheet.Spieler, value, v => _sheet.Spieler = v);
    }

    public string Spezies
    {
        get => _sheet.Spezies;
        set
        {
            if (_sheet.Spezies == value) return;
            _sheet.Spezies = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Spezies)));
            NotifyDerivedValues();
            RequestDelayedSave();
        }
    }

    /// <summary>Info-Text zur gewählten Spezies (LeP-Basis, SK-Mod, ZK-Mod, GS).</summary>
    public string SpeziesInfoText
    {
        get
        {
            var sp = AktuelleSpezies;
            if (sp == null) return string.Empty;
            return $"LeP-Basis {sp.LePBasis} | SK {sp.SeelenkraftMod:+#;-#;0} | ZK {sp.ZähigkeitMod:+#;-#;0} | GS {sp.Geschwindigkeit} | SchiP {sp.SchicksalspunkteMax}";
        }
    }

    public string Kultur
    {
        get => _sheet.Kultur;
        set => SetProperty(_sheet.Kultur, value, v => _sheet.Kultur = v);
    }

    public string Profession
    {
        get => _sheet.Profession;
        set => SetProperty(_sheet.Profession, value, v => _sheet.Profession = v);
    }

    public string Geschlecht
    {
        get => _sheet.Geschlecht;
        set => SetProperty(_sheet.Geschlecht, value, v => _sheet.Geschlecht = v);
    }

    public string Geburtstag
    {
        get => _sheet.Geburtstag;
        set => SetProperty(_sheet.Geburtstag, value, v => _sheet.Geburtstag = v);
    }

    public string Alter
    {
        get => _sheet.Alter;
        set => SetProperty(_sheet.Alter, value, v => _sheet.Alter = v);
    }

    public string Größe
    {
        get => _sheet.Größe;
        set => SetProperty(_sheet.Größe, value, v => _sheet.Größe = v);
    }

    public string Gewicht
    {
        get => _sheet.Gewicht;
        set => SetProperty(_sheet.Gewicht, value, v => _sheet.Gewicht = v);
    }

    public string Haarfarbe
    {
        get => _sheet.Haarfarbe;
        set => SetProperty(_sheet.Haarfarbe, value, v => _sheet.Haarfarbe = v);
    }

    public string Augenfarbe
    {
        get => _sheet.Augenfarbe;
        set => SetProperty(_sheet.Augenfarbe, value, v => _sheet.Augenfarbe = v);
    }

    public string Sozialstatus
    {
        get => _sheet.Sozialstatus;
        set => SetProperty(_sheet.Sozialstatus, value, v => _sheet.Sozialstatus = v);
    }

    // --- Hauptattribute (lösen Neuberechnung der Basiswerte aus) ---

    public int Mut
    {
        get => _sheet.Mut;
        set => SetAttributeProperty(_sheet.Mut, value, v => _sheet.Mut = v);
    }

    public int Klugheit
    {
        get => _sheet.Klugheit;
        set => SetAttributeProperty(_sheet.Klugheit, value, v => _sheet.Klugheit = v);
    }

    public int Intuition
    {
        get => _sheet.Intuition;
        set => SetAttributeProperty(_sheet.Intuition, value, v => _sheet.Intuition = v);
    }

    public int Charisma
    {
        get => _sheet.Charisma;
        set => SetAttributeProperty(_sheet.Charisma, value, v => _sheet.Charisma = v);
    }

    public int Fingerfertigkeit
    {
        get => _sheet.Fingerfertigkeit;
        set => SetAttributeProperty(_sheet.Fingerfertigkeit, value, v => _sheet.Fingerfertigkeit = v);
    }

    public int Gewandtheit
    {
        get => _sheet.Gewandtheit;
        set => SetAttributeProperty(_sheet.Gewandtheit, value, v => _sheet.Gewandtheit = v);
    }

    public int Konstitution
    {
        get => _sheet.Konstitution;
        set => SetAttributeProperty(_sheet.Konstitution, value, v => _sheet.Konstitution = v);
    }

    public int Körperkraft
    {
        get => _sheet.Körperkraft;
        set => SetAttributeProperty(_sheet.Körperkraft, value, v => _sheet.Körperkraft = v);
    }

    // --- Berechnete Basiswerte (DSA 5 Formeln) ---

    /// <summary>LeP = 2×KO + SpeziesLeP + Zugekauft</summary>
    public int Lebensenergie
    {
        get
        {
            int basis = AktuelleSpezies?.LePBasis ?? 5;
            return 2 * _sheet.Konstitution + basis + _sheet.LebensenergieZugekauft;
        }
    }

    /// <summary>AsP = Zugekauft (nur mit Vorteil Zauberer)</summary>
    public int Astralenergie => _sheet.AstralenergieZugekauft;

    /// <summary>KaP = Zugekauft (nur mit Vorteil Geweihter)</summary>
    public int Karmaenergie => _sheet.KarmaenergieZugekauft;

    /// <summary>SK = ceil((MU+KL+IN)/6) + SpeziesMod + Zugekauft</summary>
    public int Seelenkraft
    {
        get
        {
            int mod = AktuelleSpezies?.SeelenkraftMod ?? -5;
            return (int)Math.Ceiling((_sheet.Mut + _sheet.Klugheit + _sheet.Intuition) / 6.0) + mod + _sheet.SeelenkraftZugekauft;
        }
    }

    /// <summary>ZK = ceil((KO+KO+KK)/6) + SpeziesMod + Zugekauft</summary>
    public int Zähigkeit
    {
        get
        {
            int mod = AktuelleSpezies?.ZähigkeitMod ?? -5;
            return (int)Math.Ceiling((_sheet.Konstitution + _sheet.Konstitution + _sheet.Körperkraft) / 6.0) + mod + _sheet.ZähigkeitZugekauft;
        }
    }

    /// <summary>INI = ceil((MU+GE)/2)</summary>
    public int InitiativeBasis => (int)Math.Ceiling((_sheet.Mut + _sheet.Gewandtheit) / 2.0);

    /// <summary>GS = SpeziesGS (Zwerg 6, sonst 8)</summary>
    public int Geschwindigkeit => AktuelleSpezies?.Geschwindigkeit ?? 8;

    // --- Zugekaufte Modifikatoren (editierbar) ---

    public int LebensenergieZugekauft
    {
        get => _sheet.LebensenergieZugekauft;
        set
        {
            if (_sheet.LebensenergieZugekauft == value) return;
            _sheet.LebensenergieZugekauft = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieZugekauft)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lebensenergie)));
            RequestDelayedSave();
        }
    }

    public int AstralenergieZugekauft
    {
        get => _sheet.AstralenergieZugekauft;
        set
        {
            if (_sheet.AstralenergieZugekauft == value) return;
            _sheet.AstralenergieZugekauft = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralenergieZugekauft)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Astralenergie)));
            RequestDelayedSave();
        }
    }

    public int KarmaenergieZugekauft
    {
        get => _sheet.KarmaenergieZugekauft;
        set
        {
            if (_sheet.KarmaenergieZugekauft == value) return;
            _sheet.KarmaenergieZugekauft = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieZugekauft)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Karmaenergie)));
            RequestDelayedSave();
        }
    }

    public int SeelenkraftZugekauft
    {
        get => _sheet.SeelenkraftZugekauft;
        set
        {
            if (_sheet.SeelenkraftZugekauft == value) return;
            _sheet.SeelenkraftZugekauft = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftZugekauft)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seelenkraft)));
            RequestDelayedSave();
        }
    }

    public int ZähigkeitZugekauft
    {
        get => _sheet.ZähigkeitZugekauft;
        set
        {
            if (_sheet.ZähigkeitZugekauft == value) return;
            _sheet.ZähigkeitZugekauft = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitZugekauft)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zähigkeit)));
            RequestDelayedSave();
        }
    }

    // --- AP / SchiP ---

    public int AbenteuerpunkteGesamt
    {
        get => _sheet.AbenteuerpunkteGesamt;
        set => SetProperty(_sheet.AbenteuerpunkteGesamt, value, v => _sheet.AbenteuerpunkteGesamt = v);
    }

    public int AbenteuerpunkteVerfuegbar
    {
        get => _sheet.AbenteuerpunkteVerfuegbar;
        set => SetProperty(_sheet.AbenteuerpunkteVerfuegbar, value, v => _sheet.AbenteuerpunkteVerfuegbar = v);
    }

    public int AbenteuerpunkteAusgegeben
    {
        get => _sheet.AbenteuerpunkteAusgegeben;
        set => SetProperty(_sheet.AbenteuerpunkteAusgegeben, value, v => _sheet.AbenteuerpunkteAusgegeben = v);
    }

    public int SchicksalspunkteGesamt
    {
        get => _sheet.SchicksalspunkteGesamt;
        set => SetProperty(_sheet.SchicksalspunkteGesamt, value, v => _sheet.SchicksalspunkteGesamt = v);
    }

    public int SchicksalspunkteVerfuegbar
    {
        get => _sheet.SchicksalspunkteVerfuegbar;
        set => SetProperty(_sheet.SchicksalspunkteVerfuegbar, value, v => _sheet.SchicksalspunkteVerfuegbar = v);
    }

    // --- Freitext ---

    public string Vorteile
    {
        get => _sheet.Vorteile;
        set => SetProperty(_sheet.Vorteile, value, v => _sheet.Vorteile = v);
    }

    public string Nachteile
    {
        get => _sheet.Nachteile;
        set => SetProperty(_sheet.Nachteile, value, v => _sheet.Nachteile = v);
    }

    public string Talente
    {
        get => _sheet.Talente;
        set => SetProperty(_sheet.Talente, value, v => _sheet.Talente = v);
    }

    public string Kampftalente
    {
        get => _sheet.Kampftalente;
        set => SetProperty(_sheet.Kampftalente, value, v => _sheet.Kampftalente = v);
    }

    // --- Talent-Tabellen (statisch aufgebaut, FW/Anmerkung editierbar) ---

    private static IReadOnlyList<TalentGroup> BuildTalentGruppen()
    {
        return new[]
        {
            new TalentGroup("Körpertalente", new[]
            {
                NewTalent("Fliegen", "B", "MU", "IN", "GE", "JA"),
                NewTalent("Gaukeleien", "A", "MU", "CH", "FF", "JA"),
                NewTalent("Klettern", "B", "MU", "GE", "KK", "JA"),
                NewTalent("Körperbeherrschung", "D", "GE", "GE", "KO", "JA"),
                NewTalent("Kraftakt", "B", "KO", "KK", "KK", "JA"),
                NewTalent("Reiten", "B", "CH", "GE", "KK", "JA"),
                NewTalent("Schwimmen", "B", "GE", "KO", "KK", "JA"),
                NewTalent("Selbstbeherrschung", "D", "MU", "MU", "KO", "NEIN"),
                NewTalent("Singen", "A", "KL", "CH", "KO", "EVTL"),
                NewTalent("Sinnesschärfe", "D", "KL", "IN", "IN", "EVTL"),
                NewTalent("Tanzen", "A", "KL", "CH", "GE", "JA"),
                NewTalent("Taschendiebstahl", "B", "MU", "FF", "GE", "JA"),
                NewTalent("Verbergen", "C", "MU", "IN", "GE", "JA"),
                NewTalent("Zechen", "A", "KL", "KO", "KK", "NEIN")
            }),
            new TalentGroup("Gesellschaftstalente", new[]
            {
                NewTalent("Bekehren & Überzeugen", "B", "MU", "KL", "CH", "NEIN"),
                NewTalent("Betören", "B", "MU", "CH", "CH", "EVTL"),
                NewTalent("Einschüchtern", "B", "MU", "IN", "CH", "NEIN"),
                NewTalent("Etikette", "B", "KL", "IN", "CH", "EVTL"),
                NewTalent("Gassenwissen", "C", "KL", "IN", "CH", "EVTL"),
                NewTalent("Menschenkenntnis", "C", "KL", "IN", "CH", "NEIN"),
                NewTalent("Überreden", "C", "MU", "IN", "CH", "NEIN"),
                NewTalent("Verkleiden", "B", "IN", "CH", "GE", "EVTL"),
                NewTalent("Willenskraft", "D", "MU", "IN", "CH", "NEIN")
            }),
            new TalentGroup("Naturtalente", new[]
            {
                NewTalent("Fährtensuchen", "C", "MU", "IN", "GE", "JA"),
                NewTalent("Fesseln", "A", "KL", "FF", "KK", "EVTL"),
                NewTalent("Fischen & Angeln", "A", "FF", "GE", "KO", "EVTL"),
                NewTalent("Orientierung", "B", "KL", "IN", "IN", "NEIN"),
                NewTalent("Pflanzenkunde", "C", "KL", "FF", "KO", "EVTL"),
                NewTalent("Tierkunde", "C", "MU", "MU", "CH", "JA"),
                NewTalent("Wildnisleben", "C", "MU", "GE", "KO", "JA")
            }),
            new TalentGroup("Wissenstalente", new[]
            {
                NewTalent("Brett- & Glücksspiel", "A", "KL", "KL", "IN", "NEIN"),
                NewTalent("Geographie", "B", "KL", "KL", "IN", "NEIN"),
                NewTalent("Geschichtswissen", "B", "KL", "KL", "IN", "NEIN"),
                NewTalent("Götter & Kulte", "B", "KL", "KL", "IN", "NEIN"),
                NewTalent("Kriegskunst", "B", "MU", "KL", "IN", "NEIN"),
                NewTalent("Magiekunde", "C", "KL", "KL", "IN", "NEIN"),
                NewTalent("Mechanik", "B", "KL", "KL", "FF", "NEIN"),
                NewTalent("Rechnen", "A", "KL", "KL", "IN", "NEIN"),
                NewTalent("Rechtskunde", "A", "KL", "KL", "IN", "NEIN"),
                NewTalent("Sagen & Legenden", "B", "KL", "KL", "IN", "NEIN"),
                NewTalent("Sphärenkunde", "B", "KL", "KL", "IN", "NEIN"),
                NewTalent("Sternkunde", "A", "KL", "KL", "IN", "NEIN")
            }),
            new TalentGroup("Handwerkstalente", new[]
            {
                NewTalent("Alchemie", "C", "MU", "KL", "FF", "JA"),
                NewTalent("Boote & Schiffe", "B", "FF", "GE", "KK", "JA"),
                NewTalent("Fahrzeuge", "A", "CH", "FF", "KO", "JA"),
                NewTalent("Handel", "B", "KL", "IN", "CH", "NEIN"),
                NewTalent("Heilkunde Gift", "B", "MU", "KL", "IN", "JA"),
                NewTalent("Heilkunde Krankheiten", "B", "MU", "IN", "KO", "JA"),
                NewTalent("Heilkunde Seele", "B", "IN", "CH", "KO", "NEIN"),
                NewTalent("Heilkunde Wunden", "D", "KL", "FF", "FF", "JA"),
                NewTalent("Holzbearbeitung", "B", "FF", "GE", "KK", "JA"),
                NewTalent("Lebensmittelbearbeitung", "A", "IN", "FF", "FF", "JA"),
                NewTalent("Lederbearbeitung", "B", "FF", "GE", "KO", "JA"),
                NewTalent("Malen & Zeichnen", "A", "IN", "FF", "FF", "JA"),
                NewTalent("Musizieren", "A", "CH", "FF", "KO", "JA"),
                NewTalent("Schlösserknacken", "C", "IN", "FF", "FF", "JA"),
                NewTalent("Steinbearbeitung", "A", "FF", "FF", "KK", "JA"),
                NewTalent("Stoffbearbeitung", "A", "KL", "FF", "FF", "JA"),
                NewTalent("Erdbearbeitung", "A", "FF", "KO", "KK", "JA"),
                NewTalent("Metallbearbeitung", "C", "FF", "KO", "KK", "JA")
            })
        };
    }

    private static TalentRow NewTalent(string talent, string faktor, string probe1, string probe2, string probe3, string belastungseinfluss)
    {
        return new TalentRow
        {
            Talent = talent,
            Steigerungsfaktor = faktor,
            Probe1 = probe1,
            Probe2 = probe2,
            Probe3 = probe3,
            Belastungseinfluss = belastungseinfluss
        };
    }

    /// <summary>Setzt eine einfache Property, feuert PropertyChanged und speichert verzögert.</summary>
    private void SetProperty<T>(T oldValue, T newValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue)) return;
        setter(newValue);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        RequestDelayedSave();
    }

    /// <summary>
    /// Setzt ein Hauptattribut, feuert PropertyChanged UND aktualisiert alle abgeleiteten Werte.
    /// </summary>
    private void SetAttributeProperty(int oldValue, int newValue, Action<int> setter, [CallerMemberName] string? propertyName = null)
    {
        if (oldValue == newValue) return;
        setter(newValue);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        NotifyDerivedValues();
        RequestDelayedSave();
    }
}
