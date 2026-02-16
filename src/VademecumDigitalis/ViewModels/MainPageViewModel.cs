using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly CharacterSheet _sheet = new();

    public event PropertyChangedEventHandler? PropertyChanged;

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
        set => SetProperty(_sheet.Konstitution, value, v => _sheet.Konstitution = v);
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
