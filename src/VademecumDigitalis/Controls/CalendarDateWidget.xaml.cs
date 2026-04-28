using Microsoft.Extensions.DependencyInjection;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis.Controls;

public partial class CalendarDateWidget : ContentView
{
    private bool _isExpanded = true;
    private bool _loaded;

    public CalendarDateWidget()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (BindingContext is null)
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            BindingContext = services?.GetService<BoronKalenderViewModel>();
        }

        if (_loaded || BindingContext is not BoronKalenderViewModel vm)
            return;

        _loaded = true;
        await vm.LoadDataAsync();
    }

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        _isExpanded = !_isExpanded;
        ExpandedContent.IsVisible = _isExpanded;
        ToggleButton.Text = _isExpanded ? "▾" : "▸";
    }
}
