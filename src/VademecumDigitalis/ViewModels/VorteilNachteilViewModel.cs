using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// ViewModel für die Vorteile/Nachteile-Seite mit Katalogsuche,
/// gruppierter Darstellung, Homebrew-Erstellung und Stufenaufstieg.
/// </summary>
public class VorteilNachteilViewModel : INotifyPropertyChanged
{
    private readonly VorteilNachteilService _vnService;
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

    public VorteilNachteilViewModel(VorteilNachteilService vnService)
    {
        _vnService = vnService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Hauptliste (gruppiert) ---

    /// <summary>Gruppierte Vorteile/Nachteile des Charakters.</summary>
    public ObservableCollection<VnGruppe> GruppierteVorteileNachteile { get; } = [];

    /// <summary>True wenn keine Vorteile/Nachteile vorhanden.</summary>
    public bool KeineVorteileNachteile => Vm.VorteilNachteilEintraege.Count == 0;

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
    public ObservableCollection<VorteilNachteil> SearchResults { get; } = [];

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
    public IReadOnlyList<string> KategorieNamen { get; } = Enum.GetValues<VorteilNachteilKategorie>()
        .Select(c => c.ToDisplayString())
        .ToList();

    // --- Initialisierung ---

    /// <summary>Lädt den Katalog und aktualisiert die gruppierte Ansicht.</summary>
    public async Task InitializeAsync()
    {
        await _vnService.LoadCatalogAsync();
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

    /// <summary>Fügt einen Vorteil/Nachteil aus dem Katalog zum Charakter hinzu.</summary>
    public void AddFromCatalog(VorteilNachteil vn, bool forceAdd = false)
    {
        ArgumentNullException.ThrowIfNull(vn);

        var existing = Vm.VorteilNachteilEintraege
            .FirstOrDefault(e => e.VnId == vn.Id);

        if (existing != null)
        {
            if (existing.Stufe < vn.MaxStufe)
            {
                LevelUp(existing);
            }
            return;
        }

        var entry = VorteilNachteilService.CreateEntry(vn, stufe: 1, forceAdd: forceAdd);
        Vm.VorteilNachteilHinzufuegen(entry);
        RefreshGroupedList();
        IsSearchVisible = false;
    }

    /// <summary>Erstellt einen Homebrew-Vorteil/Nachteil und fügt ihn zum Charakter hinzu.</summary>
    public void AddHomebrew()
    {
        if (string.IsNullOrWhiteSpace(HomebrewName))
            return;

        var category = Enum.GetValues<VorteilNachteilKategorie>()
            .ElementAtOrDefault(HomebrewKategorieIndex);

        var apCosts = ParseApCosts(HomebrewApKosten, HomebrewMaxStufe);
        var id = $"homebrew-{Guid.NewGuid():N}";

        var homebrew = new VorteilNachteil
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

        _vnService.AddHomebrewEntry(homebrew);

        var entry = VorteilNachteilService.CreateEntry(homebrew, stufe: 1, forceAdd: true);
        Vm.VorteilNachteilHinzufuegen(entry);

        ResetHomebrewFields();
        RefreshGroupedList();
        IsSearchVisible = false;
    }

    /// <summary>Erhöht die Stufe eines stufenbasierten VN.</summary>
    public void LevelUp(CharakterVorteilNachteilEintrag entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var catalogEntry = _vnService.FindById(entry.VnId);
        int maxStufe = catalogEntry?.MaxStufe ?? entry.MaxStufe;

        if (entry.Stufe < maxStufe)
        {
            entry.Stufe++;
            RefreshGroupedList();
        }
    }

    /// <summary>Entfernt einen Vorteil/Nachteil vom Charakter.</summary>
    public void Remove(CharakterVorteilNachteilEintrag entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Vm.VorteilNachteilEntfernen(entry);
        RefreshGroupedList();
    }

    /// <summary>Aktualisiert die gruppierte Anzeige aus den flachen Einträgen.</summary>
    public void RefreshGroupedList()
    {
        foreach (var entry in Vm.VorteilNachteilEintraege)
        {
            var catalogEntry = _vnService.FindById(entry.VnId);
            if (catalogEntry != null)
            {
                entry.MaxStufe = catalogEntry.MaxStufe;
            }
        }

        var grouped = Vm.VorteilNachteilEintraege
            .GroupBy(e => e.Kategorie)
            .OrderBy(g => g.Key.SortOrder())
            .Select(g => new VnGruppe(g.Key.ToDisplayString(), g.OrderBy(e => e.Name)))
            .ToList();

        GruppierteVorteileNachteile.Clear();
        foreach (var group in grouped)
        {
            GruppierteVorteileNachteile.Add(group);
        }

        Notify(nameof(KeineVorteileNachteile));
    }

    private void UpdateSearchResults()
    {
        var results = _vnService.Search(_searchText);
        SearchResults.Clear();
        foreach (var vn in results)
        {
            SearchResults.Add(vn);
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
