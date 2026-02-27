using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis
{
    public partial class InventoryContainerPage : ContentPage
    {
        public InventoryContainerPage(InventoryContainerViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
