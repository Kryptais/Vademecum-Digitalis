using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis
{
    public partial class InventoryDashboardPage : ContentPage
    {
        public InventoryDashboardPage(InventoryViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            
            // Trigger initial load manually or ensure VM does it
            // We can check if containers are empty and then load
            MainThread.BeginInvokeOnMainThread(async () => await vm.LoadDataAsync());
        }
    }
}
