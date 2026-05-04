using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models;
using VademecumDigitalis.Models.RuleEngine;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// ViewModel für den Vorteile/Nachteile-Tab in der Regelseite.
/// Stellt CRUD-Operationen auf dem VN-Katalog bereit.
/// </summary>
public class RegelnVnViewModel : INotifyPropertyChanged
{
    private readonly VorteilNachteilService _vnService;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private bool _isEditorVisible;
    private bool _isEditing;
    private VorteilNachteilEditModel? _currentEdit;
    private string _editorTitle = string.Empty;

    public RegelnVnViewModel(VorteilNachteilService vnService)
    {
        _vnService = vnService;
    }

    // --- Properties ---

    public ObservableCollection<RegelnKategorie> VorteileGruppen { get; } = [];
    public ObservableCollection<RegelnKategorie> NachteileGruppen { get; } = [];

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
                ApplyFilter();
            }
        }
    }

    public bool IsEditorVisible
    {
        get => _isEditorVisible;
        set { if (_isEditorVisible != value) { _isEditorVisible = value; Notify(); Notify(nameof(IsListVisible)); } }
    }

    /// <summary>True wenn ein bestehender Eintrag bearbeitet wird, false bei Neuanlage.</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set { if (_isEditing != value) { _isEditing = value; Notify(); } }
    }

    public VorteilNachteilEditModel? CurrentEdit
    {
        get => _currentEdit;
        set { if (_currentEdit != value) { _currentEdit = value; Notify(); } }
    }

    public string EditorTitle
    {
        get => _editorTitle;
        set { if (_editorTitle != value) { _editorTitle = value; Notify(); } }
    }

    /// <summary>Die Katalog-Liste ist sichtbar wenn der Editor nicht offen ist.</summary>
    public bool IsListVisible => !IsEditorVisible;

    // --- Laden ---

    public async Task LoadAsync()
    {
        IsLoading = true;
        await _vnService.LoadCatalogAsync();
        ApplyFilter();
        IsLoading = false;
    }

    // --- CRUD ---

    /// <summary>Öffnet den Editor zum Erstellen eines neuen VN-Eintrags.</summary>
    public void StartCreate()
    {
        CurrentEdit = new VorteilNachteilEditModel { IsHomebrew = true };
        EditorTitle = "Neuen Vorteil / Nachteil erstellen";
        IsEditing = false;
        IsEditorVisible = true;
    }

    /// <summary>Öffnet den Editor zum Bearbeiten eines bestehenden Homebrew-Eintrags.</summary>
    public void StartEdit(string vnId)
    {
        var vn = _vnService.FindById(vnId);
        if (vn is null || !vn.IsHomebrew) return;

        CurrentEdit = VorteilNachteilEditModel.FromCatalog(vn);
        EditorTitle = $"Bearbeiten: {vn.Name}";
        IsEditing = true;
        IsEditorVisible = true;
    }

    /// <summary>Validiert und speichert den aktuellen Editor-Inhalt.</summary>
    /// <returns>Leerer String bei Erfolg, sonst Fehlermeldung.</returns>
    public async Task<string> SaveAsync()
    {
        if (CurrentEdit is null) return "Kein Eintrag zum Speichern.";

        if (!CurrentEdit.Validate(out var error))
            return error;

        try
        {
            var entry = CurrentEdit.ToCatalogEntry();

            if (IsEditing)
                await _vnService.UpdateHomebrewEntryAsync(entry);
            else
                await _vnService.AddHomebrewEntryAndPersistAsync(entry);

            IsEditorVisible = false;
            CurrentEdit = null;
            ApplyFilter();
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] StackTrace: {ex.StackTrace}");
            return $"Fehler beim Speichern: {ex.Message}";
        }
    }

    /// <summary>Löscht einen Homebrew-Eintrag aus dem Katalog.</summary>
    /// <returns>True wenn gelöscht.</returns>
    public async Task<bool> DeleteAsync(string vnId)
    {
        var vn = _vnService.FindById(vnId);
        if (vn is null || !vn.IsHomebrew) return false;

        await _vnService.DeleteHomebrewEntryAsync(vnId);
        ApplyFilter();
        return true;
    }

    /// <summary>Schließt den Editor ohne zu speichern.</summary>
    public void CancelEdit()
    {
        IsEditorVisible = false;
        CurrentEdit = null;
    }

    // --- Effekt-Verwaltung ---

    public void AddEffect()
    {
        CurrentEdit?.Effects.Add(new RuleEffectEditModel
        {
            Kind = EffectKind.Modifier,
            Phase = ModifierPhase.DerivedValues,
            Operation = ModifierOp.Add,
            Stacking = StackingRule.Stack
        });
    }

    public void RemoveEffect(RuleEffectEditModel effect)
    {
        CurrentEdit?.Effects.Remove(effect);
    }

    // --- Filter / Gruppierung ---

    private void ApplyFilter()
    {
        var query = _searchText.Trim();
        var catalog = string.IsNullOrEmpty(query)
            ? _vnService.Catalog
            : _vnService.Catalog.Where(vn =>
                vn.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                vn.Beschreibung.Contains(query, StringComparison.OrdinalIgnoreCase));

        var entries = catalog.Select(vn => new RegelnKatalogEintrag
        {
            Id = vn.Id,
            Name = vn.Name,
            Kategorie = vn.Kategorie.ToDisplayString(),
            ApInfo = BuildApInfo(vn),
            Beschreibung = vn.Beschreibung,
            Effekte = BuildEffekteText(vn),
            IstNachteil = vn.Kategorie.IstNachteil(),
            IsHomebrew = vn.IsHomebrew
        });

        // Vorteile
        var vorteile = entries.Where(e => !e.IstNachteil)
            .GroupBy(e => e.Kategorie)
            .OrderBy(g => g.Key)
            .Select(g => new RegelnKategorie(g.Key, g.OrderBy(e => e.Name)));

        VorteileGruppen.Clear();
        foreach (var g in vorteile)
            VorteileGruppen.Add(g);

        // Nachteile
        var nachteile = entries.Where(e => e.IstNachteil)
            .GroupBy(e => e.Kategorie)
            .OrderBy(g => g.Key)
            .Select(g => new RegelnKategorie(g.Key, g.OrderBy(e => e.Name)));

        NachteileGruppen.Clear();
        foreach (var g in nachteile)
            NachteileGruppen.Add(g);
    }

    private static string BuildApInfo(VorteilNachteil vn)
    {
        if (vn.TalentTyp != TalentGebundenerTyp.Keiner) return "variabel";
        if (vn.ApKostenProStufe.Count == 0) return "–";
        if (vn.ApKostenProStufe.Count == 1) return $"{vn.ApKostenProStufe[0]} AP";
        return string.Join(" / ", vn.ApKostenProStufe.Select(c => $"{c} AP"));
    }

    private static string BuildEffekteText(VorteilNachteil vn)
    {
        IReadOnlyList<RuleEffect> effects;
        try
        {
            effects = vn.Effects;
        }
        catch
        {
            // Safety: Falls ProbenModifikatoren/ExplicitEffects null nach Deserialisierung
            return string.Empty;
        }
        if (effects.Count == 0) return string.Empty;

        return string.Join("\n", effects.Select(e =>
        {
            if (e.Kind == EffectKind.Narrative)
                return $"• {e.Title}: {e.Description}";
            var op = e.Operation switch
            {
                ModifierOp.Add => e.Value >= 0 ? $"+{e.Value}" : $"{e.Value}",
                ModifierOp.Multiply => $"×{e.Value}",
                ModifierOp.Override => $"→{e.Value}",
                _ => $"{e.Value}"
            };
            var perLvl = e.PerLevel ? " pro Stufe" : string.Empty;
            return $"• {e.Target} {op}{perLvl}";
        }));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
