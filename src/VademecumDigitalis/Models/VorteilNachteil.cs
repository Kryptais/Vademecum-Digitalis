using System.ComponentModel;

namespace VademecumDigitalis.Models;

/// <summary>Unterscheidet Vorteile von Nachteilen.</summary>
public enum VorteilNachteilTyp
{
    Vorteil,
    Nachteil
}

/// <summary>
/// Katalog-Eintrag: beschreibt einen DSA-5-Vorteil oder -Nachteil
/// (Name, AP-Kosten, maximale Stufe, Regeltext).
/// </summary>
public class VorteilNachteil
{
    /// <summary>Offizieller Name aus dem Regelwerk.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Vorteil oder Nachteil.</summary>
    public VorteilNachteilTyp Typ { get; set; }

    /// <summary>Maximale Stufe (1 für nicht-gestufte Vorteile/Nachteile).</summary>
    public int MaxStufe { get; set; } = 1;

    /// <summary>
    /// AP-Kosten pro Stufe. Positive Werte = AP-Ausgabe (Vorteile),
    /// negative Werte = AP-Erstattung (Nachteile).
    /// </summary>
    public int ApKostenProStufe { get; set; } = 0;

    /// <summary>Kurz-Regeltext.</summary>
    public string Regeltext { get; set; } = string.Empty;
}

/// <summary>
/// Ein Vorteil oder Nachteil, den ein Charakter tatsächlich besitzt.
/// Unterstützt INotifyPropertyChanged für MVVM-Binding.
/// </summary>
public class CharaktervorteilEintrag : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private VorteilNachteilTyp _typ;
    private int _stufe = 1;
    private int _apKosten = 0;
    private string _notiz = string.Empty;

    /// <summary>Name des Vorteils/Nachteils.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnzeigeText)));
            }
        }
    }

    /// <summary>Vorteil oder Nachteil.</summary>
    public VorteilNachteilTyp Typ
    {
        get => _typ;
        set
        {
            if (_typ != value)
            {
                _typ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Typ)));
            }
        }
    }

    /// <summary>Gewählte Stufe (1 bei nicht-gestuften Einträgen).</summary>
    public int Stufe
    {
        get => _stufe;
        set
        {
            if (_stufe != value)
            {
                _stufe = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stufe)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnzeigeText)));
            }
        }
    }

    /// <summary>AP-Kosten für diesen Eintrag (positiv = Ausgabe, negativ = Erstattung).</summary>
    public int ApKosten
    {
        get => _apKosten;
        set
        {
            if (_apKosten != value)
            {
                _apKosten = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ApKosten)));
            }
        }
    }

    /// <summary>Optionale Anmerkung (z. B. Spezialisierung, Quelle).</summary>
    public string Notiz
    {
        get => _notiz;
        set
        {
            if (_notiz != value)
            {
                _notiz = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notiz)));
            }
        }
    }

    /// <summary>Anzeigetext für die Liste: Name + ggf. Stufe.</summary>
    public string AnzeigeText =>
        MaxStufe > 1 ? $"{Name} {Stufe}" : Name;

    /// <summary>Maximale Stufe laut Katalog (wird beim Laden gesetzt, nicht persistiert).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int MaxStufe { get; set; } = 1;

    public event PropertyChangedEventHandler? PropertyChanged;
}
