using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class CombatTalentsPage : ContentPage
{
    public CombatTalentsPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }
}
