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

    // Tage des aktuell angezeigten Monats (inkl. Leer-Kacheln für Wochenausrichtung)
    public ObservableCollection<KalenderTag> AngezeigtesTage { get; } = [];

    // Zeilen des Kalender-Rasters (je 7 Tage pro Zeile)
    public ObservableCollection<KalenderZeile> AngezeigteZeilen { get; } = [];

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

    // Formatiertes aktuelles Datum + Wochentag
    public string AktuellesDatumText =>
        AktuellesDatum.IsValid
            ? $"{AktuellesDatum}  ·  {BoronKalender.GetWochentagName(AktuellesDatum)}"
            : AktuellesDatum.ToString();

    // Notizen
    [ObservableProperty]
    private string _notizen = string.Empty;

    // Kalender-Einträge (Geburtstage, Feste, ...)
    public ObservableCollection<KalenderEintrag> Eintraege { get; } = [];

    // Ausgewählter Tag
    [ObservableProperty]
    private KalenderTag? _selectedTag;

    public ObservableCollection<KalenderEintrag> SelectedTagEintraege { get; } = [];

    public string SelectedTagText
    {
        get
        {
            if (SelectedTag == null || SelectedTag.IstLeer) return "Kein Tag ausgewählt – Tag antippen";
            var datum = new BoronDatum(SelectedTag.Tag, AngezeigterMonatIndex, AngezeigtesJahr);
            return $"{datum}  ·  {BoronKalender.GetWochentagName(datum)}";
        }
    }

    public BoronKalenderViewModel(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task LoadDataAsync()
    {
        var data = await _persistence.LoadKalenderAsync();

        // Gemeinsames Datum bevorzugen (gesetzt von Ereignisse-Tab oder beim Charakterladen)
        string? sharedDatum = null;
        try { sharedDatum = CharacterSheetSession.Current.AktuellesDatumStr; }
        catch (InvalidOperationException) { }

        if (!string.IsNullOrWhiteSpace(sharedDatum)
            && BoronDatum.TryParse(sharedDatum, out var charDatum)
            && charDatum.IsValid)
        {
            AktuellesDatum = charDatum;
        }
        else if (data != null)
        {
            AktuellesDatum = data.AktuellesDatum.IsValid ? data.AktuellesDatum : BoronDatum.Default;
        }

        if (data != null)
        {
            Notizen = data.Notizen ?? string.Empty;
            Eintraege.Clear();
            foreach (var e in data.Eintraege ?? [])
                Eintraege.Add(e);
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
        RebuildTage();
        // Gemeinsames Datum mit Ereignisse-Tab synchronisieren
        try { CharacterSheetSession.Current.AktuellesDatumStr = value.IsValid ? value.ToString() : string.Empty; }
        catch (InvalidOperationException) { }
        RequestDelayedSave();
    }

    partial void OnNotizenChanged(string value) => RequestDelayedSave();

    /// <summary>Baut die Tages-Liste für den angezeigten Monat/Jahr (7 Spalten = Wochentage).</summary>
    private void RebuildTage()
    {
        AngezeigtesTage.Clear();
        var monat = BoronKalender.GetMonat(AngezeigterMonatIndex);
        if (monat == null) return;

        // Leer-Kacheln vor dem 1. des Monats (Wochenausrichtung)
        var ersterTag = new BoronDatum(1, AngezeigterMonatIndex, AngezeigtesJahr);
        int startWochentag = BoronKalender.GetWochentagIndex(ersterTag);
        for (int i = 0; i < startWochentag; i++)
            AngezeigtesTage.Add(new KalenderTag(0, false, istLeer: true));

        for (int t = 1; t <= monat.Tage; t++)
        {
            var datum = new BoronDatum(t, AngezeigterMonatIndex, AngezeigtesJahr);
            bool istHeute = datum == AktuellesDatum;
            bool istPraiostag = BoronKalender.GetWochentagIndex(datum) == 3;
            bool hatEintrag = Eintraege.Any(e => e.TrifftAn(datum));
            AngezeigtesTage.Add(new KalenderTag(t, istHeute, istLeer: false, istPraiostag, hatEintrag));
        }

        // Auffüll-Kacheln am Ende für vollständige letzte Zeile
        int rest = AngezeigtesTage.Count % 7;
        if (rest != 0)
            for (int i = 0; i < 7 - rest; i++)
                AngezeigtesTage.Add(new KalenderTag(0, false, istLeer: true));

        // Zeilen-Collection für BindableLayout aufbauen
        AngezeigteZeilen.Clear();
        for (int i = 0; i < AngezeigtesTage.Count; i += 7)
        {
            AngezeigteZeilen.Add(new KalenderZeile(
                AngezeigtesTage[i],
                AngezeigtesTage[i + 1],
                AngezeigtesTage[i + 2],
                AngezeigtesTage[i + 3],
                AngezeigtesTage[i + 4],
                AngezeigtesTage[i + 5],
                AngezeigtesTage[i + 6]));
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
        if (tag == null || tag.IstLeer) return;
        AktuellesDatum = new BoronDatum(tag.Tag, AngezeigterMonatIndex, AngezeigtesJahr);
        SelectedTag = tag;
        OnPropertyChanged(nameof(SelectedTagText));
        AktualisiereSelectedTagEintraege();
    }

    private void AktualisiereSelectedTagEintraege()
    {
        SelectedTagEintraege.Clear();
        if (SelectedTag == null || SelectedTag.IstLeer) return;
        var datum = new BoronDatum(SelectedTag.Tag, AngezeigterMonatIndex, AngezeigtesJahr);
        foreach (var e in Eintraege.Where(e => e.TrifftAn(datum)))
            SelectedTagEintraege.Add(e);
    }

    /// <summary>Fügt einen Kalendereintrag hinzu und aktualisiert die Ansicht.</summary>
    public void EintragHinzufuegen(KalenderEintrag eintrag)
    {
        Eintraege.Add(eintrag);
        RebuildTage();
        AktualisiereSelectedTagEintraege();
        RequestDelayedSave();
    }

    /// <summary>Entfernt einen Kalendereintrag und aktualisiert die Ansicht.</summary>
    public void EintragEntfernen(KalenderEintrag eintrag)
    {
        Eintraege.Remove(eintrag);
        RebuildTage();
        AktualisiereSelectedTagEintraege();
        RequestDelayedSave();
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
                Notizen = Notizen,
                Eintraege = [.. Eintraege]
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
public class KalenderTag
{
    public int Tag { get; }
    public bool IstHeute { get; }
    public bool IstLeer { get; }
    public bool HatEintrag { get; }
    public bool ShowContent { get; }
    public Color TileBackground { get; }
    public Color TileBorder { get; }

    public KalenderTag(int tag, bool istHeute, bool istLeer = false, bool istPraiostag = false, bool hatEintrag = false)
    {
        Tag = tag;
        IstHeute = istHeute;
        IstLeer = istLeer;
        HatEintrag = hatEintrag;
        ShowContent = !istLeer;

        TileBackground = istLeer   ? Colors.Transparent
                       : istHeute  ? Color.FromArgb("#0E7490")
                       : istPraiostag ? Color.FromArgb("#1C2E1C")
                       : Color.FromArgb("#2A2A2A");

        TileBorder = istLeer   ? Colors.Transparent
                   : istHeute  ? Color.FromArgb("#14B8A6")
                   : istPraiostag ? Color.FromArgb("#2D5A2D")
                   : Color.FromArgb("#444444");
    }
}

/// <summary>Eine Zeile im Kalender-Raster (7 Tage nebeneinander).</summary>
public class KalenderZeile
{
    public KalenderTag Tag0 { get; }
    public KalenderTag Tag1 { get; }
    public KalenderTag Tag2 { get; }
    public KalenderTag Tag3 { get; }
    public KalenderTag Tag4 { get; }
    public KalenderTag Tag5 { get; }
    public KalenderTag Tag6 { get; }

    public KalenderZeile(KalenderTag t0, KalenderTag t1, KalenderTag t2, KalenderTag t3,
                         KalenderTag t4, KalenderTag t5, KalenderTag t6)
    {
        Tag0 = t0; Tag1 = t1; Tag2 = t2; Tag3 = t3;
        Tag4 = t4; Tag5 = t5; Tag6 = t6;
    }
}
