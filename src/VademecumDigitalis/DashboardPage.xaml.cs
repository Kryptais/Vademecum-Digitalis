using VademecumDigitalis.Services;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly IServiceProvider _services;

    public DashboardPage(DashboardViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCharactersAsync();
    }

    private async void OnCharacterLoadClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharacterSummary summary)
        {
            var loaded = await _vm.LoadCharacterAsync(summary);
            if (loaded)
                NavigateToCharacterShell();
        }
    }

    private async void OnCharacterDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not CharacterSummary summary)
            return;

        bool confirmed = await DisplayAlert(
            "Charakter löschen",
            $"\"{summary.CharacterName}\" wirklich löschen?",
            "Löschen", "Abbrechen");

        if (confirmed)
            await _vm.DeleteCharacterAsync(summary);
    }

    private void OnNewCharacterClicked(object sender, EventArgs e)
    {
        _vm.CreateNewCharacter();
        NavigateToCharacterShell();
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        var imported = await _vm.ImportCharacterAsync();
        if (imported)
            await _vm.LoadCharactersAsync();
    }

    private async void OnRegelnClicked(object sender, EventArgs e)
    {
        var page = _services.GetRequiredService<RegelnPage>();
        await Navigation.PushAsync(page);
    }

    private async void OnEinstellungenClicked(object sender, EventArgs e)
    {
        var page = _services.GetRequiredService<EinstellungenPage>();
        await Navigation.PushAsync(page);
    }

    private void NavigateToCharacterShell()
    {
        if (Application.Current is App app)
            app.SwitchToCharacterShell(_services);
    }
}
