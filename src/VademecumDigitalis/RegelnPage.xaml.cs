using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class RegelnPage : ContentPage
{
    private readonly RegelnViewModel _vm;

    public RegelnPage(RegelnViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        UpdateTabStyles();
    }

    // --- Tab-Umschaltung ---

    private void OnTabVnClicked(object? sender, EventArgs e) { _vm.SelectTab(0); UpdateTabStyles(); }
    private void OnTabSfClicked(object? sender, EventArgs e) { _vm.SelectTab(1); UpdateTabStyles(); }
    private void OnTabMagieClicked(object? sender, EventArgs e) { _vm.SelectTab(2); UpdateTabStyles(); }
    private void OnTabGoetterwirkenClicked(object? sender, EventArgs e) { _vm.SelectTab(3); UpdateTabStyles(); }

    private void UpdateTabStyles()
    {
        var buttons = new[] { TabVnBtn, TabSfBtn, TabMagieBtn, TabGoetterwirkenBtn };
        for (int i = 0; i < buttons.Length; i++)
        {
            var active = i == _vm.SelectedTabIndex;
            buttons[i].BackgroundColor = active
                ? Color.FromArgb("#0E7490")
                : Color.FromArgb("#2A2A3E");
            buttons[i].TextColor = active
                ? Colors.White
                : Color.FromArgb("#B0B0B0");
        }
    }

    // --- CRUD ---

    private void OnCreateClicked(object? sender, EventArgs e)
    {
        _vm.VnTab.StartCreate();
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string id)
            _vm.VnTab.StartEdit(id);
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string id) return;

        bool confirmed = await DisplayAlert(
            "Eintrag löschen",
            "Diesen Homebrew-Eintrag wirklich löschen?",
            "Löschen", "Abbrechen");

        if (confirmed)
            await _vm.VnTab.DeleteAsync(id);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var error = await _vm.VnTab.SaveAsync();
        if (!string.IsNullOrEmpty(error))
            await DisplayAlert("Validierung", error, "OK");
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _vm.VnTab.CancelEdit();
    }

    // --- Effekte ---

    private void OnAddEffectClicked(object? sender, EventArgs e)
    {
        _vm.VnTab.AddEffect();
    }

    private void OnRemoveEffectClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is RuleEffectEditModel effect)
            _vm.VnTab.RemoveEffect(effect);
    }
}
