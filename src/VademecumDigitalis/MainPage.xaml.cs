using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }
}
