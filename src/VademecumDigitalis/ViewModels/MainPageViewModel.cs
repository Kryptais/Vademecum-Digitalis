using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly CharacterSheet _sheet = new();

    public MainPageViewModel()
    {
        TalentGruppen = BuildTalentGruppen();
        SpeziesOptionen = BuildSpeziesOptionen();
        AusgewählteSpeziesOption = SpeziesOptionen.FirstOrDefault(o => o.Name == "Mensch") ?? SpeziesOptionen[0];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TalentGroup> TalentGruppen { get; }

    public IReadOnlyList<SpeziesOption> SpeziesOptionen { get; }

    private SpeziesOption? _ausgewählteSpeziesOption;
    public SpeziesOption? AusgewählteSpeziesOption
    {
        get => _ausgewählteSpeziesOption;
        set
        {
            if (ReferenceEquals(_ausgewählteSpeziesOption, value))
            {
                return;
            }

            _ausgewählteSpeziesOption = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AusgewählteSpeziesOption)));

            if (value is null)
            {
                return;
            }

            Spezies = value.Name;
            ApplySpeziesGrundwerte(value);
        }
    }

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
        set => SetProperty(_sheet.Spezies, value, v => _sheet.Spezies = v);
    }

    public int SpeziesApKosten
    {
        get => _sheet.SpeziesApKosten;
        set => SetProperty(_sheet.SpeziesApKosten, value, v => _sheet.SpeziesApKosten = v);
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
        set
        {
            SetProperty(_sheet.Mut, value, v => _sheet.Mut = v);
            RecalculateBasiswerte();
        }
    }

    public int Klugheit
    {
        get => _sheet.Klugheit;
        set
        {
            SetProperty(_sheet.Klugheit, value, v => _sheet.Klugheit = v);
            RecalculateBasiswerte();
        }
    }

    public int Intuition
    {
        get => _sheet.Intuition;
        set
        {
            SetProperty(_sheet.Intuition, value, v => _sheet.Intuition = v);
            RecalculateBasiswerte();
        }
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
        set
        {
            SetProperty(_sheet.Gewandtheit, value, v => _sheet.Gewandtheit = v);
            RecalculateBasiswerte();
        }
    }

    public int Konstitution
    {
        get => _sheet.Konstitution;
        set
        {
            SetProperty(_sheet.Konstitution, value, v => _sheet.Konstitution = v);
            RecalculateBasiswerte();
        }
    }

    public int Körperkraft
    {
        get => _sheet.Körperkraft;
        set
        {
            SetProperty(_sheet.Körperkraft, value, v => _sheet.Körperkraft = v);
            RecalculateBasiswerte();
        }
    }

    public int Lebensenergie
    {
        get => _sheet.Lebensenergie;
        set => SetProperty(_sheet.Lebensenergie, value, v => _sheet.Lebensenergie = v);
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

    public int LebensenergieZukauf
    {
        get => _sheet.LebensenergieZukauf;
        set
        {
            SetProperty(_sheet.LebensenergieZukauf, value, v => _sheet.LebensenergieZukauf = v);
            RecalculateBasiswerte();
        }
    }

    public int LebensenergieBoni
    {
        get => _sheet.LebensenergieBoni;
        set
        {
            SetProperty(_sheet.LebensenergieBoni, value, v => _sheet.LebensenergieBoni = v);
            RecalculateBasiswerte();
        }
    }

    public int SeelenkraftZukauf
    {
        get => _sheet.SeelenkraftZukauf;
        set
        {
            SetProperty(_sheet.SeelenkraftZukauf, value, v => _sheet.SeelenkraftZukauf = v);
            RecalculateBasiswerte();
        }
    }

    public int SeelenkraftBoni
    {
        get => _sheet.SeelenkraftBoni;
        set
        {
            SetProperty(_sheet.SeelenkraftBoni, value, v => _sheet.SeelenkraftBoni = v);
            RecalculateBasiswerte();
        }
    }

    public int ZähigkeitZukauf
    {
        get => _sheet.ZähigkeitZukauf;
        set
        {
            SetProperty(_sheet.ZähigkeitZukauf, value, v => _sheet.ZähigkeitZukauf = v);
            RecalculateBasiswerte();
        }
    }

    public int ZähigkeitBoni
    {
        get => _sheet.ZähigkeitBoni;
        set
        {
            SetProperty(_sheet.ZähigkeitBoni, value, v => _sheet.ZähigkeitBoni = v);
            RecalculateBasiswerte();
        }
    }

    public int InitiativeBasisZukauf
    {
        get => _sheet.InitiativeBasisZukauf;
        set
        {
            SetProperty(_sheet.InitiativeBasisZukauf, value, v => _sheet.InitiativeBasisZukauf = v);
            RecalculateBasiswerte();
        }
    }

    public int InitiativeBasisBoni
    {
        get => _sheet.InitiativeBasisBoni;
        set
        {
            SetProperty(_sheet.InitiativeBasisBoni, value, v => _sheet.InitiativeBasisBoni = v);
            RecalculateBasiswerte();
        }
    }

    public int GeschwindigkeitZukauf
    {
        get => _sheet.GeschwindigkeitZukauf;
        set
        {
            SetProperty(_sheet.GeschwindigkeitZukauf, value, v => _sheet.GeschwindigkeitZukauf = v);
            RecalculateBasiswerte();
        }
    }

    public int GeschwindigkeitBoni
    {
        get => _sheet.GeschwindigkeitBoni;
        set
        {
            SetProperty(_sheet.GeschwindigkeitBoni, value, v => _sheet.GeschwindigkeitBoni = v);
            RecalculateBasiswerte();
        }
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
        return
        [
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
        ];
    }

    private static IReadOnlyList<SpeziesOption> BuildSpeziesOptionen()
    {
        return
        [
            new SpeziesOption("Mensch", 0, 5, -5, -5, 8),
            new SpeziesOption("Elf", 110, 3, -4, -6, 8),
            new SpeziesOption("Halbelf", 20, 5, -5, -5, 8),
            new SpeziesOption("Zwerg", 80, 7, -4, -4, 6)
        ];
    }

    private SpeziesOption AktiveSpezies => AusgewählteSpeziesOption ?? SpeziesOptionen.First();

    private void ApplySpeziesGrundwerte(SpeziesOption option)
    {
        SpeziesApKosten = option.ApKosten;
        RecalculateBasiswerte();
    }

    private void RecalculateBasiswerte()
    {
        var spezies = AktiveSpezies;

        Lebensenergie = spezies.LebensenergieGrundwert + (2 * Konstitution) + LebensenergieZukauf + LebensenergieBoni;
        Seelenkraft = spezies.SeelenkraftGrundwert + ((Mut + Klugheit + Intuition) / 6) + SeelenkraftZukauf + SeelenkraftBoni;
        Zähigkeit = spezies.ZähigkeitGrundwert + ((Konstitution + Konstitution + Körperkraft) / 6) + ZähigkeitZukauf + ZähigkeitBoni;
        InitiativeBasis = (int)Math.Ceiling((Mut + Gewandtheit) / 2d) + InitiativeBasisZukauf + InitiativeBasisBoni;
        Geschwindigkeit = spezies.GeschwindigkeitGrundwert + GeschwindigkeitZukauf + GeschwindigkeitBoni;

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

    public sealed record SpeziesOption(
        string Name,
        int ApKosten,
        int LebensenergieGrundwert,
        int SeelenkraftGrundwert,
        int ZähigkeitGrundwert,
        int GeschwindigkeitGrundwert);

    private void SetProperty<T>(T oldValue, T newValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return;
        }

        setter(newValue);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
