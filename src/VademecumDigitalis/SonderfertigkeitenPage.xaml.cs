using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class SonderfertigkeitenPage : ContentPage
{
    private readonly SonderfertigkeitenViewModel _vm;

    public SonderfertigkeitenPage(SonderfertigkeitenViewModel vm)
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
        if (sender is Button btn && btn.CommandParameter is SpecialAbility sf)
        {
            _vm.AddFromCatalog(sf);
        }
    }

    private void OnAddHomebrewClicked(object? sender, EventArgs e)
    {
        _vm.AddHomebrew();
    }

    private void OnLevelUpClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterSonderfertigkeitEintrag entry)
        {
            _vm.LevelUp(entry);
        }
    }

    private void OnEntfernenClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterSonderfertigkeitEintrag entry)
        {
            _vm.Remove(entry);
        }
    }
}
