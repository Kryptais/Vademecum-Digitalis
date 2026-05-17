using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VademecumDigitalis.Models;

public class TalentGroup : INotifyPropertyChanged
{
    private bool _isExpanded;

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

public class TalentRow : INotifyPropertyChanged
{
    private string _fw = string.Empty;
    private string _anmerkung = string.Empty;
    private int _probeWert1;
    private int _probeWert2;
    private int _probeWert3;
    private int _fwBonus;
    private bool _hatBegabung;
    private bool _hatUnfaehigkeit;

    public string Talent { get; set; } = string.Empty;
    public string Steigerungsfaktor { get; set; } = string.Empty;
    public string Probe1 { get; set; } = string.Empty;
    public string Probe2 { get; set; } = string.Empty;
    public string Probe3 { get; set; } = string.Empty;
    public string Belastungseinfluss { get; set; } = string.Empty;

    /// <summary>Talentgruppe, zu der dieses Talent gehört (z.B. "Körpertalente").</summary>
    public string Gruppe { get; set; } = string.Empty;

    public string Fw
    {
        get => _fw;
        set
        {
            if (_fw != value)
            {
                _fw = value;
                OnPropertyChanged(nameof(Fw));
                OnPropertyChanged(nameof(FwGesamt));
            }
        }
    }

    /// <summary>Bonus auf FW durch Vorteile/SF (z.B. durch Begabung).</summary>
    public int FwBonus
    {
        get => _fwBonus;
        set
        {
            if (_fwBonus != value)
            {
                _fwBonus = value;
                OnPropertyChanged(nameof(FwBonus));
                OnPropertyChanged(nameof(FwGesamt));
            }
        }
    }

    /// <summary>Berechneter Gesamt-FW (Basis + Bonus).</summary>
    [JsonIgnore]
    public string FwGesamt
    {
        get
        {
            if (!int.TryParse(_fw, out var basis))
                return _fw;
            var gesamt = basis + _fwBonus;
            return gesamt != basis ? $"{gesamt}" : _fw;
        }
    }

    /// <summary>Effektiver Probenwert für Probe 1 (Basisattribut + Modifikatoren).</summary>
    public int ProbeWert1
    {
        get => _probeWert1;
        set { if (_probeWert1 != value) { _probeWert1 = value; OnPropertyChanged(nameof(ProbeWert1)); } }
    }

    /// <summary>Effektiver Probenwert für Probe 2 (Basisattribut + Modifikatoren).</summary>
    public int ProbeWert2
    {
        get => _probeWert2;
        set { if (_probeWert2 != value) { _probeWert2 = value; OnPropertyChanged(nameof(ProbeWert2)); } }
    }

    /// <summary>Effektiver Probenwert für Probe 3 (Basisattribut + Modifikatoren).</summary>
    public int ProbeWert3
    {
        get => _probeWert3;
        set { if (_probeWert3 != value) { _probeWert3 = value; OnPropertyChanged(nameof(ProbeWert3)); } }
    }

    /// <summary>True wenn der Charakter eine Begabung für dieses Talent hat.</summary>
    public bool HatBegabung
    {
        get => _hatBegabung;
        set { if (_hatBegabung != value) { _hatBegabung = value; OnPropertyChanged(nameof(HatBegabung)); } }
    }

    /// <summary>True wenn der Charakter eine Unfähigkeit für dieses Talent hat.</summary>
    public bool HatUnfaehigkeit
    {
        get => _hatUnfaehigkeit;
        set { if (_hatUnfaehigkeit != value) { _hatUnfaehigkeit = value; OnPropertyChanged(nameof(HatUnfaehigkeit)); } }
    }

    private string _bonusInfo = string.Empty;

    /// <summary>Lesbare Zusammenfassung aktiver Boni durch VN/SF, z. B. "Flink +1, Behände +1".</summary>
    public string BonusInfo
    {
        get => _bonusInfo;
        set { if (_bonusInfo != value) { _bonusInfo = value; OnPropertyChanged(nameof(BonusInfo)); OnPropertyChanged(nameof(HatBonusInfo)); } }
    }

    public bool HatBonusInfo => !string.IsNullOrEmpty(_bonusInfo);

    public string Anmerkung
    {
        get => _anmerkung;
        set
        {
            if (_anmerkung != value)
            {
                _anmerkung = value;
                OnPropertyChanged(nameof(Anmerkung));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>Eine Zeile in der Kampftechnik-Tabelle (DSA 5).</summary>
public class KampftechnikRow : INotifyPropertyChanged
{
    private string _ktw = "6";
    private int _boni;
    private int _atFkBasis;
    private int _atFkBoniEffekte;
    private int _paradeBasis;
    private int _paradeBoniEffekte;

    public string Kampftechnik { get; set; } = string.Empty;

    /// <summary>Leiteigenschaft(en) – z.B. "GE/KK" oder "FF".</summary>
    public string Leiteigenschaft { get; set; } = string.Empty;

    public string Steigerungsfaktor { get; set; } = string.Empty;

    /// <summary>Gibt an ob dies eine Fernkampftechnik ist (AT wird über FF statt MU berechnet).</summary>
    public bool IstFernkampf { get; set; }

    /// <summary>Kampftechnikwert (editierbar, wird mit AP gesteigert).</summary>
    public string Ktw
    {
        get => _ktw;
        set
        {
            if (_ktw != value)
            {
                _ktw = value;
                OnPropertyChanged(nameof(Ktw));
                OnPropertyChanged(nameof(Gesamt));
            }
        }
    }

    /// <summary>
    /// Reiner AT/FK-Basiswert aus der Leiteigenschaft: ⌊(MU-8)/3⌋ bzw. ⌊(FF-8)/3⌋.
    /// Enthält keine Status-Modifikatoren und keine RuleEffect-Boni.
    /// </summary>
    public int AtFkBasis
    {
        get => _atFkBasis;
        set { if (_atFkBasis != value) { _atFkBasis = value; OnPropertyChanged(nameof(AtFkBasis)); OnPropertyChanged(nameof(Gesamt)); } }
    }

    /// <summary>Manueller AT-Boni (z. B. Formation, Umstandsmodifikatoren) – editierbar.</summary>
    public int Boni
    {
        get => _boni;
        set { if (_boni != value) { _boni = value; OnPropertyChanged(nameof(Boni)); OnPropertyChanged(nameof(Gesamt)); } }
    }

    /// <summary>
    /// AT/FK-Boni aus Vorteilen, Nachteilen, Sonderfertigkeiten und Zustands­modifikatoren
    /// (Schmerz, Furcht, …). Wird vom ViewModel berechnet.
    /// </summary>
    [JsonIgnore]
    public int BoniEffekte
    {
        get => _atFkBoniEffekte;
        set { if (_atFkBoniEffekte != value) { _atFkBoniEffekte = value; OnPropertyChanged(nameof(BoniEffekte)); OnPropertyChanged(nameof(Gesamt)); } }
    }

    /// <summary>Gesamtwert = KTW + AT/FK-Basis + manueller Boni + Effekt-/Status-Boni.</summary>
    [JsonIgnore]
    public int Gesamt
    {
        get
        {
            int.TryParse(_ktw, out var ktw);
            return ktw + _atFkBasis + _boni + _atFkBoniEffekte;
        }
    }

    /// <summary>
    /// Reiner Parade-Basiswert: ⌈KTW/2⌉ + ⌊(LE-8)/3⌋ (nur Nahkampf).
    /// Enthält keine Status-Modifikatoren und keine RuleEffect-Boni.
    /// </summary>
    public int ParadeBasis
    {
        get => _paradeBasis;
        set { if (_paradeBasis != value) { _paradeBasis = value; OnPropertyChanged(nameof(ParadeBasis)); OnPropertyChanged(nameof(Parade)); } }
    }

    /// <summary>PA-Boni aus Effekten und Zustands­modifikatoren. Wird vom ViewModel berechnet.</summary>
    [JsonIgnore]
    public int ParadeBoniEffekte
    {
        get => _paradeBoniEffekte;
        set { if (_paradeBoniEffekte != value) { _paradeBoniEffekte = value; OnPropertyChanged(nameof(ParadeBoniEffekte)); OnPropertyChanged(nameof(Parade)); } }
    }

    /// <summary>Parade-Gesamtwert = ParadeBasis + ParadeBoniEffekte.</summary>
    [JsonIgnore]
    public int Parade => _paradeBasis + _paradeBoniEffekte;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>Zeile für die Kampf-Status-Tabelle (Betäubung, Verwirrung, etc.).</summary>
public class StatusRow : INotifyPropertyChanged
{
    private int _stufe;

    public StatusRow(string statusName)
    {
        StatusName = statusName;
    }

    public string StatusName { get; }

    /// <summary>Aktuelle Stufe des Status (0 = keine, 1-3 = Modifikator -1 bis -3, 4 = handlungsunfähig).</summary>
    public int Stufe
    {
        get => _stufe;
        set
        {
            value = Math.Clamp(value, 0, 4); // Begrenze auf 0-4
            if (_stufe != value)
            {
                _stufe = value;
                OnPropertyChanged(nameof(Stufe));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
