using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis
{
    public partial class MoneyTransferPage : ContentPage
    {
        // Public properties to access result
        public bool Confirmed => (BindingContext as MoneyTransferViewModel)?.Confirmed ?? false;
        public int Dukaten => (BindingContext as MoneyTransferViewModel)?.Dukaten ?? 0;
        public int Silbertaler => (BindingContext as MoneyTransferViewModel)?.Silbertaler ?? 0;
        public int Heller => (BindingContext as MoneyTransferViewModel)?.Heller ?? 0;
        public int Kreuzer => (BindingContext as MoneyTransferViewModel)?.Kreuzer ?? 0;

        public MoneyTransferPage(Models.InventoryContainer source)
        {
            InitializeComponent();
            var vm = new MoneyTransferViewModel(source);
            BindingContext = vm;

            vm.RequestClose += (s, e) =>
            {
                Navigation.PopModalAsync();
            };
        }
    }
}
