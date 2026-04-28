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

    private void OnAddFromCatalogClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is VorteilNachteil vn)
        {
            _vm.AddFromCatalog(vn);
        }
    }

    private void OnAddHomebrewClicked(object? sender, EventArgs e)
    {
        _vm.AddHomebrew();
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
