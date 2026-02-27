using Microsoft.Maui.Controls;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis
{
    public partial class GlobalItemSearchPage : ContentPage
    {
        public GlobalItemSearchPage(GlobalItemSearchViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
