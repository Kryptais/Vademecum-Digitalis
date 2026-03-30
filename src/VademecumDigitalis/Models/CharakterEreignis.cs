using System.ComponentModel;

namespace VademecumDigitalis.Models;

/// <summary>
/// Ein dokumentiertes Charakter-Ereignis, das Stat-Boni/Mali dauerhaft verändert.
/// </summary>
public class CharakterEreignis : INotifyPropertyChanged
{
    private string _datumStr = string.Empty;
    private string _beschreibung = string.Empty;
    private int _alterBonus;
    private int _schicksalspunkteBonus;
    private int _lepBonus;
    private int _aspBonus;
    private int _kapBonus;
    private int _skBonus;
    private int _zkBonus;

    /// <summary>Aventurisches Datum als String, z.B. "13. Boron 1060 BF".</summary>
    public string DatumStr
    {
        get => _datumStr;
        set { if (_datumStr != value) { _datumStr = value; OnPropertyChanged(); } }
    }

    /// <summary>Beschreibung des Ereignisses.</summary>
    public string Beschreibung
    {
        get => _beschreibung;
        set { if (_beschreibung != value) { _beschreibung = value; OnPropertyChanged(); } }
    }

    /// <summary>Altersveränderung in Jahren (positiv = älter, negativ = jünger).</summary>
    public int AlterBonus
    {
        get => _alterBonus;
        set { if (_alterBonus != value) { _alterBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Schicksalspunkte-Veränderung (dauerhaft verbrannt/geschenkt).</summary>
    public int SchicksalspunkteBonus
    {
        get => _schicksalspunkteBonus;
        set { if (_schicksalspunkteBonus != value) { _schicksalspunkteBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Lebensenergie-Bonus/-Malus aus dem Ereignis.</summary>
    public int LepBonus
    {
        get => _lepBonus;
        set { if (_lepBonus != value) { _lepBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Astralenergie-Bonus/-Malus.</summary>
    public int AspBonus
    {
        get => _aspBonus;
        set { if (_aspBonus != value) { _aspBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Karmaenergie-Bonus/-Malus.</summary>
    public int KapBonus
    {
        get => _kapBonus;
        set { if (_kapBonus != value) { _kapBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Seelenkraft-Bonus/-Malus.</summary>
    public int SkBonus
    {
        get => _skBonus;
        set { if (_skBonus != value) { _skBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Zähigkeit-Bonus/-Malus.</summary>
    public int ZkBonus
    {
        get => _zkBonus;
        set { if (_zkBonus != value) { _zkBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(Zusammenfassung)); } }
    }

    /// <summary>Kurzdarstellung für die Liste.</summary>
    public string Zusammenfassung
    {
        get
        {
            var boni = new List<string>();
            if (AlterBonus != 0) boni.Add($"Alter {AlterBonus:+#;-#;0} J.");
            if (SchicksalspunkteBonus != 0) boni.Add($"SchiP {SchicksalspunkteBonus:+#;-#;0}");
            if (LepBonus != 0) boni.Add($"LeP {LepBonus:+#;-#;0}");
            if (AspBonus != 0) boni.Add($"AsP {AspBonus:+#;-#;0}");
            if (KapBonus != 0) boni.Add($"KaP {KapBonus:+#;-#;0}");
            if (SkBonus != 0) boni.Add($"SK {SkBonus:+#;-#;0}");
            if (ZkBonus != 0) boni.Add($"ZK {ZkBonus:+#;-#;0}");
            return boni.Count > 0 ? string.Join(", ", boni) : "Keine Stat-Änderungen";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
