using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class TalentsPage : ContentPage
{
    public TalentsPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }
}
