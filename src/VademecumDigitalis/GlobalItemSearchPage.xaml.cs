using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis
{
    public partial class GlobalItemSearchPage : ContentPage
    {
        public ObservableCollection<GlobalSearchResult> SearchResults { get; } = new ObservableCollection<GlobalSearchResult>();
        private InventoryViewModel _vm;

        public GlobalItemSearchPage()
        {
            InitializeComponent();
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _vm = services?.GetService(typeof(InventoryViewModel)) as InventoryViewModel;
            
            ResultsCollectionView.ItemsSource = SearchResults;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            SearchResults.Clear();
            if (string.IsNullOrWhiteSpace(e.NewTextValue) || _vm == null) return;

            var term = e.NewTextValue;
            foreach (var container in _vm.Containers)
            {
                var matches = container.Items.Where(i => i.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || 
                                                       (i.Details != null && i.Details.Contains(term, StringComparison.OrdinalIgnoreCase)));
                
                foreach (var item in matches)
                {
                    SearchResults.Add(new GlobalSearchResult(item, container));
                }
            }
        }

        private async void OnItemTapped(object sender, EventArgs e)
        {
             if (sender is Element elem && elem.BindingContext is GlobalSearchResult res)
             {
                 // Navigate to the container page
                 await Navigation.PushAsync(new InventoryContainerPage(res.Container));
             }
        }
    }

    public class GlobalSearchResult
    {
        public InventoryItem Item { get; }
        public InventoryContainer Container { get; }
        
        public string ItemName => Item.Name;
        public string ContainerName => Container.Name;
        public string QuantityText => $"{Item.Quantity}x";

        public GlobalSearchResult(InventoryItem item, InventoryContainer container)
        {
            Item = item;
            Container = container;
        }
    }
}
