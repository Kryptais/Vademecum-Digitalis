using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VademecumDigitalis.ViewModels;

/// <summary>Katalogeintrag für die Regelansicht (flach, gruppiert nach Kategorie).</summary>
public sealed class RegelnKatalogEintrag
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Kategorie { get; init; } = string.Empty;
    public string ApInfo { get; init; } = string.Empty;
    public string Beschreibung { get; init; } = string.Empty;
    public string Effekte { get; init; } = string.Empty;
    public bool IstNachteil { get; init; }
    public bool IsHomebrew { get; init; }
    /// <summary>True wenn die Regel-Logik fest im Code steckt – Eintrag nicht editier-/löschbar.</summary>
    public bool HasCodeLogic { get; init; }
    public bool IsEditable => !HasCodeLogic;
    public bool HasBeschreibung => !string.IsNullOrWhiteSpace(Beschreibung);
    public bool HasEffekte => !string.IsNullOrWhiteSpace(Effekte);
}

/// <summary>Kategorie-Gruppe mit einer Liste von Einträgen für die gruppierte CollectionView.</summary>
public sealed class RegelnKategorie : List<RegelnKatalogEintrag>
{
    public string Titel { get; }
    public RegelnKategorie(string titel, IEnumerable<RegelnKatalogEintrag> items) : base(items)
        => Titel = titel;
}

/// <summary>
/// ViewModel für die Regelseite mit Tab-Umschaltung.
/// Delegiert Vorteile/Nachteile-Logik an <see cref="RegelnVnViewModel"/>.
/// </summary>
public class RegelnViewModel : INotifyPropertyChanged
{
    private int _selectedTabIndex;

    public RegelnViewModel(RegelnVnViewModel vnTab)
    {
        VnTab = vnTab;
    }

    /// <summary>ViewModel für den Vorteile/Nachteile-Tab.</summary>
    public RegelnVnViewModel VnTab { get; }

    // --- Tab-Umschaltung ---

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex != value)
            {
                _selectedTabIndex = value;
                Notify();
                Notify(nameof(IsVnVisible));
                Notify(nameof(IsSfVisible));
                Notify(nameof(IsMagieVisible));
                Notify(nameof(IsGoetterwirkenVisible));
            }
        }
    }

    public bool IsVnVisible => SelectedTabIndex == 0;
    public bool IsSfVisible => SelectedTabIndex == 1;
    public bool IsMagieVisible => SelectedTabIndex == 2;
    public bool IsGoetterwirkenVisible => SelectedTabIndex == 3;

    public void SelectTab(int index)
    {
        SelectedTabIndex = index;
    }

    /// <summary>Lädt den aktiven Tab.</summary>
    public async Task LoadAsync()
    {
        await VnTab.LoadAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
