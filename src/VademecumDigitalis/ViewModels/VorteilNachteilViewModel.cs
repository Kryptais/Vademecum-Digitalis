using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// ViewModel für die Vorteile/Nachteile-Seite mit Katalogsuche,
/// gruppierter Darstellung und Stufenaufstieg.
/// </summary>
public class VorteilNachteilViewModel : INotifyPropertyChanged
{
    private readonly VorteilNachteilService _vnService;
    private readonly TalentCatalogService _talentCatalogService;
    private MainPageViewModel Vm => CharacterSheetSession.Current;

    private bool _isSearchVisible;
    private bool _showNachteile;
    private string _searchText = string.Empty;

    public VorteilNachteilViewModel(VorteilNachteilService vnService, TalentCatalogService talentCatalogService)
    {
        _vnService = vnService;
        _talentCatalogService = talentCatalogService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Hauptliste (gruppiert) ---

    /// <summary>Gruppierte Vorteile/Nachteile des Charakters.</summary>
    public ObservableCollection<VnGruppe> GruppierteVorteileNachteile { get; } = [];

    /// <summary>True wenn keine Vorteile/Nachteile vorhanden.</summary>
    public bool KeineVorteileNachteile => Vm.VorteilNachteilEintraege.Count == 0;

    public int VorteileCount => Vm.VorteilNachteilEintraege.Count(e => !e.Kategorie.IstNachteil());

    public int NachteileCount => Vm.VorteilNachteilEintraege.Count(e => e.Kategorie.IstNachteil());

    public int GesamtApKosten => _vnService.CalculateTotalApCost(Vm.VorteilNachteilEintraege);

    public string GesamtApAnzeige => GesamtApKosten switch
    {
        > 0 => $"{GesamtApKosten} AP Kosten",
        < 0 => $"{Math.Abs(GesamtApKosten)} AP Rückgewinn",
        _ => "0 AP"
    };

    // --- Suche ---

    /// <summary>Ob der Such-/Hinzufügen-Bereich sichtbar ist.</summary>
    public bool IsSearchVisible
    {
        get => _isSearchVisible;
        set { if (_isSearchVisible != value) { _isSearchVisible = value; Notify(); } }
    }

    /// <summary>Schaltet die Katalogsuche zwischen Vorteilen und Nachteilen um.</summary>
    public bool ShowNachteile
    {
        get => _showNachteile;
        set
        {
            if (_showNachteile != value)
            {
                _showNachteile = value;
                Notify();
                Notify(nameof(KatalogModusTitel));
                UpdateSearchResults();
            }
        }
    }

    public string KatalogModusTitel => ShowNachteile ? "Nachteile" : "Vorteile";

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

    // --- Initialisierung ---

    /// <summary>Lädt den Katalog und aktualisiert die gruppierte Ansicht.</summary>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _vnService.LoadCatalogAsync(),
            _talentCatalogService.EnsureLoadedAsync()
        );
        RefreshGroupedList();
        UpdateSearchResults();
    }

    // --- Aktionen ---

    /// <summary>Öffnet/schließt den Such-/Hinzufügen-Bereich.</summary>
    public void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (IsSearchVisible)
        {
            SearchText = string.Empty;
            UpdateSearchResults();
        }
    }

    /// <summary>
    /// Fügt einen Vorteil/Nachteil aus dem Katalog zum Charakter hinzu.
    /// Für talent-gebundene VNs (Begabung/Unfähigkeit) wird ein Talent-Picker angezeigt.
    /// </summary>
    public async Task AddFromCatalogAsync(
        VorteilNachteil vn,
        Func<string[], string, Task<string?>>? talentPicker = null,
        bool forceAdd = false)
    {
        ArgumentNullException.ThrowIfNull(vn);

        if (vn.TalentTyp != TalentGebundenerTyp.Keiner)
        {
            if (talentPicker == null) return;

            // Alle Talentnamen aus dem Katalog, alphabetisch
            var alleTalente = _talentCatalogService.Katalog
                .Select(t => t.Name)
                .OrderBy(t => t)
                .ToArray();

            var title = vn.TalentTyp == TalentGebundenerTyp.Begabung
                ? "Talent für Begabung wählen"
                : "Talent für Unfähigkeit wählen";

            var selectedTalent = await talentPicker(alleTalente, title);
            if (string.IsNullOrEmpty(selectedTalent)) return;

            // Begabung: kein Duplikat für dasselbe Talent
            if (vn.TalentTyp == TalentGebundenerTyp.Begabung)
            {
                var alreadyHas = Vm.VorteilNachteilEintraege.Any(e =>
                    string.Equals(e.VnId, vn.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.Notiz?.Trim(), selectedTalent, StringComparison.OrdinalIgnoreCase));
                if (alreadyHas) return;
            }

            // AP-Kosten dynamisch berechnen (SF des Talents)
            var katalogTalent = _talentCatalogService.FindByName(selectedTalent);
            var sfIndex = SfToIndex(katalogTalent?.Sf ?? "A");
            var apKosten = vn.TalentTyp == TalentGebundenerTyp.Begabung
                ? sfIndex * 6
                : -(sfIndex * 10);

            var entry = VorteilNachteilService.CreateEntry(vn, stufe: 1, forceAdd: forceAdd);
            entry.Notiz = selectedTalent;
            entry.ApKosten = apKosten;
            Vm.VorteilNachteilHinzufuegen(entry);
            RefreshGroupedList();
            IsSearchVisible = false;
            return;
        }

        // Normaler (nicht talent-gebundener) VN
        var existing = Vm.VorteilNachteilEintraege
            .FirstOrDefault(e => e.VnId == vn.Id);

        if (existing != null)
        {
            if (existing.Stufe < vn.MaxStufe)
                LevelUp(existing);
            return;
        }

        var newEntry = VorteilNachteilService.CreateEntry(vn, stufe: 1, forceAdd: forceAdd);
        Vm.VorteilNachteilHinzufuegen(newEntry);
        RefreshGroupedList();
        IsSearchVisible = false;
    }

    private static int SfToIndex(string sf) => (sf?.ToUpperInvariant() ?? "A") switch
    {
        "A" => 1,
        "B" => 2,
        "C" => 3,
        "D" => 4,
        _ => 1
    };

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
        Notify(nameof(VorteileCount));
        Notify(nameof(NachteileCount));
        Notify(nameof(GesamtApKosten));
        Notify(nameof(GesamtApAnzeige));
    }

    private void UpdateSearchResults()
    {
        var results = _vnService.Search(_searchText)
            .Where(vn => ShowNachteile == vn.Kategorie.IstNachteil());

        SearchResults.Clear();
        foreach (var vn in results)
        {
            SearchResults.Add(vn);
        }
    }

    private void Notify([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
