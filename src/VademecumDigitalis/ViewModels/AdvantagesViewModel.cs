using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// ViewModel für die AdvantagesPage (Vorteile/Nachteile).
/// Verwaltet strukturierte Einträge, Katalogzugriff und AP-Berechnung.
/// </summary>
public class AdvantagesViewModel : INotifyPropertyChanged
{
    private readonly AdvantagesService _service;
    private CancellationTokenSource? _saveCts;

    // ── Eingabefelder für neuen Eintrag ──────────────────────────────────────────

    private string _neuerVorteilName = string.Empty;
    private int _neuerVorteilStufe;
    private string _neuerVorteilAnmerkung = string.Empty;

    private string _neuerNachteilName = string.Empty;
    private int _neuerNachteilStufe;
    private string _neuerNachteilAnmerkung = string.Empty;

    public AdvantagesViewModel(AdvantagesService service)
    {
        _service = service;

        Vorteile = [];
        Nachteile = [];

        Vorteile.CollectionChanged += (_, _) => { NotifyApProperties(); RequestDelayedSave(); };
        Nachteile.CollectionChanged += (_, _) => { NotifyApProperties(); RequestDelayedSave(); };

        VorteilHinzufuegenCommand = new RelayCommand(_ => VorteilHinzufuegen(), _ => !string.IsNullOrWhiteSpace(NeuerVorteilName));
        VorteilEntfernenCommand = new RelayCommand(param => VorteilEntfernen(param as Vorteil));
        NachteilHinzufuegenCommand = new RelayCommand(_ => NachteilHinzufuegen(), _ => !string.IsNullOrWhiteSpace(NeuerNachteilName));
        NachteilEntfernenCommand = new RelayCommand(param => NachteilEntfernen(param as Vorteil));
        VorteilKatalogWaehlenCommand = new RelayCommand(param => VorteilAusKatalogWaehlen(param as string));
        NachteilKatalogWaehlenCommand = new RelayCommand(param => NachteilAusKatalogWaehlen(param as string));
    }

    // ── Collections ──────────────────────────────────────────────────────────────

    public ObservableCollection<Vorteil> Vorteile { get; }
    public ObservableCollection<Vorteil> Nachteile { get; }

    // ── Eingabefelder: Neuer Vorteil ─────────────────────────────────────────────

    public string NeuerVorteilName
    {
        get => _neuerVorteilName;
        set
        {
            if (SetField(ref _neuerVorteilName, value))
            {
                (VorteilHinzufuegenCommand as RelayCommand)?.RaiseCanExecuteChanged();

                // Stufe aus Katalog vorbelegen
                var eintrag = VorteilKatalog.FindByName(value);
                if (eintrag is { MaxStufe: > 0 } && NeuerVorteilStufe == 0)
                    NeuerVorteilStufe = 1;
            }
        }
    }

    public int NeuerVorteilStufe
    {
        get => _neuerVorteilStufe;
        set => SetField(ref _neuerVorteilStufe, value);
    }

    public string NeuerVorteilAnmerkung
    {
        get => _neuerVorteilAnmerkung;
        set => SetField(ref _neuerVorteilAnmerkung, value);
    }

    // ── Eingabefelder: Neuer Nachteil ────────────────────────────────────────────

    public string NeuerNachteilName
    {
        get => _neuerNachteilName;
        set
        {
            if (SetField(ref _neuerNachteilName, value))
            {
                (NachteilHinzufuegenCommand as RelayCommand)?.RaiseCanExecuteChanged();

                var eintrag = VorteilKatalog.FindByName(value);
                if (eintrag is { MaxStufe: > 0 } && NeuerNachteilStufe == 0)
                    NeuerNachteilStufe = 1;
            }
        }
    }

    public int NeuerNachteilStufe
    {
        get => _neuerNachteilStufe;
        set => SetField(ref _neuerNachteilStufe, value);
    }

    public string NeuerNachteilAnmerkung
    {
        get => _neuerNachteilAnmerkung;
        set => SetField(ref _neuerNachteilAnmerkung, value);
    }

    // ── Katalogvorschläge ─────────────────────────────────────────────────────────

    public IReadOnlyList<string> VorteilVorschlaege => VorteilKatalog.VorteilNamen;
    public IReadOnlyList<string> NachteilVorschlaege => VorteilKatalog.NachteilNamen;

    // ── AP-Anzeige ────────────────────────────────────────────────────────────────

    public int VorteilApKosten => _service.BerechneApKosten(Vorteile);
    public int NachteilApEinnahmen => _service.BerechneApEinnahmen(Nachteile);

    /// <summary>AP-Saldo: Einnahmen durch Nachteile minus Kosten durch Vorteile.</summary>
    public int ApSaldo => NachteilApEinnahmen - VorteilApKosten;

    // ── Befehle ───────────────────────────────────────────────────────────────────

    public ICommand VorteilHinzufuegenCommand { get; }
    public ICommand VorteilEntfernenCommand { get; }
    public ICommand NachteilHinzufuegenCommand { get; }
    public ICommand NachteilEntfernenCommand { get; }
    public ICommand VorteilKatalogWaehlenCommand { get; }
    public ICommand NachteilKatalogWaehlenCommand { get; }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────────

    private void VorteilHinzufuegen()
    {
        if (string.IsNullOrWhiteSpace(NeuerVorteilName)) return;

        Vorteile.Add(new Vorteil
        {
            Name = NeuerVorteilName.Trim(),
            Stufe = NeuerVorteilStufe,
            Anmerkung = NeuerVorteilAnmerkung.Trim(),
            IstNachteil = false
        });

        NeuerVorteilName = string.Empty;
        NeuerVorteilStufe = 0;
        NeuerVorteilAnmerkung = string.Empty;
    }

    private void VorteilEntfernen(Vorteil? vorteil)
    {
        if (vorteil != null)
            Vorteile.Remove(vorteil);
    }

    private void NachteilHinzufuegen()
    {
        if (string.IsNullOrWhiteSpace(NeuerNachteilName)) return;

        Nachteile.Add(new Vorteil
        {
            Name = NeuerNachteilName.Trim(),
            Stufe = NeuerNachteilStufe,
            Anmerkung = NeuerNachteilAnmerkung.Trim(),
            IstNachteil = true
        });

        NeuerNachteilName = string.Empty;
        NeuerNachteilStufe = 0;
        NeuerNachteilAnmerkung = string.Empty;
    }

    private void NachteilEntfernen(Vorteil? nachteil)
    {
        if (nachteil != null)
            Nachteile.Remove(nachteil);
    }

    private void VorteilAusKatalogWaehlen(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        NeuerVorteilName = name;
        var eintrag = VorteilKatalog.FindByName(name);
        if (eintrag is { MaxStufe: > 0 })
            NeuerVorteilStufe = 1;
    }

    private void NachteilAusKatalogWaehlen(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        NeuerNachteilName = name;
        var eintrag = VorteilKatalog.FindByName(name);
        if (eintrag is { MaxStufe: > 0 })
            NeuerNachteilStufe = 1;
    }

    private void NotifyApProperties()
    {
        OnPropertyChanged(nameof(VorteilApKosten));
        OnPropertyChanged(nameof(NachteilApEinnahmen));
        OnPropertyChanged(nameof(ApSaldo));
    }

    // ── Daten laden ───────────────────────────────────────────────────────────────

    public async Task LoadDataAsync()
    {
        var data = await _service.LoadAsync();

        Vorteile.Clear();
        foreach (var v in data.Vorteile)
            Vorteile.Add(v);

        Nachteile.Clear();
        foreach (var n in data.Nachteile)
            Nachteile.Add(n);
    }

    // ── Verzögertes Speichern ─────────────────────────────────────────────────────

    private void RequestDelayedSave()
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1500, token);
                if (token.IsCancellationRequested) return;

                await _service.SaveAsync(Vorteile, Nachteile);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvantagesViewModel] Fehler beim Speichern: {ex.Message}");
            }
        });
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
