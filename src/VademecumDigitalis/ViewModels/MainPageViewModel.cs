using System.Collections.ObjectModel;
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

    // --- Ereignisse ---

    public ObservableCollection<CharakterEreignis> Ereignisse { get; } = [];

    /// <summary>True wenn keine Ereignisse vorhanden (für Empty-Label-Binding).</summary>
    public bool KeinEreignisse => Ereignisse.Count == 0;

    /// <summary>Summe aller Alter-Boni aus Ereignissen (in Jahren).</summary>
    public int EreignisAlterBonus => Ereignisse.Sum(e => e.AlterBonus);

    /// <summary>Summe aller SchiP-Boni aus Ereignissen.</summary>
    public int EreignisSchiPBonus => Ereignisse.Sum(e => e.SchicksalspunkteBonus);

    /// <summary>Summe aller LeP-Boni aus Ereignissen.</summary>
    public int EreignisLepBonus => Ereignisse.Sum(e => e.LepBonus);

    /// <summary>Summe aller AsP-Boni aus Ereignissen.</summary>
    public int EreignisAspBonus => Ereignisse.Sum(e => e.AspBonus);

    /// <summary>Summe aller KaP-Boni aus Ereignissen.</summary>
    public int EreignisKapBonus => Ereignisse.Sum(e => e.KapBonus);

    /// <summary>Summe aller SK-Boni aus Ereignissen.</summary>
    public int EreignisSkBonus => Ereignisse.Sum(e => e.SkBonus);

    /// <summary>Summe aller ZK-Boni aus Ereignissen.</summary>
    public int EreignisZkBonus => Ereignisse.Sum(e => e.ZkBonus);

    // Anzeigestrings für Ereignis-Boni in der Basiswerte-Tabelle (leer wenn 0)
    public string EreignisLepBonusAnzeige => EreignisLepBonus != 0 ? $"{EreignisLepBonus:+#;-#;0} Ere." : "";
    public string EreignisAspBonusAnzeige => EreignisAspBonus != 0 ? $"{EreignisAspBonus:+#;-#;0} Ere." : "";
    public string EreignisKapBonusAnzeige => EreignisKapBonus != 0 ? $"{EreignisKapBonus:+#;-#;0} Ere." : "";
    public string EreignisSkBonusAnzeige  => EreignisSkBonus  != 0 ? $"{EreignisSkBonus:+#;-#;0} Ere."  : "";
    public string EreignisZkBonusAnzeige  => EreignisZkBonus  != 0 ? $"{EreignisZkBonus:+#;-#;0} Ere."  : "";

    // Boni-Spalte: Summe aus Vorteilsboni + Ereignisboni (alle Quellen)
    public int LebensenergieBoniGesamt   => _sheet.LebensenergieVorteilsBonus + EreignisLepBonus;
    public int AstralergieBoniGesamt     => _sheet.AstralenergieVorteilsBonus + EreignisAspBonus;
    public int KarmaenergieBoniGesamt    => _sheet.KarmaenergieVorteilsBonus  + EreignisKapBonus;
    public int SeelenkraftBoniGesamt     => _sheet.SeelenkraftVorteilsBonus   + EreignisSkBonus;
    public int ZähigkeitBoniGesamt       => _sheet.ZähigkeitVorteilsBonus     + EreignisZkBonus;
    public int InitiativeBasisBoniGesamt => _sheet.InitiativeBasisVorteilsBonus;
    public int GeschwindigkeitBoniGesamt => _sheet.GeschwindigkeitVorteilsBonus;

    private void OnEreignisChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyEreignisBoni();
        RequestDelayedSave();
    }

    private void NotifyEreignisBoni()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeinEreignisse)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisAlterBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisSchiPBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisLepBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisAspBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisKapBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisSkBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisZkBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisLepBonusAnzeige)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisAspBonusAnzeige)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisKapBonusAnzeige)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisSkBonusAnzeige)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EreignisZkBonusAnzeige)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlterBerechnet)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lebensenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Astralenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Karmaenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seelenkraft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zähigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteGesamt)));
    }

    public void EreignisHinzufuegen(CharakterEreignis ereignis)
    {
        ereignis.PropertyChanged += OnEreignisChanged;
        Ereignisse.Add(ereignis);
        NotifyEreignisBoni();
        RequestDelayedSave();
    }

    public void EreignisEntfernen(CharakterEreignis ereignis)
    {
        ereignis.PropertyChanged -= OnEreignisChanged;
        Ereignisse.Remove(ereignis);
        NotifyEreignisBoni();
        RequestDelayedSave();
    }

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

        // Vorteilsboni
        _sheet.LebensenergieVorteilsBonus = s.LebensenergieVorteilsBonus;
        _sheet.AstralenergieVorteilsBonus = s.AstralenergieVorteilsBonus;
        _sheet.KarmaenergieVorteilsBonus = s.KarmaenergieVorteilsBonus;
        _sheet.SeelenkraftVorteilsBonus = s.SeelenkraftVorteilsBonus;
        _sheet.ZähigkeitVorteilsBonus = s.ZähigkeitVorteilsBonus;
        _sheet.InitiativeBasisVorteilsBonus = s.InitiativeBasisVorteilsBonus;
        _sheet.GeschwindigkeitVorteilsBonus = s.GeschwindigkeitVorteilsBonus;

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

        // Aktuelles Datum
        _sheet.AktuellesDatumStr = s.AktuellesDatumStr;

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

        // Ereignisse laden
        foreach (var e in Ereignisse.ToList())
        {
            e.PropertyChanged -= OnEreignisChanged;
        }
        Ereignisse.Clear();
        if (data.Ereignisse != null)
        {
            foreach (var e in data.Ereignisse)
            {
                e.PropertyChanged += OnEreignisChanged;
                Ereignisse.Add(e);
            }
        }
        NotifyEreignisBoni();
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
                // Speichere die Vorteilsboni
                LebensenergieVorteilsBonus = _sheet.LebensenergieVorteilsBonus,
                AstralenergieVorteilsBonus = _sheet.AstralenergieVorteilsBonus,
                KarmaenergieVorteilsBonus = _sheet.KarmaenergieVorteilsBonus,
                SeelenkraftVorteilsBonus = _sheet.SeelenkraftVorteilsBonus,
                ZähigkeitVorteilsBonus = _sheet.ZähigkeitVorteilsBonus,
                InitiativeBasisVorteilsBonus = _sheet.InitiativeBasisVorteilsBonus,
                GeschwindigkeitVorteilsBonus = _sheet.GeschwindigkeitVorteilsBonus,
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
                Kampftalente = _sheet.Kampftalente,
                AktuellesDatumStr = _sheet.AktuellesDatumStr
            },
            TalentValues = talentValues,
            Ereignisse = Ereignisse.ToList()
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lebensenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Astralenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralenergieFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Karmaenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seelenkraft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zähigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisBerechnet)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geschwindigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AktuelleSpezies)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeziesInfoText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlterBerechnet)));
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralenergieVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitVorteilsBonus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteVerfuegbar)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteAusgegeben)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteVerfuegbar)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Vorteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Nachteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Talente)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kampftalente)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AktuellesDatumStr)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlterBerechnet)));
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
        set
        {
            SetProperty(_sheet.Geburtstag, value, v => _sheet.Geburtstag = v);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlterBerechnet)));
        }
    }

    public string Alter
    {
        get => _sheet.Alter;
        set => SetProperty(_sheet.Alter, value, v => _sheet.Alter = v);
    }

    /// <summary>Aktuelles aventurisches In-Game-Datum.</summary>
    public string AktuellesDatumStr
    {
        get => _sheet.AktuellesDatumStr;
        set
        {
            SetProperty(_sheet.AktuellesDatumStr, value, v => _sheet.AktuellesDatumStr = v);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlterBerechnet)));
        }
    }

    /// <summary>
    /// Berechnetes Alter aus Geburtstag + aktuellem Datum + Ereignis-Alters-Boni.
    /// Gibt einen lesbaren String zurück, z.B. "32 Jahre".
    /// </summary>
    public string AlterBerechnet
    {
        get
        {
            if (!BoronDatum.TryParse(_sheet.Geburtstag, out var geburt)) return "—";
            if (!BoronDatum.TryParse(_sheet.AktuellesDatumStr, out var heute)) return "—";

            int jahre = heute.Jahr - geburt.Jahr;
            if (heute.Monat < geburt.Monat || (heute.Monat == geburt.Monat && heute.Tag < geburt.Tag))
                jahre--;

            int gesamt = Math.Max(0, jahre + EreignisAlterBonus);
            return $"{gesamt} Jahre";
        }
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

    /// <summary>LeP-Basiswert = 2×KO + SpeziesLeP (ohne Zugekauft/Boni)</summary>
    public int LebensenergieBasis
    {
        get
        {
            int basis = AktuelleSpezies?.LePBasis ?? 5;
            return 2 * _sheet.Konstitution + basis;
        }
    }

    /// <summary>LeP gesamt = Basis + Zugekauft + Vorteilsboni + EreignisBoni</summary>
    public int Lebensenergie => LebensenergieBasis + _sheet.LebensenergieZugekauft + _sheet.LebensenergieVorteilsBonus + EreignisLepBonus;

    public string LebensenergieFormel
    {
        get
        {
            int basis = AktuelleSpezies?.LePBasis ?? 5;
            return $"2\u00d7KO({_sheet.Konstitution}) + {basis}";
        }
    }

    /// <summary>AsP gesamt = Zugekauft + Vorteilsboni + EreignisBoni</summary>
    public int Astralenergie => _sheet.AstralenergieZugekauft + _sheet.AstralenergieVorteilsBonus + EreignisAspBonus;

    public string AstralenergieFormel => "kein Basiswert \u2013 nur zugekauft";

    /// <summary>KaP gesamt = Zugekauft + Vorteilsboni + EreignisBoni</summary>
    public int Karmaenergie => _sheet.KarmaenergieZugekauft + _sheet.KarmaenergieVorteilsBonus + EreignisKapBonus;

    public string KarmaenergieFormel => "kein Basiswert \u2013 nur zugekauft";

    /// <summary>SK-Basiswert = ⌈(MU+KL+IN)/6⌉ + SpeziesMod (ohne Zugekauft/Boni)</summary>
    public int SeelenkraftBasis
    {
        get
        {
            int mod = AktuelleSpezies?.SeelenkraftMod ?? -5;
            return (int)Math.Ceiling((_sheet.Mut + _sheet.Klugheit + _sheet.Intuition) / 6.0) + mod;
        }
    }

    /// <summary>SK gesamt = Basis + Vorteilsboni + EreignisBoni (kein Zukauf möglich)</summary>
    public int Seelenkraft => SeelenkraftBasis + _sheet.SeelenkraftVorteilsBonus + EreignisSkBonus;

    public string SeelenkraftFormel
    {
        get
        {
            int mod = AktuelleSpezies?.SeelenkraftMod ?? -5;
            return $"\u2308(MU+KL+IN)/6\u2309 = \u2308({_sheet.Mut}+{_sheet.Klugheit}+{_sheet.Intuition})/6\u2309 {mod:+#;-#;0}";
        }
    }

    /// <summary>ZK-Basiswert = ⌈(KO+KO+KK)/6⌉ + SpeziesMod (ohne Zugekauft/Boni)</summary>
    public int ZähigkeitBasis
    {
        get
        {
            int mod = AktuelleSpezies?.ZähigkeitMod ?? -5;
            return (int)Math.Ceiling((_sheet.Konstitution + _sheet.Konstitution + _sheet.Körperkraft) / 6.0) + mod;
        }
    }

    /// <summary>ZK gesamt = Basis + Vorteilsboni + EreignisBoni (kein Zukauf möglich)</summary>
    public int Zähigkeit => ZähigkeitBasis + _sheet.ZähigkeitVorteilsBonus + EreignisZkBonus;

    public string ZähigkeitFormel
    {
        get
        {
            int mod = AktuelleSpezies?.ZähigkeitMod ?? -5;
            return $"\u2308(KO+KO+KK)/6\u2309 = \u2308({_sheet.Konstitution}+{_sheet.Konstitution}+{_sheet.Körperkraft})/6\u2309 {mod:+#;-#;0}";
        }
    }

    /// <summary>INI-Basiswert = ⌈(MU+GE)/2⌉ (ohne Vorteilsboni)</summary>
    public int InitiativeBasisBerechnet => (int)Math.Ceiling((_sheet.Mut + _sheet.Gewandtheit) / 2.0);

    /// <summary>INI gesamt = Berechnet + Vorteilsboni</summary>
    public int InitiativeBasis => InitiativeBasisBerechnet + _sheet.InitiativeBasisVorteilsBonus;

    public string InitiativeBasisFormel => $"\u2308(MU+GE)/2\u2309 = \u2308({_sheet.Mut}+{_sheet.Gewandtheit})/2\u2309";

    /// <summary>GS-Basiswert = SpeziesGS</summary>
    public int GeschwindigkeitBasis => AktuelleSpezies?.Geschwindigkeit ?? 8;

    /// <summary>GS gesamt = SpeziesGS + Vorteilsboni</summary>
    public int Geschwindigkeit => GeschwindigkeitBasis + _sheet.GeschwindigkeitVorteilsBonus;

    public string GeschwindigkeitFormel => $"Spezieswert ({AktuelleSpezies?.Name ?? "Mensch"}: {GeschwindigkeitBasis})";


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

    // --- Vorteilsboni (Vorteile / Sonderfertigkeiten etc.) ---

    public int LebensenergieVorteilsBonus
    {
        get => _sheet.LebensenergieVorteilsBonus;
        set
        {
            if (_sheet.LebensenergieVorteilsBonus == value) return;
            _sheet.LebensenergieVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lebensenergie)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieBoniGesamt)));
            RequestDelayedSave();
        }
    }

    public int AstralenergieVorteilsBonus
    {
        get => _sheet.AstralenergieVorteilsBonus;
        set
        {
            if (_sheet.AstralenergieVorteilsBonus == value) return;
            _sheet.AstralenergieVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralenergieVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Astralenergie)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralergieBoniGesamt)));
            RequestDelayedSave();
        }
    }

    public int KarmaenergieVorteilsBonus
    {
        get => _sheet.KarmaenergieVorteilsBonus;
        set
        {
            if (_sheet.KarmaenergieVorteilsBonus == value) return;
            _sheet.KarmaenergieVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Karmaenergie)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieBoniGesamt)));
            RequestDelayedSave();
        }
    }

    public int SeelenkraftVorteilsBonus
    {
        get => _sheet.SeelenkraftVorteilsBonus;
        set
        {
            if (_sheet.SeelenkraftVorteilsBonus == value) return;
            _sheet.SeelenkraftVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seelenkraft)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftBoniGesamt)));
            RequestDelayedSave();
        }
    }

    public int ZähigkeitVorteilsBonus
    {
        get => _sheet.ZähigkeitVorteilsBonus;
        set
        {
            if (_sheet.ZähigkeitVorteilsBonus == value) return;
            _sheet.ZähigkeitVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zähigkeit)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitBoniGesamt)));
            RequestDelayedSave();
        }
    }

    public int InitiativeBasisVorteilsBonus
    {
        get => _sheet.InitiativeBasisVorteilsBonus;
        set
        {
            if (_sheet.InitiativeBasisVorteilsBonus == value) return;
            _sheet.InitiativeBasisVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasis)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisBoniGesamt)));
            RequestDelayedSave();
        }
    }

    public int GeschwindigkeitVorteilsBonus
    {
        get => _sheet.GeschwindigkeitVorteilsBonus;
        set
        {
            if (_sheet.GeschwindigkeitVorteilsBonus == value) return;
            _sheet.GeschwindigkeitVorteilsBonus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitVorteilsBonus)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geschwindigkeit)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitBoniGesamt)));
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
        get => _sheet.SchicksalspunkteGesamt + EreignisSchiPBonus;
        set => SetProperty(_sheet.SchicksalspunkteGesamt, value - EreignisSchiPBonus, v => _sheet.SchicksalspunkteGesamt = v);
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
