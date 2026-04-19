using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// ViewModel für die Sonderfertigkeiten-Seite mit Katalogsuche,
/// gruppierter Darstellung, Homebrew-Erstellung und Stufenaufstieg.
/// </summary>
public class SonderfertigkeitenViewModel : INotifyPropertyChanged
{
    private readonly SpecialAbilityService _sfService;
    private MainPageViewModel Vm => CharacterSheetSession.Current;

    private bool _isSearchVisible;
    private bool _isHomebrewMode;
    private string _searchText = string.Empty;

    // Homebrew-Felder
    private string _homebrewName = string.Empty;
    private string _homebrewBeschreibung = string.Empty;
    private int _homebrewMaxStufe = 1;
    private string _homebrewApKosten = string.Empty;
    private int _homebrewKategorieIndex;
    private string _homebrewAnmerkungen = string.Empty;

    public SonderfertigkeitenViewModel(SpecialAbilityService sfService)
    {
        _sfService = sfService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Hauptliste (gruppiert) ---

    /// <summary>Gruppierte Sonderfertigkeiten des Charakters.</summary>
    public ObservableCollection<SfGruppe> GruppierteSonderfertigkeiten { get; } = [];

    /// <summary>True wenn keine Sonderfertigkeiten vorhanden.</summary>
    public bool KeineSonderfertigkeiten => Vm.SonderfertigkeitEintraege.Count == 0;

    // --- Suche ---

    /// <summary>Ob der Such-/Hinzufügen-Bereich sichtbar ist.</summary>
    public bool IsSearchVisible
    {
        get => _isSearchVisible;
        set { if (_isSearchVisible != value) { _isSearchVisible = value; Notify(); } }
    }

    /// <summary>Ob der Homebrew-Modus aktiv ist.</summary>
    public bool IsHomebrewMode
    {
        get => _isHomebrewMode;
        set { if (_isHomebrewMode != value) { _isHomebrewMode = value; Notify(); Notify(nameof(IsCatalogSearchMode)); } }
    }

    /// <summary>Ob die Katalogsuche aktiv ist (Gegenteil von Homebrew).</summary>
    public bool IsCatalogSearchMode => !_isHomebrewMode;

    /// <summary>Suchtext für die Katalogsuche.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                Notify();
                UpdateSearchResults();
            }
        }
    }

    /// <summary>Suchergebnisse aus dem Katalog.</summary>
    public ObservableCollection<SpecialAbility> SearchResults { get; } = [];

    // --- Homebrew-Felder ---

    public string HomebrewName
    {
        get => _homebrewName;
        set { if (_homebrewName != value) { _homebrewName = value; Notify(); } }
    }

    public string HomebrewBeschreibung
    {
        get => _homebrewBeschreibung;
        set { if (_homebrewBeschreibung != value) { _homebrewBeschreibung = value; Notify(); } }
    }

    public int HomebrewMaxStufe
    {
        get => _homebrewMaxStufe;
        set { if (_homebrewMaxStufe != value) { _homebrewMaxStufe = Math.Max(1, value); Notify(); } }
    }

    public string HomebrewApKosten
    {
        get => _homebrewApKosten;
        set { if (_homebrewApKosten != value) { _homebrewApKosten = value; Notify(); } }
    }

    public int HomebrewKategorieIndex
    {
        get => _homebrewKategorieIndex;
        set { if (_homebrewKategorieIndex != value) { _homebrewKategorieIndex = value; Notify(); } }
    }

    public string HomebrewAnmerkungen
    {
        get => _homebrewAnmerkungen;
        set { if (_homebrewAnmerkungen != value) { _homebrewAnmerkungen = value; Notify(); } }
    }

    /// <summary>Alle Kategorienamen für den Picker.</summary>
    public IReadOnlyList<string> KategorieNamen { get; } = Enum.GetValues<SpecialAbilityCategory>()
        .Select(c => c.ToDisplayString())
        .ToList();

    // --- Initialisierung ---

    /// <summary>Lädt den Katalog und aktualisiert die gruppierte Ansicht.</summary>
    public async Task InitializeAsync()
    {
        await _sfService.LoadCatalogAsync();
        RefreshGroupedList();
    }

    // --- Aktionen ---

    /// <summary>Öffnet/schließt den Such-/Hinzufügen-Bereich.</summary>
    public void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (IsSearchVisible)
        {
            SearchText = string.Empty;
            IsHomebrewMode = false;
            UpdateSearchResults();
        }
    }

    /// <summary>Fügt eine Sonderfertigkeit aus dem Katalog zum Charakter hinzu.</summary>
    public void AddFromCatalog(SpecialAbility sf, bool forceAdd = false)
    {
        ArgumentNullException.ThrowIfNull(sf);

        // Prüfen ob schon vorhanden
        var existing = Vm.SonderfertigkeitEintraege
            .FirstOrDefault(e => e.SfId == sf.Id);

        if (existing != null)
        {
            // Wenn stufenbasiert, Stufe erhöhen
            if (existing.Stufe < sf.MaxStufe)
            {
                LevelUp(existing);
            }
            return;
        }

        var entry = SpecialAbilityService.CreateEntry(sf, stufe: 1, forceAdd: forceAdd);
        Vm.SonderfertigkeitHinzufuegen(entry);
        RefreshGroupedList();
        IsSearchVisible = false;
    }

    /// <summary>Erstellt eine Homebrew-SF und fügt sie zum Charakter hinzu.</summary>
    public void AddHomebrew()
    {
        if (string.IsNullOrWhiteSpace(HomebrewName))
            return;

        var category = Enum.GetValues<SpecialAbilityCategory>()
            .ElementAtOrDefault(HomebrewKategorieIndex);

        var apCosts = ParseApCosts(HomebrewApKosten, HomebrewMaxStufe);
        var id = $"homebrew-{Guid.NewGuid():N}";

        var homebrew = new SpecialAbility
        {
            Id = id,
            Name = HomebrewName.Trim(),
            Beschreibung = HomebrewBeschreibung?.Trim() ?? string.Empty,
            Kategorie = category,
            MaxStufe = HomebrewMaxStufe,
            ApKostenProStufe = apCosts,
            Anmerkungen = HomebrewAnmerkungen?.Trim() ?? string.Empty,
            IsHomebrew = true
        };

        _sfService.AddHomebrewEntry(homebrew);

        var entry = SpecialAbilityService.CreateEntry(homebrew, stufe: 1, forceAdd: true);
        Vm.SonderfertigkeitHinzufuegen(entry);

        ResetHomebrewFields();
        RefreshGroupedList();
        IsSearchVisible = false;
    }

    /// <summary>Erhöht die Stufe einer stufenbasierten SF.</summary>
    public void LevelUp(CharakterSonderfertigkeitEintrag entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var catalogEntry = _sfService.FindById(entry.SfId);
        int maxStufe = catalogEntry?.MaxStufe ?? entry.MaxStufe;

        if (entry.Stufe < maxStufe)
        {
            entry.Stufe++;
            RefreshGroupedList();
        }
    }

    /// <summary>Entfernt eine Sonderfertigkeit vom Charakter.</summary>
    public void Remove(CharakterSonderfertigkeitEintrag entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Vm.SonderfertigkeitEntfernen(entry);
        RefreshGroupedList();
    }

    /// <summary>Aktualisiert die gruppierte Anzeige aus den flachen Einträgen.</summary>
    public void RefreshGroupedList()
    {
        // MaxStufe aus Katalog aktualisieren
        foreach (var entry in Vm.SonderfertigkeitEintraege)
        {
            var catalogEntry = _sfService.FindById(entry.SfId);
            if (catalogEntry != null)
            {
                entry.MaxStufe = catalogEntry.MaxStufe;
            }
        }

        var grouped = Vm.SonderfertigkeitEintraege
            .GroupBy(e => e.Kategorie)
            .OrderBy(g => g.Key.SortOrder())
            .Select(g => new SfGruppe(g.Key.ToDisplayString(), g.OrderBy(e => e.Name)))
            .ToList();

        GruppierteSonderfertigkeiten.Clear();
        foreach (var group in grouped)
        {
            GruppierteSonderfertigkeiten.Add(group);
        }

        Notify(nameof(KeineSonderfertigkeiten));
    }

    private void UpdateSearchResults()
    {
        var results = _sfService.Search(_searchText);
        SearchResults.Clear();
        foreach (var sf in results)
        {
            SearchResults.Add(sf);
        }
    }

    private void ResetHomebrewFields()
    {
        HomebrewName = string.Empty;
        HomebrewBeschreibung = string.Empty;
        HomebrewMaxStufe = 1;
        HomebrewApKosten = string.Empty;
        HomebrewKategorieIndex = 0;
        HomebrewAnmerkungen = string.Empty;
    }

    private static List<int> ParseApCosts(string input, int maxStufe)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Enumerable.Repeat(10, maxStufe).ToList();

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var costs = new List<int>();
        foreach (var part in parts)
        {
            costs.Add(int.TryParse(part, out int val) ? val : 10);
        }

        // Auffüllen wenn weniger Werte als Stufen angegeben
        while (costs.Count < maxStufe)
        {
            costs.Add(costs.Count > 0 ? costs[^1] : 10);
        }

        return costs;
    }

    private void Notify([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
