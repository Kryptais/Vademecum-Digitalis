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

    private IReadOnlyList<RegelnKategorie> _vorteileGruppen = [];
    private IReadOnlyList<RegelnKategorie> _nachteileGruppen = [];

    /// <summary>
    /// Settable Property statt ObservableCollection — vermeidet Clear+Add auf einer
    /// grouped CollectionView, was unter MAUI/WinUI zu stowed exceptions führt.
    /// </summary>
    public IReadOnlyList<RegelnKategorie> VorteileGruppen
    {
        get => _vorteileGruppen;
        private set { _vorteileGruppen = value; Notify(); }
    }

    public IReadOnlyList<RegelnKategorie> NachteileGruppen
    {
        get => _nachteileGruppen;
        private set { _nachteileGruppen = value; Notify(); }
    }

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

    /// <summary>
    /// Versucht, den Editor zum Bearbeiten eines Eintrags zu öffnen.
    /// Liefert einen Fehlertext, wenn der Eintrag fest in C# implementiert ist.
    /// </summary>
    public string StartEdit(string vnId)
    {
        var vn = _vnService.FindById(vnId);
        if (vn is null) return "Eintrag nicht gefunden.";
        if (vn.HasCodeLogic)
            return $"„{vn.Name}\" ist fest in der App implementiert (Würfellogik / Spezialverhalten) und kann nicht editiert werden.";

        CurrentEdit = VorteilNachteilEditModel.FromCatalog(vn);
        EditorTitle = $"Bearbeiten: {vn.Name}";
        IsEditing = true;
        IsEditorVisible = true;
        return string.Empty;
    }

    /// <summary>Öffnet den Editor mit einer Kopie eines vorhandenen Eintrags (neue Id).</summary>
    public void StartCopy(string vnId)
    {
        var vn = _vnService.FindById(vnId);
        if (vn is null) return;

        var model = VorteilNachteilEditModel.FromCatalog(vn);
        model.Id = string.Empty; // Neue Id wird beim Speichern generiert
        model.Name = $"{vn.Name} (Kopie)";
        // Eine Kopie übernimmt nicht die Code-Logik des Originals — sie ist nur Daten.
        model.HasCodeLogic = false;
        model.IsHomebrew = true;
        CurrentEdit = model;
        EditorTitle = $"Kopie von: {vn.Name}";
        IsEditing = false;
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

            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] Pre-Persist (IsEditing={IsEditing})");
            if (IsEditing)
                await _vnService.UpdateUserEntryAsync(entry);
            else
                await _vnService.AddUserEntryAndPersistAsync(entry);
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] Post-Persist, ApplyFilter…");
            // ApplyFilter MUSS aufgerufen werden, solange die Katalog-CollectionViews noch
            // unsichtbar sind (IsListVisible=false). Sonst crasht MAUI/WinUI beim Clear+Add
            // auf grouped CollectionView (stowed exception 0xc000027b).
            ApplyFilter();
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] ApplyFilter done, hiding editor");
            IsEditorVisible = false;
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] Editor hidden");
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.SaveAsync] StackTrace: {ex.StackTrace}");
            return $"Fehler beim Speichern: {ex.Message}";
        }
    }

    /// <summary>Löscht einen Eintrag (offiziell oder Homebrew) aus dem Katalog.</summary>
    /// <returns>Leerer String bei Erfolg, sonst Fehlertext (z. B. bei Code-Logik-Einträgen).</returns>
    public async Task<string> DeleteAsync(string vnId)
    {
        var vn = _vnService.FindById(vnId);
        if (vn is null) return "Eintrag nicht gefunden.";
        if (vn.HasCodeLogic)
            return $"„{vn.Name}\" ist fest in der App implementiert und kann nicht gelöscht werden.";

        await _vnService.DeleteUserEntryAsync(vnId);
        ApplyFilter();
        return string.Empty;
    }

    /// <summary>Schließt den Editor ohne zu speichern.</summary>
    public void CancelEdit()
    {
        IsEditorVisible = false;
        // CurrentEdit nicht ersetzen — vermeidet Compiled-Binding-Crashs bei
        // BindableLayout/Picker. Nächstes StartCreate/StartEdit setzt eine neue Instanz.
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
        try
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
                IsHomebrew = vn.IsHomebrew,
                HasCodeLogic = vn.HasCodeLogic
            }).ToList();

            // Materialisieren BEVOR die ObservableCollections angefasst werden — so kann
            // eine Exception in BuildApInfo/BuildEffekteText nicht mitten im Clear/Add
            // zuschlagen und WinUI in einen inkonsistenten Zustand bringen.
            var vorteile = entries.Where(e => !e.IstNachteil)
                .GroupBy(e => e.Kategorie)
                .OrderBy(g => g.Key)
                .Select(g => new RegelnKategorie(g.Key, g.OrderBy(e => e.Name)))
                .ToList();

            var nachteile = entries.Where(e => e.IstNachteil)
                .GroupBy(e => e.Kategorie)
                .OrderBy(g => g.Key)
                .Select(g => new RegelnKategorie(g.Key, g.OrderBy(e => e.Name)))
                .ToList();

            VorteileGruppen = vorteile;
            NachteileGruppen = nachteile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.ApplyFilter] ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[RegelnVnViewModel.ApplyFilter] StackTrace: {ex.StackTrace}");
            throw;
        }
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
