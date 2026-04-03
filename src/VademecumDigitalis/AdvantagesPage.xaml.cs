using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class AdvantagesPage : ContentPage
{
    private readonly AdvantagesViewModel _viewModel;

    public AdvantagesPage(AdvantagesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}
