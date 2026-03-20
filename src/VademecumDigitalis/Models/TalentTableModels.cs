using System.ComponentModel;

namespace VademecumDigitalis.Models;

public class TalentGroup
{
    public TalentGroup(string gruppe, IEnumerable<TalentRow> eintraege)
    {
        Gruppe = gruppe;
        Eintraege = eintraege.ToList();
    }

    public string Gruppe { get; }

    public List<TalentRow> Eintraege { get; }
}

public class TalentRow : INotifyPropertyChanged
{
    private string _fw = string.Empty;
    private string _anmerkung = string.Empty;

    public string Talent { get; set; } = string.Empty;
    public string Steigerungsfaktor { get; set; } = string.Empty;
    public string Probe1 { get; set; } = string.Empty;
    public string Probe2 { get; set; } = string.Empty;
    public string Probe3 { get; set; } = string.Empty;
    public string Belastungseinfluss { get; set; } = string.Empty;

    public string Fw
    {
        get => _fw;
        set
        {
            if (_fw != value)
            {
                _fw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fw)));
            }
        }
    }

    public string Anmerkung
    {
        get => _anmerkung;
        set
        {
            if (_anmerkung != value)
            {
                _anmerkung = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anmerkung)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
