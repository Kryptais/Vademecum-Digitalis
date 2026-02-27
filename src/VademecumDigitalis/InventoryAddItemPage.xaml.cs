using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis
{
    public partial class InventoryAddItemPage : ContentPage
    {
        // Public property to access the result after modal close
        public InventoryItem? ResultItem => (BindingContext as InventoryAddItemViewModel)?.ResultItem;

        public InventoryAddItemPage()
        {
            InitializeComponent();
            var vm = new InventoryAddItemViewModel();
            BindingContext = vm;

            // Simple event handling for closing logic triggered by VM
            vm.RequestClose += (s, e) => 
            {
                Navigation.PopModalAsync();
            };
        }

        public void SetEditingItem(InventoryItem item)
        {
            if (BindingContext is InventoryAddItemViewModel vm)
            {
                vm.SetEditingItem(item);
            }
        }
    }
}
