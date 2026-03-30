using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class EreignissePage : ContentPage
{
    private MainPageViewModel Vm => CharacterSheetSession.Current;
    private CharakterEreignis? _editingEreignis;

    public EreignissePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = Vm;
        RefreshUI();
        NeuDatumEntry.Text = Vm.AktuellesDatumStr;
    }

    private void RefreshUI()
    {
        AktuellesDatumLabel.Text = string.IsNullOrWhiteSpace(Vm.AktuellesDatumStr)
            ? "Noch kein Datum gesetzt"
            : Vm.AktuellesDatumStr;

        GeburtstagLabel.Text = string.IsNullOrWhiteSpace(Vm.Geburtstag)
            ? "—"
            : Vm.Geburtstag;

        AlterBerechnetLabel.Text = Vm.AlterBerechnet;

        int bonus = Vm.EreignisAlterBonus;
        EreignisAlterBonusLabel.Text = bonus == 0 ? "0 Jahre" : $"{bonus:+#;-#;0} Jahre";
        EreignisAlterBonusLabel.TextColor = bonus > 0
            ? Color.FromArgb("#DC2626")
            : bonus < 0
                ? Color.FromArgb("#059669")
                : Color.FromArgb("#111827");

        SchiPGesamtSpan.Text = Vm.SchicksalspunkteGesamt.ToString();
    }

    private async void OnDatumAendern(object? sender, EventArgs e)
    {
        var picker = new DsaDatePickerPage();
        picker.SetInitialDatum(Vm.AktuellesDatumStr);
        await Navigation.PushModalAsync(picker);

        // Warte auf Rückkehr
        picker.Disappearing += (_, _) =>
        {
            if (picker.Confirmed && picker.ResultDatum != null)
            {
                Vm.AktuellesDatumStr = picker.ResultDatum;
                RefreshUI();
            }
        };
    }

    private async void OnNeuDatumPicker(object? sender, EventArgs e)
    {
        var picker = new DsaDatePickerPage();
        picker.SetInitialDatum(NeuDatumEntry.Text);
        await Navigation.PushModalAsync(picker);

        picker.Disappearing += (_, _) =>
        {
            if (picker.Confirmed && picker.ResultDatum != null)
            {
                NeuDatumEntry.Text = picker.ResultDatum;
            }
        };
    }

    private void OnEreignisHinzufuegen(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NeuBeschreibungEditor.Text) &&
            string.IsNullOrWhiteSpace(NeuDatumEntry.Text))
        {
            DisplayAlert("Fehler", "Bitte mindestens ein Datum oder eine Beschreibung eingeben.", "OK");
            return;
        }

        if (_editingEreignis != null)
        {
            _editingEreignis.DatumStr = NeuDatumEntry.Text?.Trim() ?? string.Empty;
            _editingEreignis.Beschreibung = NeuBeschreibungEditor.Text?.Trim() ?? string.Empty;
            _editingEreignis.AlterBonus = ParseInt(NeuAlterEntry.Text);
            _editingEreignis.SchicksalspunkteBonus = ParseInt(NeuSchiPEntry.Text);
            _editingEreignis.LepBonus = ParseInt(NeuLepEntry.Text);
            _editingEreignis.AspBonus = ParseInt(NeuAspEntry.Text);
            _editingEreignis.KapBonus = ParseInt(NeuKapEntry.Text);
            _editingEreignis.SkBonus = ParseInt(NeuSkEntry.Text);
            _editingEreignis.ZkBonus = ParseInt(NeuZkEntry.Text);
            ResetFormModus();
        }
        else
        {
            var ereignis = new CharakterEreignis
            {
                DatumStr = NeuDatumEntry.Text?.Trim() ?? string.Empty,
                Beschreibung = NeuBeschreibungEditor.Text?.Trim() ?? string.Empty,
                AlterBonus = ParseInt(NeuAlterEntry.Text),
                SchicksalspunkteBonus = ParseInt(NeuSchiPEntry.Text),
                LepBonus = ParseInt(NeuLepEntry.Text),
                AspBonus = ParseInt(NeuAspEntry.Text),
                KapBonus = ParseInt(NeuKapEntry.Text),
                SkBonus = ParseInt(NeuSkEntry.Text),
                ZkBonus = ParseInt(NeuZkEntry.Text)
            };

            Vm.EreignisHinzufuegen(ereignis);
            ResetFormModus();
        }

        RefreshUI();
    }

    private void OnEreignisBearbeiten(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterEreignis ereignis)
        {
            _editingEreignis = ereignis;
            NeuDatumEntry.Text = ereignis.DatumStr;
            NeuBeschreibungEditor.Text = ereignis.Beschreibung;
            NeuAlterEntry.Text = ereignis.AlterBonus.ToString();
            NeuSchiPEntry.Text = ereignis.SchicksalspunkteBonus.ToString();
            NeuLepEntry.Text = ereignis.LepBonus.ToString();
            NeuAspEntry.Text = ereignis.AspBonus.ToString();
            NeuKapEntry.Text = ereignis.KapBonus.ToString();
            NeuSkEntry.Text = ereignis.SkBonus.ToString();
            NeuZkEntry.Text = ereignis.ZkBonus.ToString();
            FormModusLabel.Text = "Ereignis bearbeiten";
            HinzufuegenButton.Text = "✔ Änderungen speichern";
            AbrechenButton.IsVisible = true;
        }
    }

    private void OnBearbeitenAbbrechen(object? sender, EventArgs e)
    {
        ResetFormModus();
    }

    private void ResetFormModus()
    {
        _editingEreignis = null;
        FormModusLabel.Text = "Neues Ereignis hinzufügen";
        HinzufuegenButton.Text = "✚ Ereignis hinzufügen";
        AbrechenButton.IsVisible = false;
        NeuDatumEntry.Text = string.Empty;
        NeuBeschreibungEditor.Text = string.Empty;
        NeuAlterEntry.Text = "0";
        NeuSchiPEntry.Text = "0";
        NeuLepEntry.Text = "0";
        NeuAspEntry.Text = "0";
        NeuKapEntry.Text = "0";
        NeuSkEntry.Text = "0";
        NeuZkEntry.Text = "0";
    }

    private void OnEreignisLoeschen(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterEreignis ereignis)
        {
            Vm.EreignisEntfernen(ereignis);
            RefreshUI();
        }
    }

    private async void OnSchiPVerbrannt(object? sender, EventArgs e)
    {
        var ereignis = new CharakterEreignis
        {
            DatumStr = Vm.AktuellesDatumStr,
            Beschreibung = "Schicksalspunkt verbrannt",
            SchicksalspunkteBonus = -1
        };
        Vm.EreignisHinzufuegen(ereignis);
        RefreshUI();
        await DisplayAlert("Info", "1 Schicksalspunkt dauerhaft verbrannt eingetragen.", "OK");
    }

    private async void OnSchiPGeschenkt(object? sender, EventArgs e)
    {
        var ereignis = new CharakterEreignis
        {
            DatumStr = Vm.AktuellesDatumStr,
            Beschreibung = "Schicksalspunkt erhalten",
            SchicksalspunkteBonus = +1
        };
        Vm.EreignisHinzufuegen(ereignis);
        RefreshUI();
        await DisplayAlert("Info", "1 Schicksalspunkt erhalten eingetragen.", "OK");
    }

    private async void OnSchiPManuell(object? sender, EventArgs e)
    {
        var input = await DisplayPromptAsync(
            "Schicksalspunkte",
            "Änderung eingeben (z. B. -2 oder +3):",
            keyboard: Keyboard.Numeric,
            initialValue: "0");

        if (input == null) return;
        int wert = ParseInt(input);
        if (wert == 0) return;

        var ereignis = new CharakterEreignis
        {
            DatumStr = Vm.AktuellesDatumStr,
            Beschreibung = wert > 0 ? $"{wert} Schicksalspunkte erhalten" : $"{Math.Abs(wert)} Schicksalspunkte verbrannt",
            SchicksalspunkteBonus = wert
        };
        Vm.EreignisHinzufuegen(ereignis);
        RefreshUI();
    }

    private static int ParseInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return int.TryParse(text.Trim(), out int v) ? v : 0;
    }
}
