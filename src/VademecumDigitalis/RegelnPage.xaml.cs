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

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.CommandParameter is string id)
            {
                var error = _vm.VnTab.StartEdit(id);
                if (!string.IsNullOrEmpty(error))
                    await DisplayAlert("Bearbeiten nicht möglich", error, "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnEditClicked] CRASH: {ex}");
        }
    }

    private void OnCopyClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.CommandParameter is string id)
                _vm.VnTab.StartCopy(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnCopyClicked] CRASH: {ex}");
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn || btn.CommandParameter is not string id) return;

            bool confirmed = await DisplayAlert(
                "Eintrag löschen",
                "Diesen Eintrag wirklich löschen?",
                "Löschen", "Abbrechen");

            if (confirmed)
            {
                var error = await _vm.VnTab.DeleteAsync(id);
                if (!string.IsNullOrEmpty(error))
                    await DisplayAlert("Löschen nicht möglich", error, "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnDeleteClicked] CRASH: {ex}");
            await DisplayAlert("Unerwarteter Fehler", ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var error = await _vm.VnTab.SaveAsync();
            if (!string.IsNullOrEmpty(error))
                await DisplayAlert("Validierung", error, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] CRASH: {ex}");
            await DisplayAlert("Unerwarteter Fehler beim Speichern", ex.ToString(), "OK");
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _vm.VnTab.CancelEdit();
    }

    // --- Effekte ---

    private void OnAddEffectClicked(object? sender, EventArgs e)
    {
        try
        {
            _vm.VnTab.AddEffect();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnAddEffectClicked] CRASH: {ex}");
        }
    }

    private void OnRemoveEffectClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.CommandParameter is RuleEffectEditModel effect)
                _vm.VnTab.RemoveEffect(effect);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnRemoveEffectClicked] CRASH: {ex}");
        }
    }
}
