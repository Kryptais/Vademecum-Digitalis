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
        if (_vm.Gruppen.Count == 0)
            await _vm.LoadAsync();
    }
}
