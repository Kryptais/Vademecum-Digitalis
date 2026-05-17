using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Models.RuleEngine;
using VademecumDigitalis.Services;
using VademecumDigitalis.Services.RuleEngine;

namespace VademecumDigitalis.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly CharacterSheet _sheet = new();
    private readonly PersistenceService _persistence;
    private readonly TalentModifierService? _talentModifierService;
    private readonly VorteilNachteilService? _vorteilNachteilService;
    private readonly TalentCatalogService? _talentCatalogService;
    private readonly EffectResolver? _effectResolver;
    private readonly CharacterSaveService? _characterSaveService;
    private CancellationTokenSource? _saveCts;

    public MainPageViewModel() : this(new PersistenceService(), null)
    {
    }

    public MainPageViewModel(
        PersistenceService persistence,
        TalentModifierService? talentModifierService = null,
        VorteilNachteilService? vorteilNachteilService = null,
        EffectResolver? effectResolver = null,
        TalentCatalogService? talentCatalogService = null,
        CharacterSaveService? characterSaveService = null)
    {
        _persistence = persistence;
        _talentModifierService = talentModifierService;
        _vorteilNachteilService = vorteilNachteilService;
        _talentCatalogService = talentCatalogService;
        _effectResolver = effectResolver;
        _characterSaveService = characterSaveService;
        TalentGruppen = BuildTalentGruppen();
        Kampftechniken = BuildKampftechniken();
        KampfStatiRows = BuildKampfStatiRows();
        SubscribeToTalentChanges();
        SubscribeToKampftechnikenChanges();
        SubscribeToStatusChanges();
        ToggleExpandCommand = new Command<TalentGroup>(g => g.IsExpanded = !g.IsExpanded);
        _ = LoadRuleCatalogAsync();
    }

    /// <summary>Command zum Auf-/Zuklappen einer Talentgruppe.</summary>
    public Command<TalentGroup> ToggleExpandCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TalentGroup> TalentGruppen { get; }

    // --- Kampftechniken ---

    public IReadOnlyList<KampftechnikRow> Kampftechniken { get; }

    // --- Kampf-Status ---

    public IReadOnlyList<StatusRow> KampfStatiRows { get; }

    /// <summary>Ausweichen = ⌈GE/2⌉</summary>
    public int Ausweichen => (int)Math.Ceiling(_sheet.Gewandtheit / 2.0);

    /// <summary>Aktuelle Lebensenergie (editierbar im Kampf-Tab).</summary>
    public int AktuelleLebensenergie
    {
        get
        {
            if (_sheet.AktuelleLebensenergie < 0)
                return Lebensenergie;
            return _sheet.AktuelleLebensenergie;
        }
        set
        {
            if (_sheet.AktuelleLebensenergie == value) return;
            _sheet.AktuelleLebensenergie = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AktuelleLebensenergie)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebenVerloren)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Schmerzstufen)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchmerzstufenDetails)));
            RequestDelayedSave();
        }
    }

    /// <summary>Verlorene Lebenspunkte.</summary>
    public int LebenVerloren => Math.Max(0, Lebensenergie - AktuelleLebensenergie);

    /// <summary>Aktuelle Schmerzstufen basierend auf verlorener Lebensenergie.</summary>
    public int Schmerzstufen
    {
        get
        {
            int max = Lebensenergie;
            int verloren = LebenVerloren;
            if (max <= 0) return 0;
            int stufen = 0;
            if (verloren >= (int)Math.Ceiling(max * 0.25)) stufen++;
            if (verloren >= (int)Math.Ceiling(max * 0.50)) stufen++;
            if (verloren >= (int)Math.Ceiling(max * 0.75)) stufen++;
            if (AktuelleLebensenergie <= 5 ){ stufen++; };
            if (AktuelleLebensenergie <= 0) stufen = -1; // Sterbend
            return stufen;
        }
    }

    /// <summary>Detail-Text für Schmerzschwellen mit konkreten LeP-Werten.</summary>
    public string SchmerzstufenDetails
    {
        get
        {
            int max = Lebensenergie;
            if (max <= 0) return string.Empty;
            int schwelle1 = max - (int)Math.Ceiling(max * 0.25);
            int schwelle2 = max - (int)Math.Ceiling(max * 0.50);
            int schwelle3 = max - (int)Math.Ceiling(max * 0.75);
            int schwelle4 = 5;
            int aktuell = AktuelleLebensenergie;

            string Marker(int schwelle) => aktuell <= schwelle ? " ◄" : "";

            return $"1. Schmerz bei ≤{schwelle1} LeP{Marker(schwelle1)}\n" +
                   $"2. Schmerz bei ≤{schwelle2} LeP{Marker(schwelle2)}\n" +
                   $"3. Schmerz bei ≤{schwelle3} LeP{Marker(schwelle3)}\n" +
                   $"4. Schmerz bei ≤{schwelle4} LeP{Marker(schwelle4)}\n" +
                   $"Sterbend bei ≤5 LeP{(aktuell <= 5 ? " ◄" : "")}";
        }
    }

    // --- Ereignisse ---

    public ObservableCollection<CharakterEreignis> Ereignisse { get; } = [];

    /// <summary>True wenn keine Ereignisse vorhanden (für Empty-Label-Binding).</summary>
    public bool KeinEreignisse => Ereignisse.Count == 0;

    // --- Vorteile / Nachteile ---

    public ObservableCollection<CharakterVorteilNachteilEintrag> VorteilNachteilEintraege { get; } = [];

    /// <summary>True wenn keine Vorteile/Nachteile vorhanden.</summary>
    public bool KeineVorteileNachteile => VorteilNachteilEintraege.Count == 0;

    public void VorteilNachteilHinzufuegen(CharakterVorteilNachteilEintrag eintrag)
    {
        eintrag.PropertyChanged += OnVorteilNachteilChanged;
        VorteilNachteilEintraege.Add(eintrag);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineVorteileNachteile)));
        RecalculateTalentProben();
        NotifyRuleEffectValuesChanged();
        RequestDelayedSave();
    }

    public void VorteilNachteilEntfernen(CharakterVorteilNachteilEintrag eintrag)
    {
        eintrag.PropertyChanged -= OnVorteilNachteilChanged;
        VorteilNachteilEintraege.Remove(eintrag);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineVorteileNachteile)));
        RecalculateTalentProben();
        NotifyRuleEffectValuesChanged();
        RequestDelayedSave();
    }

    private void OnVorteilNachteilChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateTalentProben();
        NotifyRuleEffectValuesChanged();
        RequestDelayedSave();
    }

    // --- Sonderfertigkeiten ---

    public ObservableCollection<CharakterSonderfertigkeitEintrag> SonderfertigkeitEintraege { get; } = [];

    /// <summary>True wenn keine Sonderfertigkeiten vorhanden.</summary>
    public bool KeineSonderfertigkeiten => SonderfertigkeitEintraege.Count == 0;

    public void SonderfertigkeitHinzufuegen(CharakterSonderfertigkeitEintrag eintrag)
    {
        eintrag.PropertyChanged += OnSonderfertigkeitChanged;
        SonderfertigkeitEintraege.Add(eintrag);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineSonderfertigkeiten)));
        RequestDelayedSave();
    }

    public void SonderfertigkeitEntfernen(CharakterSonderfertigkeitEintrag eintrag)
    {
        eintrag.PropertyChanged -= OnSonderfertigkeitChanged;
        SonderfertigkeitEintraege.Remove(eintrag);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineSonderfertigkeiten)));
        RequestDelayedSave();
    }

    private void OnSonderfertigkeitChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateTalentProben();
        RequestDelayedSave();
    }

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

    // Boni-Spalte: alle additiven Quellen (manueller Vorteilsbonus + Ereignisse + RuleEffects).
    // Berechnet als Gesamtwert minus reine Basis, damit RuleEffect-Boni (z. B. „Hohe Lebensenergie")
    // automatisch sichtbar werden.
    public int LebensenergieBoniGesamt   => Lebensenergie   - LebensenergieBasis - _sheet.LebensenergieZugekauft;
    public int AstralergieBoniGesamt     => Astralenergie   - _sheet.AstralenergieZugekauft;
    public int KarmaenergieBoniGesamt    => Karmaenergie    - _sheet.KarmaenergieZugekauft;
    public int SeelenkraftBoniGesamt     => Seelenkraft     - SeelenkraftBasis;
    public int ZähigkeitBoniGesamt       => Zähigkeit       - ZähigkeitBasis;
    public int InitiativeBasisBoniGesamt => InitiativeBasis - InitiativeBasisBerechnet;
    public int GeschwindigkeitBoniGesamt => Geschwindigkeit - GeschwindigkeitBasis;

    public string GeschwindigkeitAuditText => BuildGeschwindigkeitAuditText();

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
        ApplyCharacterSheetData(data);
    }

    private void ApplyCharacterSheetData(CharacterSheetData data)
    {
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

        // Kampftechnik-Werte (KTW + Boni) laden
        if (data.KampftechnikValues != null)
        {
            var ktLookup = data.KampftechnikValues.ToDictionary(kt => kt.Kampftechnik, kt => kt);
            foreach (var kt in Kampftechniken)
            {
                if (ktLookup.TryGetValue(kt.Kampftechnik, out var saved))
                {
                    kt.Ktw = saved.Ktw;
                    kt.Boni = saved.Boni;
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

        // Vorteile/Nachteile laden
        foreach (var vn in VorteilNachteilEintraege.ToList())
        {
            vn.PropertyChanged -= OnVorteilNachteilChanged;
        }
        VorteilNachteilEintraege.Clear();
        if (data.VorteilNachteilListe != null)
        {
            foreach (var vn in data.VorteilNachteilListe)
            {
                vn.PropertyChanged += OnVorteilNachteilChanged;
                VorteilNachteilEintraege.Add(vn);
            }
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineVorteileNachteile)));
        // Effekte der geladenen Vorteile/Nachteile auf abgeleitete Werte (GS, LeP, …) anwenden.
        NotifyRuleEffectValuesChanged();

        // Sonderfertigkeiten laden
        foreach (var sf in SonderfertigkeitEintraege.ToList())
        {
            sf.PropertyChanged -= OnSonderfertigkeitChanged;
        }
        SonderfertigkeitEintraege.Clear();
        if (data.SonderfertigkeitListe != null)
        {
            foreach (var sf in data.SonderfertigkeitListe)
            {
                sf.PropertyChanged += OnSonderfertigkeitChanged;
                SonderfertigkeitEintraege.Add(sf);
            }
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineSonderfertigkeiten)));

        RecalculateTalentProben();
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

        var kampftechnikValues = new List<KampftechnikSaveEntry>();
        foreach (var kt in Kampftechniken)
        {
            if (!string.IsNullOrEmpty(kt.Ktw) || kt.Boni != 0)
            {
                kampftechnikValues.Add(new KampftechnikSaveEntry
                {
                    Kampftechnik = kt.Kampftechnik,
                    Ktw = kt.Ktw,
                    Boni = kt.Boni
                });
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
                AktuellesDatumStr = _sheet.AktuellesDatumStr,
                KampfStati = _sheet.KampfStati
            },
            TalentValues = talentValues,
            KampftechnikValues = kampftechnikValues,
            Ereignisse = Ereignisse.ToList(),
            VorteilNachteilListe = VorteilNachteilEintraege.ToList(),
            SonderfertigkeitListe = SonderfertigkeitEintraege.ToList()
        };
    }

    private async Task SaveDataAsync()
    {
        try
        {
            var data = BuildSaveData();
            await _persistence.SaveCharacterSheetAsync(data);

            // Zusätzlich in den benutzerseitigen Charakter-Slot schreiben, damit das
            // Dashboard die Änderungen sieht. Dateiname = aktiver Slot oder
            // (bei neuem Charakter) abgeleitet aus dem Namen.
            if (_characterSaveService != null)
            {
                string? filename = ActiveCharacterFileName;
                if (string.IsNullOrWhiteSpace(filename))
                {
                    filename = SanitizeFileName(_sheet.Name);
                    if (!string.IsNullOrWhiteSpace(filename))
                        ActiveCharacterFileName = filename;
                }

                if (!string.IsNullOrWhiteSpace(filename))
                {
                    await _characterSaveService.SaveCharacterAsync(data, filename);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving character sheet: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }

    // --- Dashboard-Schnittstelle ---

    /// <summary>Dateiname des aktuell geladenen Charakters (ohne Extension). Leer = unbenannter Charakter.</summary>
    public string ActiveCharacterFileName { get; private set; } = string.Empty;

    /// <summary>Gibt den vollständigen Savestand zurück (für Export / Dashboard-Speicherung).</summary>
    public CharacterSheetData ToCharacterSheetData() => BuildSaveData();

    /// <summary>Lädt einen Charakter aus einem <see cref="CharacterSheetData"/>-Objekt in die aktive Session.</summary>
    public async Task LoadFromCharacterSheetDataAsync(CharacterSheetData data, string fileName)
    {
        ActiveCharacterFileName = fileName;
        await Task.Run(() => ApplyCharacterSheetData(data));
    }

    /// <summary>Setzt alle Felder auf Standardwerte zurück (neuer Charakter).</summary>
    public void ResetToNewCharacter()
    {
        ActiveCharacterFileName = string.Empty;

        _sheet.Name = string.Empty;
        _sheet.Spieler = string.Empty;
        _sheet.Spezies = string.Empty;
        _sheet.Kultur = string.Empty;
        _sheet.Profession = string.Empty;
        _sheet.Geschlecht = string.Empty;
        _sheet.Geburtstag = string.Empty;
        _sheet.Alter = string.Empty;
        _sheet.Größe = string.Empty;
        _sheet.Gewicht = string.Empty;
        _sheet.Haarfarbe = string.Empty;
        _sheet.Augenfarbe = string.Empty;
        _sheet.Sozialstatus = string.Empty;
        _sheet.Mut = 8; _sheet.Klugheit = 8; _sheet.Intuition = 8; _sheet.Charisma = 8;
        _sheet.Fingerfertigkeit = 8; _sheet.Gewandtheit = 8; _sheet.Konstitution = 8; _sheet.Körperkraft = 8;
        _sheet.LebensenergieZugekauft = 0; _sheet.AstralenergieZugekauft = 0;
        _sheet.KarmaenergieZugekauft = 0; _sheet.SeelenkraftZugekauft = 0; _sheet.ZähigkeitZugekauft = 0;
        _sheet.LebensenergieVorteilsBonus = 0; _sheet.AstralenergieVorteilsBonus = 0;
        _sheet.KarmaenergieVorteilsBonus = 0; _sheet.SeelenkraftVorteilsBonus = 0;
        _sheet.ZähigkeitVorteilsBonus = 0; _sheet.InitiativeBasisVorteilsBonus = 0; _sheet.GeschwindigkeitVorteilsBonus = 0;
        _sheet.AbenteuerpunkteGesamt = 1100; _sheet.AbenteuerpunkteVerfuegbar = 0; _sheet.AbenteuerpunkteAusgegeben = 1100;
        _sheet.SchicksalspunkteGesamt = 3; _sheet.SchicksalspunkteVerfuegbar = 3;
        _sheet.Vorteile = string.Empty; _sheet.Nachteile = string.Empty;
        _sheet.Talente = string.Empty; _sheet.Kampftalente = string.Empty;
        _sheet.AktuellesDatumStr = string.Empty;
        _sheet.AktuelleLebensenergie = -1;
        _sheet.KampfStati.Clear();

        foreach (var group in TalentGruppen)
            foreach (var row in group.Eintraege)
            { row.Fw = string.Empty; row.Anmerkung = string.Empty; }

        foreach (var kt in Kampftechniken)
        { kt.Ktw = string.Empty; kt.Boni = 0; }

        foreach (var vn in VorteilNachteilEintraege.ToList())
            vn.PropertyChanged -= OnVorteilNachteilChanged;
        VorteilNachteilEintraege.Clear();

        foreach (var sf in SonderfertigkeitEintraege.ToList())
            sf.PropertyChanged -= OnSonderfertigkeitChanged;
        SonderfertigkeitEintraege.Clear();

        foreach (var e in Ereignisse.ToList())
            e.PropertyChanged -= OnEreignisChanged;
        Ereignisse.Clear();

        NotifyAllProperties();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineVorteileNachteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeineSonderfertigkeiten)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeinEreignisse)));
        NotifyEreignisBoni();
        RecalculateTalentProben();
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

    /// <summary>Abonniere PropertyChanged für Kampftechniken (KTW, Boni) um Parade neu zu berechnen und zu speichern.</summary>
    private void SubscribeToKampftechnikenChanges()
    {
        foreach (var kt in Kampftechniken)
        {
            kt.PropertyChanged += (sender, e) =>
            {
                // Bei KTW-Änderung: Parade neuberechnen
                if (e.PropertyName == nameof(KampftechnikRow.Ktw) && sender is KampftechnikRow row)
                {
                    RecalculateKampftechnikenParade(row);
                }
                RequestDelayedSave();
            };
        }
    }

    /// <summary>Berechnet Parade für eine einzelne Kampftechnik neu (effizienter als volle Neuberechnung).</summary>
    private void RecalculateKampftechnikenParade(KampftechnikRow kt)
    {
        if (kt.IstFernkampf)
        {
            kt.ParadeBasis = 0;
            kt.ParadeBoniEffekte = 0;
            return;
        }

        var attrs = BuildAttributeDictionary();
        int best = 0;
        foreach (var le in kt.Leiteigenschaft.Split('/'))
        {
            if (attrs.TryGetValue(le.Trim(), out var val))
                best = Math.Max(best, val);
        }

        if (int.TryParse(kt.Ktw, out var ktw))
        {
            int paBasis = Math.Max(0, (int)Math.Ceiling(ktw / 2.0) + (int)Math.Floor((best - 8) / 3.0));
            int statusMod = GetStatusModifikatorForPA();
            int paResolved = (int)ResolveDerivedValue($"combat.{kt.Kampftechnik}.PA", paBasis).FinalValue;
            int effektDelta = paResolved - paBasis;
            kt.ParadeBasis = paBasis;
            kt.ParadeBoniEffekte = effektDelta + statusMod;
        }
        else
        {
            kt.ParadeBasis = 0;
            kt.ParadeBoniEffekte = 0;
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitAuditText)));

        RecalculateTalentProben();
        RecalculateKampftechniken();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ausweichen)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AktuelleLebensenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebenVerloren)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Schmerzstufen)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchmerzstufenDetails)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Wundschwelle)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WundschwelleFormel)));
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitAuditText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteVerfuegbar)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AbenteuerpunkteAusgegeben)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteVerfuegbar)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Vorteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Nachteile)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Talente)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Kampftalente)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AktuelleLebensenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebenVerloren)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Schmerzstufen)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchmerzstufenDetails)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ausweichen)));
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
    public int Lebensenergie => (int)ResolveDerivedValue(
        "derived.LEP",
        LebensenergieBasis + _sheet.LebensenergieZugekauft + _sheet.LebensenergieVorteilsBonus + EreignisLepBonus).FinalValue;

    public string LebensenergieFormel
    {
        get
        {
            int basis = AktuelleSpezies?.LePBasis ?? 5;
            return $"2\u00d7KO({_sheet.Konstitution}) + {basis}";
        }
    }

    /// <summary>AsP gesamt = Zugekauft + Vorteilsboni + EreignisBoni + RuleEffects (derived.ASP)</summary>
    public int Astralenergie => (int)ResolveDerivedValue(
        "derived.ASP",
        _sheet.AstralenergieZugekauft + _sheet.AstralenergieVorteilsBonus + EreignisAspBonus).FinalValue;

    public string AstralenergieFormel => "kein Basiswert \u2013 nur zugekauft";

    /// <summary>KaP gesamt = Zugekauft + Vorteilsboni + EreignisBoni + RuleEffects (derived.KAP)</summary>
    public int Karmaenergie => (int)ResolveDerivedValue(
        "derived.KAP",
        _sheet.KarmaenergieZugekauft + _sheet.KarmaenergieVorteilsBonus + EreignisKapBonus).FinalValue;

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
    public int Seelenkraft => (int)ResolveDerivedValue(
        "derived.SK",
        SeelenkraftBasis + _sheet.SeelenkraftVorteilsBonus + EreignisSkBonus).FinalValue;

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
    public int Zähigkeit => (int)ResolveDerivedValue(
        "derived.ZK",
        ZähigkeitBasis + _sheet.ZähigkeitVorteilsBonus + EreignisZkBonus).FinalValue;

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

    /// <summary>INI gesamt = Berechnet + Vorteilsboni + RuleEffects (derived.INI)</summary>
    public int InitiativeBasis => (int)ResolveDerivedValue(
        "derived.INI",
        InitiativeBasisBerechnet + _sheet.InitiativeBasisVorteilsBonus).FinalValue;

    public string InitiativeBasisFormel => $"\u2308(MU+GE)/2\u2309 = \u2308({_sheet.Mut}+{_sheet.Gewandtheit})/2\u2309";

    /// <summary>GS-Basiswert = SpeziesGS</summary>
    public int GeschwindigkeitBasis => AktuelleSpezies?.Geschwindigkeit ?? 8;

    /// <summary>GS gesamt = SpeziesGS + manuelle Boni + aktive RuleEffects</summary>
    public int Geschwindigkeit => (int)ResolveDerivedValue(
        "derived.GS",
        GeschwindigkeitBasis + _sheet.GeschwindigkeitVorteilsBonus).FinalValue;

    public string GeschwindigkeitFormel =>
        $"Spezieswert ({AktuelleSpezies?.Name ?? "Mensch"}: {GeschwindigkeitBasis})"
        + (_sheet.GeschwindigkeitVorteilsBonus != 0
            ? $" + manuell {_sheet.GeschwindigkeitVorteilsBonus:+#;-#;0}"
            : "");

    /// <summary>Wundschwelle = ⌈KO/2⌉</summary>
    public int Wundschwelle => (int)Math.Ceiling(_sheet.Konstitution / 2.0);

    /// <summary>Formeldarstellung der Wundschwelle.</summary>
    public string WundschwelleFormel => $"⌈KO/2⌉ = ⌈{_sheet.Konstitution}/2⌉";

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
            NotifyRuleEffectValuesChanged();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitBoniGesamt)));
            RequestDelayedSave();
        }
    }

    private async Task LoadRuleCatalogAsync()
    {
        var tasks = new List<Task>();
        if (_vorteilNachteilService is not null)
            tasks.Add(_vorteilNachteilService.LoadCatalogAsync());
        if (_talentCatalogService is not null)
            tasks.Add(_talentCatalogService.EnsureLoadedAsync());

        if (tasks.Count == 0) return;
        await Task.WhenAll(tasks);
        NotifyRuleEffectValuesChanged();
        RecalculateTalentProben();
    }

    private RuleEffectResolution ResolveDerivedValue(string target, decimal baseValue)
    {
        if (_vorteilNachteilService is null || _effectResolver is null)
        {
            return new RuleEffectResolution
            {
                Target = target,
                BaseValue = baseValue,
                FinalValue = baseValue
            };
        }

        var sources = _vorteilNachteilService.CreateEffectSources(VorteilNachteilEintraege);
        return _effectResolver.Resolve(target, baseValue, sources);
    }

    private string BuildGeschwindigkeitAuditText()
    {
        var resolution = ResolveDerivedValue(
            "derived.GS",
            GeschwindigkeitBasis + _sheet.GeschwindigkeitVorteilsBonus);
        if (resolution.AuditEntries.Count == 0)
            return GeschwindigkeitFormel;

        var audit = string.Join(
            " | ",
            resolution.AuditEntries.Select(entry =>
                $"{entry.SourceName}: {entry.Before:0.##} -> {entry.After:0.##} ({entry.AppliedValue:+0.##;-0.##;0})"));

        return $"{GeschwindigkeitFormel} | {audit}";
    }

    private void NotifyRuleEffectValuesChanged()
    {
        // Gesamtwerte
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lebensenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Astralenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Karmaenergie)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seelenkraft)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zähigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasis)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchicksalspunkteGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Geschwindigkeit)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitFormel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitAuditText)));
        // Boni-Spalten (enthalten jetzt auch die RuleEffect-Deltas)
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LebensenergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AstralergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KarmaenergieBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeelenkraftBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZähigkeitBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitiativeBasisBoniGesamt)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeschwindigkeitBoniGesamt)));
        // Kampftechniken-AT/PA hängen jetzt auch von RuleEffects ab (combat.X.AT / .PA).
        RecalculateKampftechniken();
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
        get => (int)ResolveDerivedValue(
            "derived.SchiP",
            _sheet.SchicksalspunkteGesamt + EreignisSchiPBonus).FinalValue;
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
        var gruppen = new[]
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

        // Gruppe auf jede TalentRow setzen
        foreach (var gruppe in gruppen)
        {
            foreach (var row in gruppe.Eintraege)
            {
                row.Gruppe = gruppe.Gruppe;
            }
        }

        return gruppen;
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

    /// <summary>Berechnet AT/FK-Basiswerte und Parade für alle Kampftechniken neu.</summary>
    private void RecalculateKampftechniken()
    {
        var attrs = BuildAttributeDictionary();
        int statusModAT = GetStatusModifikatorForAT();
        int statusModPA = GetStatusModifikatorForPA();

        foreach (var kt in Kampftechniken)
        {
            // AT/FK-Basis: ⌊(MU-8)/3⌋ bzw. ⌊(FF-8)/3⌋ (rein, ohne Status, ohne Effekte)
            int atBasis = kt.IstFernkampf
                ? Math.Max(0, (int)Math.Floor((attrs.GetValueOrDefault("FF", 8) - 8) / 3.0))
                : Math.Max(0, (int)Math.Floor((attrs.GetValueOrDefault("MU", 8) - 8) / 3.0));
            kt.AtFkBasis = atBasis;

            // Effekt-Boni über RuleEffectResolver bestimmen (z. B. Vorteil "Zweihandhiebwaffen +2 AT")
            int atResolved = (int)ResolveDerivedValue($"combat.{kt.Kampftechnik}.AT", atBasis).FinalValue;
            int atEffektDelta = atResolved - atBasis;
            kt.BoniEffekte = atEffektDelta + statusModAT;

            // Parade nur für Nahkampf
            int best = 0;
            foreach (var le in kt.Leiteigenschaft.Split('/'))
            {
                if (attrs.TryGetValue(le.Trim(), out var val))
                    best = Math.Max(best, val);
            }
            if (!kt.IstFernkampf && int.TryParse(kt.Ktw, out var ktw))
            {
                int paBasis = Math.Max(0, (int)Math.Ceiling(ktw / 2.0) + (int)Math.Floor((best - 8) / 3.0));
                int paResolved = (int)ResolveDerivedValue($"combat.{kt.Kampftechnik}.PA", paBasis).FinalValue;
                int paEffektDelta = paResolved - paBasis;
                kt.ParadeBasis = paBasis;
                kt.ParadeBoniEffekte = paEffektDelta + statusModPA;
            }
            else
            {
                kt.ParadeBasis = 0;
                kt.ParadeBoniEffekte = 0;
            }
        }
    }

    /// <summary>Berechnet Gesamt-Status-Modifikator für AT/FK (Betäubung, Furcht, Schmerz, Paralyse).</summary>
    private int GetStatusModifikatorForAT()
    {
        int mod = 0;
        foreach (var status in KampfStatiRows)
        {
            if (status.Stufe == 4) continue; // Stufe 4 = handlungsunfähig (kein Malus, da keine Handlung möglich)

            switch (status.StatusName)
            {
                case "Betäubung":
                case "Furcht":
                case "Schmerz":
                    // -1/-2/-3 auf alle Proben inkl. AT/FK
                    mod -= Math.Min(status.Stufe, 3);
                    break;
                case "Paralyse":
                    // -1/-2/-3 auf GE/FF-basierte Proben inkl. AT
                    mod -= Math.Min(status.Stufe, 3);
                    break;
            }
        }
        return mod;
    }

    /// <summary>Berechnet Gesamt-Status-Modifikator für PA (Betäubung, Furcht, Schmerz, Paralyse).</summary>
    private int GetStatusModifikatorForPA()
    {
        int mod = 0;
        foreach (var status in KampfStatiRows)
        {
            if (status.Stufe == 4) continue; // Stufe 4 = handlungsunfähig

            switch (status.StatusName)
            {
                case "Betäubung":
                case "Furcht":
                case "Schmerz":
                    // -1/-2/-3 auf alle Proben inkl. PA
                    mod -= Math.Min(status.Stufe, 3);
                    break;
                case "Paralyse":
                    // -1/-2/-3 auf GE/FF-basierte Proben inkl. PA
                    mod -= Math.Min(status.Stufe, 3);
                    break;
            }
        }
        return mod;
    }

    private static IReadOnlyList<KampftechnikRow> BuildKampftechniken()
    {
        return new KampftechnikRow[]
        {
            new() { Kampftechnik = "Armbrüste", Leiteigenschaft = "FF", Steigerungsfaktor = "B", IstFernkampf = true },
            new() { Kampftechnik = "Bögen", Leiteigenschaft = "FF", Steigerungsfaktor = "C", IstFernkampf = true },
            new() { Kampftechnik = "Dolche", Leiteigenschaft = "GE", Steigerungsfaktor = "B" },
            new() { Kampftechnik = "Fechtwaffen", Leiteigenschaft = "GE", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Hiebwaffen", Leiteigenschaft = "KK", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Kettenwaffen", Leiteigenschaft = "KK", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Lanzen", Leiteigenschaft = "KK", Steigerungsfaktor = "B" },
            new() { Kampftechnik = "Raufen", Leiteigenschaft = "GE/KK", Steigerungsfaktor = "B" },
            new() { Kampftechnik = "Schilde", Leiteigenschaft = "KK", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Schwerter", Leiteigenschaft = "GE/KK", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Stangenwaffen", Leiteigenschaft = "GE/KK", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Wurfwaffen", Leiteigenschaft = "FF", Steigerungsfaktor = "B", IstFernkampf = true },
            new() { Kampftechnik = "Zweihandhiebwaffen", Leiteigenschaft = "KK", Steigerungsfaktor = "C" },
            new() { Kampftechnik = "Zweihandschwerter", Leiteigenschaft = "KK", Steigerungsfaktor = "C" },
        };
    }

    private IReadOnlyList<StatusRow> BuildKampfStatiRows()
    {
        var rows = new StatusRow[]
        {
            new("Belastung"),
            new("Berauschtheit"),
            new("Betäubung"),
            new("Entrückt"),
            new("Furcht"),
            new("Paralyse"),
            new("Schmerz"),
            new("Verwirrung"),
        };

        // Lade gespeicherte Werte aus CharacterSheet
        foreach (var row in rows)
        {
            if (_sheet.KampfStati.TryGetValue(row.StatusName, out int stufe))
            {
                row.Stufe = stufe; // Direkt Stufe (0-4) setzen
            }
        }

        return rows;
    }

    /// <summary>Abonniere PropertyChanged für alle StatusRows, um Änderungen zu speichern.</summary>
    private void SubscribeToStatusChanges()
    {
        foreach (var row in KampfStatiRows)
        {
            row.PropertyChanged += OnStatusRowChanged;
        }
    }

    private void OnStatusRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is StatusRow row)
        {
            // Speichere aktuelle Stufe (0-4) im CharacterSheet
            _sheet.KampfStati[row.StatusName] = row.Stufe;

            // Neuberechnung von AT/PA wegen Status-Modifikatoren
            RecalculateKampftechniken();

            RequestDelayedSave();
        }
    }

    /// <summary>Berechnet alle Talent-Probenwerte neu (nach Attribut-/VN-/SF-Änderung).</summary>
    private void RecalculateTalentProben()
    {
        _talentModifierService?.UpdateTalentProben(
            TalentGruppen,
            BuildAttributeDictionary(),
            VorteilNachteilEintraege.ToList(),
            SonderfertigkeitEintraege.ToList());
    }

    private Dictionary<string, int> BuildAttributeDictionary() => new()
    {
        ["MU"] = _sheet.Mut,
        ["KL"] = _sheet.Klugheit,
        ["IN"] = _sheet.Intuition,
        ["CH"] = _sheet.Charisma,
        ["FF"] = _sheet.Fingerfertigkeit,
        ["GE"] = _sheet.Gewandtheit,
        ["KO"] = _sheet.Konstitution,
        ["KK"] = _sheet.Körperkraft
    };

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
