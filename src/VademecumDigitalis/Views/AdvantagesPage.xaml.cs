using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class AdvantagesPage : ContentPage
{
    private readonly VorteilNachteilViewModel _vm;
    private readonly IServiceProvider _services;

    public AdvantagesPage(VorteilNachteilViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
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

    private void OnLevelDownClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterVorteilNachteilEintrag entry)
        {
            _vm.LevelDown(entry);
        }
    }

    private void OnEntfernenClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterVorteilNachteilEintrag entry)
        {
            _vm.Remove(entry);
        }
    }

    private void OnDashboardClicked(object? sender, EventArgs e)
    {
        if (Application.Current is App app)
            app.SwitchToDashboard(_services);
    }
}
