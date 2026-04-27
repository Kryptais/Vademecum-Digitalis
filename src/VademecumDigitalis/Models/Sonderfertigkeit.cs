using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VademecumDigitalis.Models;

/// <summary>Kategorien für DSA-5-Sonderfertigkeiten.</summary>
public enum SonderfertigkeitKategorie
{
    Allgemein,
    Kampf,
    Magisch,
    Karmal,
    Sprachschrift,
}

/// <summary>Eintrag einer Sonderfertigkeit auf dem Charakterbogen.</summary>
public class CharakterSonderfertigkeitEintrag : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private SonderfertigkeitKategorie _kategorie = SonderfertigkeitKategorie.Allgemein;
    private string _notiz = string.Empty;

    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; OnPropertyChanged(); }
    }

    public SonderfertigkeitKategorie Kategorie
    {
        get => _kategorie;
        set { if (_kategorie == value) return; _kategorie = value; OnPropertyChanged(); OnPropertyChanged(nameof(KategorieAnzeige)); }
    }

    public string Notiz
    {
        get => _notiz;
        set { if (_notiz == value) return; _notiz = value; OnPropertyChanged(); }
    }

    /// <summary>Anzeigename der Kategorie.</summary>
    public string KategorieAnzeige => Kategorie switch
    {
        SonderfertigkeitKategorie.Kampf => "Kampf-SF",
        SonderfertigkeitKategorie.Magisch => "Magische SF",
        SonderfertigkeitKategorie.Karmal => "Karmale SF",
        SonderfertigkeitKategorie.Sprachschrift => "Sprache/Schrift",
        _ => "Allgemeine SF"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
