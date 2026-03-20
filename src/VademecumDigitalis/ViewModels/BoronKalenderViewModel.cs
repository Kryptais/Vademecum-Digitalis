using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

/// <summary>
/// ViewModel für die aventurische Kalender-Seite.
/// Zeigt den aktuellen Monat als Tagesraster, unterstützt Navigation und Notizen.
/// </summary>
public partial class BoronKalenderViewModel : ObservableObject
{
    private readonly PersistenceService _persistence;
    private CancellationTokenSource? _saveCts;

    // Das aktuelle Spielwelt-Datum (persistiert)
    [ObservableProperty]
    private BoronDatum _aktuellesDatum = BoronDatum.Default;

    // Der angezeigte Monat/Jahr für die Kalenderansicht
    [ObservableProperty]
    private int _angezeigtesJahr = 1040;

    [ObservableProperty]
    private int _angezeigterMonatIndex = 1;

    // Tage des aktuell angezeigten Monats
    public ObservableCollection<KalenderTag> AngezeigtesTage { get; } = [];

    // Alle Monatsnamen für den Picker
    public IReadOnlyList<string> MonatsNamen => BoronKalender.MonatsNamen;

    // Angezeigter Monatsname
    public string AngezeigterMonatName =>
        BoronKalender.GetMonat(AngezeigterMonatIndex)?.Name ?? "?";

    /// <summary>Gewählter Monatsname im Picker (für SelectedItem-Binding).</summary>
    public string AngezeigterMonatNamePicker
    {
        get => BoronKalender.GetMonat(AngezeigterMonatIndex)?.Name ?? "Praios";
        set
        {
            var monat = BoronKalender.GetMonat(value);
            if (monat != null && monat.Index != AngezeigterMonatIndex)
                AngezeigterMonatIndex = monat.Index;
        }
    }

    // Formatiertes aktuelles Datum
    public string AktuellesDatumText => AktuellesDatum.ToString();

    // Notizen
    [ObservableProperty]
    private string _notizen = string.Empty;

    public BoronKalenderViewModel(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task LoadDataAsync()
    {
        var data = await _persistence.LoadKalenderAsync();
        if (data != null)
        {
            AktuellesDatum = data.AktuellesDatum.IsValid ? data.AktuellesDatum : BoronDatum.Default;
            Notizen = data.Notizen ?? string.Empty;
        }

        AngezeigtesJahr = AktuellesDatum.Jahr;
        AngezeigterMonatIndex = AktuellesDatum.Monat;
        RebuildTage();
    }

    partial void OnAngezeigterMonatIndexChanged(int value)
    {
        OnPropertyChanged(nameof(AngezeigterMonatName));
        OnPropertyChanged(nameof(AngezeigterMonatNamePicker));
        RebuildTage();
    }

    partial void OnAngezeigtesJahrChanged(int value) => RebuildTage();

    partial void OnAktuellesDatumChanged(BoronDatum value)
    {
        OnPropertyChanged(nameof(AktuellesDatumText));
        RebuildTage(); // Markierung aktualisieren
        RequestDelayedSave();
    }

    partial void OnNotizenChanged(string value) => RequestDelayedSave();

    /// <summary>Baut die Tages-Liste für den angezeigten Monat/Jahr.</summary>
    private void RebuildTage()
    {
        AngezeigtesTage.Clear();
        var monat = BoronKalender.GetMonat(AngezeigterMonatIndex);
        if (monat == null) return;

        for (int t = 1; t <= monat.Tage; t++)
        {
            bool istHeute = (t == AktuellesDatum.Tag
                          && AngezeigterMonatIndex == AktuellesDatum.Monat
                          && AngezeigtesJahr == AktuellesDatum.Jahr);

            AngezeigtesTage.Add(new KalenderTag(t, istHeute));
        }
    }

    // --- Navigation ---

    [RelayCommand]
    private void VorherigerMonat()
    {
        if (AngezeigterMonatIndex > 1)
        {
            AngezeigterMonatIndex--;
        }
        else
        {
            AngezeigterMonatIndex = 13;
            AngezeigtesJahr--;
        }
    }

    [RelayCommand]
    private void NächsterMonat()
    {
        if (AngezeigterMonatIndex < 13)
        {
            AngezeigterMonatIndex++;
        }
        else
        {
            AngezeigterMonatIndex = 1;
            AngezeigtesJahr++;
        }
    }

    [RelayCommand]
    private void SpringeZuHeute()
    {
        AngezeigtesJahr = AktuellesDatum.Jahr;
        AngezeigterMonatIndex = AktuellesDatum.Monat;
    }

    // --- Aktuelles Datum setzen ---

    [RelayCommand]
    private void TagVor()
    {
        AktuellesDatum = AktuellesDatum.AddTage(1);
        AngezeigtesJahr = AktuellesDatum.Jahr;
        AngezeigterMonatIndex = AktuellesDatum.Monat;
    }

    [RelayCommand]
    private void TagZurück()
    {
        AktuellesDatum = AktuellesDatum.AddTage(-1);
        AngezeigtesJahr = AktuellesDatum.Jahr;
        AngezeigterMonatIndex = AktuellesDatum.Monat;
    }

    [RelayCommand]
    private void WocheVor()
    {
        AktuellesDatum = AktuellesDatum.AddTage(7);
        AngezeigtesJahr = AktuellesDatum.Jahr;
        AngezeigterMonatIndex = AktuellesDatum.Monat;
    }

    [RelayCommand]
    private void TagAuswählen(KalenderTag tag)
    {
        if (tag == null) return;
        AktuellesDatum = new BoronDatum(tag.Tag, AngezeigterMonatIndex, AngezeigtesJahr);
    }

    // --- Date Picker Command (für Geburtstag etc.) ---

    /// <summary>Picker-Ergebnis: gewählter Tag</summary>
    [ObservableProperty]
    private int _pickerTag = 1;

    /// <summary>Picker-Ergebnis: gewählter Monatsindex (1-basiert)</summary>
    [ObservableProperty]
    private int _pickerMonatIndex = 1;

    /// <summary>Picker-Ergebnis: gewähltes Jahr</summary>
    [ObservableProperty]
    private int _pickerJahr = 1040;

    /// <summary>Gewählter Monat im Datum-Picker (String für SelectedItem-Binding).</summary>
    public string PickerMonatName
    {
        get => BoronKalender.GetMonat(PickerMonatIndex)?.Name ?? "Praios";
        set
        {
            var monat = BoronKalender.GetMonat(value);
            if (monat != null && monat.Index != PickerMonatIndex)
                PickerMonatIndex = monat.Index;
        }
    }

    /// <summary>Maximale Tage im Picker-Monat.</summary>
    public int PickerMaxTage =>
        BoronKalender.GetMonat(PickerMonatIndex)?.Tage ?? 30;

    partial void OnPickerMonatIndexChanged(int value)
    {
        OnPropertyChanged(nameof(PickerMaxTage));
        OnPropertyChanged(nameof(PickerMonatName));
        OnPropertyChanged(nameof(PickerDatumText));
        if (PickerTag > PickerMaxTage)
            PickerTag = PickerMaxTage;
    }

    /// <summary>Formatiert das aktuell im Picker gewählte Datum.</summary>
    public string PickerDatumText =>
        new BoronDatum(PickerTag, PickerMonatIndex, PickerJahr).ToString();

    partial void OnPickerTagChanged(int value) => OnPropertyChanged(nameof(PickerDatumText));
    partial void OnPickerJahrChanged(int value) => OnPropertyChanged(nameof(PickerDatumText));

    /// <summary>Setzt den Picker auf ein vorhandenes Datum-String.</summary>
    public void SetPickerFromString(string? datumString)
    {
        if (BoronDatum.TryParse(datumString, out var d))
        {
            PickerTag = d.Tag;
            PickerMonatIndex = d.Monat;
            PickerJahr = d.Jahr;
        }
        else
        {
            PickerTag = AktuellesDatum.Tag;
            PickerMonatIndex = AktuellesDatum.Monat;
            PickerJahr = AktuellesDatum.Jahr;
        }
    }

    /// <summary>Liefert das aktuell im Picker eingestellte Datum als formatierten String.</summary>
    public string GetPickerResult() =>
        new BoronDatum(PickerTag, PickerMonatIndex, PickerJahr).ToString();

    // --- Persistenz ---

    private async Task SaveDataAsync()
    {
        try
        {
            var data = new KalenderData
            {
                AktuellesDatum = AktuellesDatum,
                Notizen = Notizen
            };
            await _persistence.SaveKalenderAsync(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving calendar: {ex.Message}");
        }
    }

    private void RequestDelayedSave()
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000, token);
                if (token.IsCancellationRequested) return;
                await SaveDataAsync();
            }
            catch (TaskCanceledException) { }
        });
    }
}

/// <summary>Einzelner Tag in der Kalenderansicht.</summary>
public partial class KalenderTag : ObservableObject
{
    public int Tag { get; }

    [ObservableProperty]
    private bool _istHeute;

    public KalenderTag(int tag, bool istHeute)
    {
        Tag = tag;
        IstHeute = istHeute;
    }
}
