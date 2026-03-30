using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class BoronKalenderPage : ContentPage
{
    public BoronKalenderPage(BoronKalenderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BoronKalenderViewModel vm)
            await vm.LoadDataAsync();
    }

    private void OnEintragHinzufuegen(object? sender, EventArgs e)
    {
        if (BindingContext is not BoronKalenderViewModel vm) return;
        if (vm.SelectedTag == null || vm.SelectedTag.IstLeer)
        {
            DisplayAlert("Kein Tag gewählt", "Bitte erst einen Tag im Kalender antippen.", "OK");
            return;
        }
        var titel = NeuEintragTitelEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(titel))
        {
            DisplayAlert("Kein Titel", "Bitte einen Titel eingeben.", "OK");
            return;
        }

        var eintrag = new KalenderEintrag
        {
            Titel = titel,
            EintragTag = vm.SelectedTag.Tag,
            EintragMonat = vm.AngezeigterMonatIndex,
            EintragJahr = vm.AngezeigtesJahr,
            IstJaehrlich = NeuEintragJaehrlichSwitch.IsToggled
        };

        vm.EintragHinzufuegen(eintrag);
        NeuEintragTitelEntry.Text = string.Empty;
    }

    private void OnEintragLoeschen(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is KalenderEintrag eintrag
            && BindingContext is BoronKalenderViewModel vm)
        {
            vm.EintragEntfernen(eintrag);
        }
    }
}
