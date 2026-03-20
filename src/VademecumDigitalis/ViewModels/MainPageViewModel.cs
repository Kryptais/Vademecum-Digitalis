using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly CharacterSheet _sheet = new();
    private readonly ICharacterSaveService _saveService;

    public MainPageViewModel()
    {
        _saveService = new CharacterSaveService();
        TalentGruppen = BuildTalentGruppen();
        Spezies_Liste = BuildSpezies();
        Basiswerte = BuildBasiswerte();
        ToggleExpandCommand = new Command<TalentGroup>(ToggleGroupExpand);
        SaveCharacterCommand = new Command(SaveCharacter);
        LoadCharacterCommand = new Command(LoadCharacter);
    }

    public ICommand ToggleExpandCommand { get; }
    public ICommand SaveCharacterCommand { get; }
    public ICommand LoadCharacterCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TalentGroup> TalentGruppen { get; }
    public IReadOnlyList<Species> Spezies_Liste { get; }
    public List<Basiswert> Basiswerte { get; }

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

    private Species? _selectedSpecies;

    public Species? Spezies
    {
        get => _selectedSpecies;
        set
        {
            if (_selectedSpecies != value)
            {
                _selectedSpecies = value;
                _sheet.Spezies = value?.Name ?? string.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Spezies)));
                UpdateBasiswerte();
            }
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

    public int Mut
    {
        get => _sheet.Mut;
        set => SetProperty(_sheet.Mut, value, v => _sheet.Mut = v);
    }

    public int Klugheit
    {
        get => _sheet.Klugheit;
        set => SetProperty(_sheet.Klugheit, value, v => _sheet.Klugheit = v);
    }

    public int Intuition
    {
        get => _sheet.Intuition;
        set => SetProperty(_sheet.Intuition, value, v => _sheet.Intuition = v);
    }

    public int Charisma
    {
        get => _sheet.Charisma;
        set => SetProperty(_sheet.Charisma, value, v => _sheet.Charisma = v);
    }

    public int Fingerfertigkeit
    {
        get => _sheet.Fingerfertigkeit;
        set => SetProperty(_sheet.Fingerfertigkeit, value, v => _sheet.Fingerfertigkeit = v);
    }

    public int Gewandtheit
    {
        get => _sheet.Gewandtheit;
        set => SetProperty(_sheet.Gewandtheit, value, v => _sheet.Gewandtheit = v);
    }

    public int Konstitution
    {
        get => _sheet.Konstitution;
        set
        {
            SetProperty(_sheet.Konstitution, value, v => _sheet.Konstitution = v);
            UpdateBasiswerte();
        }
    }

    public int Körperkraft
    {
        get => _sheet.Körperkraft;
        set => SetProperty(_sheet.Körperkraft, value, v => _sheet.Körperkraft = v);
    }

    public int Lebensenergie
    {
        get => _sheet.Lebensenergie;
        set => SetProperty(_sheet.Lebensenergie, value, v => _sheet.Lebensenergie = v);
    }

    private void UpdateBasiswerte()
    {
        var lepEntry = Basiswerte.FirstOrDefault(b => b.Name == "Lebensenergie");
        if (lepEntry != null)
        {
            lepEntry.Grundwert = (_selectedSpecies?.LE ?? 0) + (Konstitution * 2);
            Lebensenergie = lepEntry.Gesamt;
        }

        var gsEntry = Basiswerte.FirstOrDefault(b => b.Name == "Geschwindigkeit");
        if (gsEntry != null)
        {
            gsEntry.Grundwert = _selectedSpecies?.GS ?? 8;
            Geschwindigkeit = gsEntry.Gesamt;
        }

        // Synchronisiere alle anderen Werte von Basiswerte-Tabelle in die Properties
        foreach (var bw in Basiswerte)
        {
            switch (bw.Name)
            {
                case "Astralenergie":
                    Astralenergie = bw.Gesamt;
                    break;
                case "Karmaenergie":
                    Karmaenergie = bw.Gesamt;
                    break;
                case "Seelenkraft":
                    Seelenkraft = bw.Gesamt;
                    break;
                case "Zähigkeit":
                    Zähigkeit = bw.Gesamt;
                    break;
                case "Initiative-Basis":
                    InitiativeBasis = bw.Gesamt;
                    break;
            }
        }
    }

    private void ToggleGroupExpand(TalentGroup? group)
    {
        if (group != null)
        {
            group.IsExpanded = !group.IsExpanded;
        }
    }

    public int Astralenergie
    {
        get => _sheet.Astralenergie;
        set => SetProperty(_sheet.Astralenergie, value, v => _sheet.Astralenergie = v);
    }

    public int Karmaenergie
    {
        get => _sheet.Karmaenergie;
        set => SetProperty(_sheet.Karmaenergie, value, v => _sheet.Karmaenergie = v);
    }

    public int Seelenkraft
    {
        get => _sheet.Seelenkraft;
        set => SetProperty(_sheet.Seelenkraft, value, v => _sheet.Seelenkraft = v);
    }

    public int Zähigkeit
    {
        get => _sheet.Zähigkeit;
        set => SetProperty(_sheet.Zähigkeit, value, v => _sheet.Zähigkeit = v);
    }

    public int InitiativeBasis
    {
        get => _sheet.InitiativeBasis;
        set => SetProperty(_sheet.InitiativeBasis, value, v => _sheet.InitiativeBasis = v);
    }

    public int Geschwindigkeit
    {
        get => _sheet.Geschwindigkeit;
        set => SetProperty(_sheet.Geschwindigkeit, value, v => _sheet.Geschwindigkeit = v);
    }

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

    private static IReadOnlyList<TalentGroup> BuildTalentGruppen()
    {
        var groups = new[]
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
                NewTalent("Spährenkunde", "B", "KL", "KL", "IN", "NEIN"),
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

        // Set indices for zebra striping
        foreach (var group in groups)
        {
            for (int i = 0; i < group.Eintraege.Count; i++)
            {
                group.Eintraege[i].Index = i;
            }
        }

        return groups;
    }

    private static IReadOnlyList<Species> BuildSpezies()
    {
        return
        [
            new Species
            {
                Name = "Mensch",
                LE = 5,
                SK = -5,
                ZK = -5,
                GS = 8,
                Eigenschaften = "eine beliebige +1",
                Vorteile = "keine",
                Nachteile = "keine",
                APWert = 0
            },
            new Species
            {
                Name = "Elf",
                LE = 2,
                SK = -4,
                ZK = -6,
                GS = 8,
                Eigenschaften = "IN und GE +1; KL oder KK –2",
                Vorteile = "Zauberer, Zweistimmiger Gesang",
                Nachteile = "keine",
                APWert = 18
            },
            new Species
            {
                Name = "Halbelf",
                LE = 5,
                SK = -4,
                ZK = -6,
                GS = 8,
                Eigenschaften = "eine beliebige +1",
                Vorteile = "keine",
                Nachteile = "keine",
                APWert = 0
            },
            new Species
            {
                Name = "Zwerg",
                LE = 8,
                SK = -4,
                ZK = -4,
                GS = 6,
                Eigenschaften = "KO und KK +1; CH oder GE –2",
                Vorteile = "keine",
                Nachteile = "keine",
                APWert = 61
            }
        ];
    }

    private List<Basiswert> BuildBasiswerte()
    {
        return new List<Basiswert>
        {
            new Basiswert { Name = "Lebensenergie", Einheit = "LeP", Grundwert = 0, Zukauf = 0, Boni = 0, AllowsZukauf = true, AllowsBoni = true },
            new Basiswert { Name = "Astralenergie", Einheit = "AsP", Grundwert = 0, Zukauf = 0, Boni = 0, AllowsZukauf = true, AllowsBoni = true },
            new Basiswert { Name = "Karmaenergie", Einheit = "KaP", Grundwert = 0, Zukauf = 0, Boni = 0, AllowsZukauf = true, AllowsBoni = true },
            new Basiswert { Name = "Seelenkraft", Einheit = "SK", Grundwert = 0, Zukauf = 0, Boni = 0, AllowsZukauf = false, AllowsBoni = true },
            new Basiswert { Name = "Zähigkeit", Einheit = "ZK", Grundwert = 0, Zukauf = 0, Boni = 0, AllowsZukauf = false, AllowsBoni = true },
            new Basiswert { Name = "Initiative-Basis", Einheit = "ini", Grundwert = 0, Zukauf = 0, Boni = 0, AllowsZukauf = false, AllowsBoni = true },
            new Basiswert { Name = "Geschwindigkeit", Einheit = "GS", Grundwert = 8, Zukauf = 0, Boni = 0, AllowsZukauf = false, AllowsBoni = true }
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

    private void SetProperty<T>(T oldValue, T newValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return;
        }

        setter(newValue);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void SaveCharacter()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Fehler", 
                    "Bitte geben Sie einen Namen für den Charakter ein.", 
                    "OK");
                return;
            }

            await _saveService.SaveCharacterAsync(_sheet, Name);
            await Application.Current!.MainPage!.DisplayAlert(
                "Erfolg", 
                $"Charakter '{Name}' wurde gespeichert.", 
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Fehler beim Speichern", 
                ex.Message, 
                "OK");
        }
    }

    private async void LoadCharacter()
    {
        try
        {
            var savedCharacters = (await _saveService.GetSavedCharactersAsync()).ToList();

            if (savedCharacters.Count == 0)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Keine Charaktere", 
                    "Es wurden keine gespeicherten Charaktere gefunden.", 
                    "OK");
                return;
            }

            var selectedName = await Application.Current!.MainPage!.DisplayActionSheet(
                "Charakter laden", 
                "Abbrechen", 
                null, 
                savedCharacters.ToArray());

            if (string.IsNullOrEmpty(selectedName) || selectedName == "Abbrechen")
                return;

            var savePath = _saveService.GetCharacterPath(selectedName);
            var loadedSheet = await _saveService.LoadCharacterAsync(savePath);

            if (loadedSheet != null)
            {
                LoadSheetData(loadedSheet);
                await Application.Current!.MainPage!.DisplayAlert(
                    "Erfolg", 
                    $"Charakter '{selectedName}' wurde geladen.", 
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Fehler beim Laden", 
                ex.Message, 
                "OK");
        }
    }

    private void LoadSheetData(CharacterSheet sheet)
    {
        _sheet.Name = sheet.Name;
        _sheet.Spieler = sheet.Spieler;
        _sheet.Spezies = sheet.Spezies;
        _sheet.Kultur = sheet.Kultur;
        _sheet.Profession = sheet.Profession;
        _sheet.Geschlecht = sheet.Geschlecht;
        _sheet.Geburtstag = sheet.Geburtstag;
        _sheet.Alter = sheet.Alter;
        _sheet.Größe = sheet.Größe;
        _sheet.Gewicht = sheet.Gewicht;
        _sheet.Haarfarbe = sheet.Haarfarbe;
        _sheet.Augenfarbe = sheet.Augenfarbe;
        _sheet.Sozialstatus = sheet.Sozialstatus;

        _sheet.Mut = sheet.Mut;
        _sheet.Klugheit = sheet.Klugheit;
        _sheet.Intuition = sheet.Intuition;
        _sheet.Charisma = sheet.Charisma;
        _sheet.Fingerfertigkeit = sheet.Fingerfertigkeit;
        _sheet.Gewandtheit = sheet.Gewandtheit;
        _sheet.Konstitution = sheet.Konstitution;
        _sheet.Körperkraft = sheet.Körperkraft;

        _sheet.Lebensenergie = sheet.Lebensenergie;
        _sheet.Astralenergie = sheet.Astralenergie;
        _sheet.Karmaenergie = sheet.Karmaenergie;
        _sheet.Seelenkraft = sheet.Seelenkraft;
        _sheet.Zähigkeit = sheet.Zähigkeit;
        _sheet.InitiativeBasis = sheet.InitiativeBasis;
        _sheet.Geschwindigkeit = sheet.Geschwindigkeit;

        _sheet.AbenteuerpunkteGesamt = sheet.AbenteuerpunkteGesamt;
        _sheet.AbenteuerpunkteVerfuegbar = sheet.AbenteuerpunkteVerfuegbar;
        _sheet.AbenteuerpunkteAusgegeben = sheet.AbenteuerpunkteAusgegeben;

        _sheet.SchicksalspunkteGesamt = sheet.SchicksalspunkteGesamt;
        _sheet.SchicksalspunkteVerfuegbar = sheet.SchicksalspunkteVerfuegbar;

        _sheet.Vorteile = sheet.Vorteile;
        _sheet.Nachteile = sheet.Nachteile;
        _sheet.Talente = sheet.Talente;
        _sheet.Kampftalente = sheet.Kampftalente;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
