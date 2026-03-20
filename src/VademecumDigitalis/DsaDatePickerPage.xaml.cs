using VademecumDigitalis.Models;

namespace VademecumDigitalis;

/// <summary>
/// Modale Seite zum Wählen eines aventurischen Datums (DSA-Kalender).
/// Wird z.B. vom Geburtstags-Picker auf dem Hauptblatt aufgerufen.
/// </summary>
public partial class DsaDatePickerPage : ContentPage
{
    private int _tag = 1;
    private int _monatIndex = 0; // 0-basiert für Picker
    private int _jahr = 1040;

    /// <summary>Ergebnis-String nach Bestätigung, z.B. "12. Rahja 1038 BF".</summary>
    public string? ResultDatum { get; private set; }

    /// <summary>True wenn der User bestätigt hat.</summary>
    public bool Confirmed { get; private set; }

    public DsaDatePickerPage()
    {
        InitializeComponent();

        // Monate befüllen
        foreach (var name in BoronKalender.MonatsNamen)
            MonatPicker.Items.Add(name);

        MonatPicker.SelectedIndex = 0;
    }

    /// <summary>Setzt den Picker auf einen existierenden Datum-String.</summary>
    public void SetInitialDatum(string? datumString)
    {
        if (BoronDatum.TryParse(datumString, out var d))
        {
            _tag = d.Tag;
            _monatIndex = d.Monat - 1; // 0-basiert
            _jahr = d.Jahr;
        }
        else
        {
            _tag = 1;
            _monatIndex = 0;
            _jahr = 1040;
        }

        TagStepper.Value = _tag;
        TagLabel.Text = _tag.ToString();
        MonatPicker.SelectedIndex = _monatIndex;
        JahrEntry.Text = _jahr.ToString();
        UpdateMaxTag();
        UpdateVorschau();
    }

    private void OnTagChanged(object? sender, ValueChangedEventArgs e)
    {
        _tag = (int)e.NewValue;
        TagLabel.Text = _tag.ToString();
        UpdateVorschau();
    }

    private void OnMonatChanged(object? sender, EventArgs e)
    {
        _monatIndex = MonatPicker.SelectedIndex;
        if (_monatIndex < 0) _monatIndex = 0;
        UpdateMaxTag();
        UpdateVorschau();
    }

    private void OnJahrChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int j))
            _jahr = j;
        UpdateVorschau();
    }

    private void UpdateMaxTag()
    {
        var monat = BoronKalender.GetMonat(_monatIndex + 1);
        int max = monat?.Tage ?? 30;
        TagStepper.Maximum = max;
        if (_tag > max)
        {
            _tag = max;
            TagStepper.Value = _tag;
            TagLabel.Text = _tag.ToString();
        }
    }

    private void UpdateVorschau()
    {
        var datum = new BoronDatum(_tag, _monatIndex + 1, _jahr);
        VorschauLabel.Text = datum.ToString();
    }

    private async void OnCancel(object? sender, EventArgs e)
    {
        Confirmed = false;
        ResultDatum = null;
        await Navigation.PopModalAsync();
    }

    private async void OnConfirm(object? sender, EventArgs e)
    {
        Confirmed = true;
        var datum = new BoronDatum(_tag, _monatIndex + 1, _jahr);
        ResultDatum = datum.ToString();
        await Navigation.PopModalAsync();
    }
}
