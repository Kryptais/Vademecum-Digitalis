namespace VademecumDigitalis.Models;

using System.ComponentModel;

public class Basiswert : INotifyPropertyChanged
{
    private int _grundwert;
    private int _zukauf;
    private int _boni;

    public string Name { get; set; } = string.Empty;
    public string Einheit { get; set; } = string.Empty;
    public bool AllowsZukauf { get; set; } = false;
    public bool AllowsBoni { get; set; } = true;

    public int Grundwert
    {
        get => _grundwert;
        set
        {
            if (_grundwert != value)
            {
                _grundwert = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Grundwert)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gesamt)));
            }
        }
    }

    public int Zukauf
    {
        get => _zukauf;
        set
        {
            if (_zukauf != value)
            {
                _zukauf = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Zukauf)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gesamt)));
            }
        }
    }

    public int Boni
    {
        get => _boni;
        set
        {
            if (_boni != value)
            {
                _boni = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Boni)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gesamt)));
            }
        }
    }

    public int Gesamt => Math.Max(Grundwert + Zukauf + Boni, 1);

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class Species
{
    public string Name { get; set; } = string.Empty;
    public int LE { get; set; } // Lebensenergie
    public int SK { get; set; } // Seelenkraft
    public int ZK { get; set; } // Zähigkeit
    public int GS { get; set; } // Geschwindigkeit
    public string Eigenschaften { get; set; } = string.Empty;
    public string Vorteile { get; set; } = string.Empty;
    public string Nachteile { get; set; } = string.Empty;
    public int APWert { get; set; } // Abenteuerpunkte-Wert

    public override string ToString() => Name;
}

public class TalentGroup : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public TalentGroup(string gruppe, IEnumerable<TalentRow> eintraege)
    {
        Gruppe = gruppe;
        Eintraege = eintraege.ToList();
    }

    public string Gruppe { get; }

    public List<TalentRow> Eintraege { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class TalentRow
{
    public string Talent { get; set; } = string.Empty;
    public string Steigerungsfaktor { get; set; } = string.Empty;
    public string Probe1 { get; set; } = string.Empty;
    public string Probe2 { get; set; } = string.Empty;
    public string Probe3 { get; set; } = string.Empty;
    public string Belastungseinfluss { get; set; } = string.Empty;
    public string Fw { get; set; } = string.Empty;
    public string Anmerkung { get; set; } = string.Empty;
    public int Index { get; set; }

    public Color RowColor => (Index % 2 == 0) ? Colors.Gray : Color.Parse("#F5F5F5");
}
