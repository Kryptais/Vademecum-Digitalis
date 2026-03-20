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
}
