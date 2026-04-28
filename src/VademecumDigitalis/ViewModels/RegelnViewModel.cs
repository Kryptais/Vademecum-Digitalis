using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>Katalogeintrag für die Regelansicht (flach, gruppiert nach Kategorie).</summary>
public sealed class RegelnKatalogEintrag
{
    public string Name { get; init; } = string.Empty;
    public string Kategorie { get; init; } = string.Empty;
    public string ApInfo { get; init; } = string.Empty;
    public string Beschreibung { get; init; } = string.Empty;
    public string Effekte { get; init; } = string.Empty;
    public bool IstNachteil { get; init; }
    public bool IsHomebrew { get; init; }
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

public class RegelnViewModel : INotifyPropertyChanged
{
    private readonly VorteilNachteilService _vnService;
    private string _searchText = string.Empty;
    private bool _isLoading;

    public RegelnViewModel(VorteilNachteilService vnService)
    {
        _vnService = vnService;
    }

    public ObservableCollection<RegelnKategorie> Gruppen { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; Notify(); } }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                Notify();
                _ = ApplyFilterAsync();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadAsync()
    {
        IsLoading = true;
        await _vnService.LoadCatalogAsync();
        await ApplyFilterAsync();
        IsLoading = false;
    }

    private Task ApplyFilterAsync()
    {
        var query = _searchText.Trim();
        var catalog = string.IsNullOrEmpty(query)
            ? _vnService.Catalog
            : _vnService.Catalog.Where(vn =>
                vn.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                vn.Beschreibung.Contains(query, StringComparison.OrdinalIgnoreCase));

        var entries = catalog
            .Select(vn => new RegelnKatalogEintrag
            {
                Name = vn.Name,
                Kategorie = vn.Kategorie.ToDisplayString(),
                ApInfo = BuildApInfo(vn),
                Beschreibung = vn.Beschreibung,
                Effekte = BuildEffekteText(vn),
                IstNachteil = vn.Kategorie.IstNachteil(),
                IsHomebrew = vn.IsHomebrew
            });

        var grouped = entries
            .GroupBy(e => e.Kategorie)
            .OrderBy(g => g.Key)
            .Select(g => new RegelnKategorie(g.Key, g.OrderBy(e => e.Name)));

        Gruppen.Clear();
        foreach (var g in grouped)
            Gruppen.Add(g);

        return Task.CompletedTask;
    }

    private static string BuildApInfo(VorteilNachteil vn)
    {
        if (vn.ApKostenProStufe.Count == 0) return "–";
        if (vn.ApKostenProStufe.Count == 1) return $"{vn.ApKostenProStufe[0]} AP";
        return string.Join(" / ", vn.ApKostenProStufe.Select(c => $"{c} AP"));
    }

    private static string BuildEffekteText(VorteilNachteil vn)
    {
        var effects = vn.Effects;
        if (effects.Count == 0) return string.Empty;

        return string.Join("\n", effects.Select(e =>
        {
            if (e.Kind == Models.RuleEngine.EffectKind.Narrative)
                return $"• {e.Title}: {e.Description}";
            var op = e.Operation switch
            {
                Models.RuleEngine.ModifierOp.Add => e.Value >= 0 ? $"+{e.Value}" : $"{e.Value}",
                Models.RuleEngine.ModifierOp.Multiply => $"×{e.Value}",
                Models.RuleEngine.ModifierOp.Override => $"→{e.Value}",
                _ => $"{e.Value}"
            };
            var perLvl = e.PerLevel ? " pro Stufe" : string.Empty;
            return $"• {e.Target} {op}{perLvl}";
        }));
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
