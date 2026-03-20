using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }

    private async void OnGeburtstagPickerClicked(object? sender, EventArgs e)
    {
        var vm = BindingContext as MainPageViewModel;
        if (vm == null) return;

        var picker = new DsaDatePickerPage();
        picker.SetInitialDatum(vm.Geburtstag);

        picker.Disappearing += (s, args) =>
        {
            if (picker.Confirmed && picker.ResultDatum != null)
            {
                vm.Geburtstag = picker.ResultDatum;
            }
        };

        await Navigation.PushModalAsync(new NavigationPage(picker));
    }
}
