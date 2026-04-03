using System.ComponentModel;

namespace VademecumDigitalis.Models;

/// <summary>
/// Repräsentiert einen einzelnen Vorteil oder Nachteil eines DSA-5-Charakters.
/// </summary>
public class Vorteil : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private int _stufe;
    private string _anmerkung = string.Empty;
    private bool _istNachteil;

    /// <summary>Name des Vorteils oder Nachteils.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Anzeige));
            }
        }
    }

    /// <summary>Stufe des Vorteils/Nachteils (0 = keine Stufe).</summary>
    public int Stufe
    {
        get => _stufe;
        set
        {
            if (_stufe != value)
            {
                _stufe = value;
                OnPropertyChanged(nameof(Stufe));
                OnPropertyChanged(nameof(Anzeige));
                OnPropertyChanged(nameof(HatStufe));
            }
        }
    }

    /// <summary>Optionale Anmerkung (z. B. betroffenes Talent bei Begabung).</summary>
    public string Anmerkung
    {
        get => _anmerkung;
        set
        {
            if (_anmerkung != value)
            {
                _anmerkung = value;
                OnPropertyChanged(nameof(Anmerkung));
                OnPropertyChanged(nameof(Anzeige));
            }
        }
    }

    /// <summary>Gibt an, ob es sich um einen Nachteil handelt.</summary>
    public bool IstNachteil
    {
        get => _istNachteil;
        set
        {
            if (_istNachteil != value)
            {
                _istNachteil = value;
                OnPropertyChanged(nameof(IstNachteil));
            }
        }
    }

    /// <summary>True wenn eine Stufe größer 0 vorliegt.</summary>
    public bool HatStufe => _stufe > 0;

    /// <summary>Anzeigetext für UI-Listen.</summary>
    public string Anzeige
    {
        get
        {
            var basis = HatStufe ? $"{Name} {Stufe}" : Name;
            return string.IsNullOrWhiteSpace(Anmerkung) ? basis : $"{basis} ({Anmerkung})";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
