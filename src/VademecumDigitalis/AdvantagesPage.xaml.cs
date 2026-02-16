using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class AdvantagesPage : ContentPage
{
    public AdvantagesPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }
}
