using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class AdvantagesPage : ContentPage
{
    private readonly VorteilNachteilViewModel _vm;

    public AdvantagesPage(VorteilNachteilViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    private void OnToggleSearchClicked(object? sender, EventArgs e)
    {
        _vm.ToggleSearch();
    }

    private async void OnAddFromCatalogClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is VorteilNachteil vn)
        {
            await _vm.AddFromCatalogAsync(vn, ShowTalentPickerAsync);
        }
    }

    private async Task<string?> ShowTalentPickerAsync(string[] talente, string title)
    {
        var result = await DisplayActionSheet(title, "Abbrechen", null, talente);
        return result == "Abbrechen" || result == null ? null : result;
    }

    private void OnLevelUpClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterVorteilNachteilEintrag entry)
        {
            _vm.LevelUp(entry);
        }
    }

    private void OnEntfernenClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterVorteilNachteilEintrag entry)
        {
            _vm.Remove(entry);
        }
    }
}
