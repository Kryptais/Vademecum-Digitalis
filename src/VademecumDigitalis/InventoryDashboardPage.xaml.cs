using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace VademecumDigitalis
{
    public partial class InventoryDashboardPage : ContentPage
    {
        InventoryViewModel VM => BindingContext as InventoryViewModel ?? throw new System.InvalidOperationException("VM not resolved");

        public InventoryDashboardPage()
        {
            InitializeComponent();
            // resolve shared VM from DI via the current Application's MauiContext services
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            BindingContext = services?.GetService(typeof(InventoryViewModel)) as InventoryViewModel;

            UpdateGlobalBankLabel();
            UpdateTotalWeightsLabel();

            // subscribe to containers collection changes to update bank and weights
            VM.Containers.CollectionChanged += Containers_CollectionChanged;

            // subscribe existing containers
            foreach (var c in VM.Containers)
            {
                SubscribeContainer(c);
            }
        }

        private void Containers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InventoryContainer nc in e.NewItems)
                {
                    SubscribeContainer(nc);
                }
            }
            if (e.OldItems != null)
            {
                foreach (InventoryContainer oc in e.OldItems)
                {
                    UnsubscribeContainer(oc);
                }
            }
            UpdateGlobalBankLabel();
            UpdateTotalWeightsLabel();
        }

        private void SubscribeContainer(InventoryContainer c)
        {
            if (c == null) return;
            c.PropertyChanged += Container_PropertyChanged;
            // also ensure money changes bubble up — InventoryContainer already does this and raises TotalWeight
            c.Money.PropertyChanged += (s, e) => { UpdateGlobalBankLabel(); UpdateTotalWeightsLabel(); };
            // subscribe existing items changes handled inside InventoryContainer
        }

        private void UnsubscribeContainer(InventoryContainer c)
        {
            if (c == null) return;
            c.PropertyChanged -= Container_PropertyChanged;
        }

        private void Container_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // respond to changes affecting totals
            if (e.PropertyName == nameof(InventoryContainer.TotalWeight) ||
                e.PropertyName == nameof(InventoryContainer.IsCarried) ||
                e.PropertyName == nameof(InventoryContainer.IncludeCoinWeight))
            {
                UpdateTotalWeightsLabel();
            }
            // money changes are handled separately for the global bank labels
            UpdateGlobalBankLabel();
        }

        void UpdateGlobalBankLabel()
        {
            // sum all containers money and show breakdown
            long duk = 0, sil = 0, hel = 0, kre = 0;
            foreach (var c in VM.Containers)
            {
                duk += c.Money.Dukaten;
                sil += c.Money.Silbertaler;
                hel += c.Money.Heller;
                kre += c.Money.Kreuzer;
            }
            DukatenLabel.Text = duk.ToString();
            SilbertalerLabel.Text = sil.ToString();
            HellerLabel.Text = hel.ToString();
            KreuzerLabel.Text = kre.ToString();
        }

        void UpdateTotalWeightsLabel()
        {
            // Gesamtgewicht aller Inventare (berücksichtigt für jeden Container die IncludeCoinWeight-Einstellung)
            double totalAll = VM.Containers.Sum(c => c.TotalWeight);
            // Gesamtgewicht nur der getragenen Inventare
            double carried = VM.Containers.Where(c => c.IsCarried).Sum(c => c.TotalWeight);

            AllInventoriesWeightLabel.Text = $"{totalAll:N2} stein";
            CarriedInventoriesWeightLabel.Text = $"{carried:N2} stein";
        }

        private async void OnCreateNewContainer(object sender, EventArgs e)
        {
            var result = await DisplayPromptAsync("Neues Inventar", "Name des Containers:", "OK", "Abbrechen", "Neuer Container");
            if (!string.IsNullOrWhiteSpace(result))
            {
                var c = new InventoryContainer { Name = result };
                VM.Containers.Add(c);
                UpdateGlobalBankLabel();
                UpdateTotalWeightsLabel();
            }
        }

        private async void OnContainerSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0) return;
            var container = e.CurrentSelection[0] as InventoryContainer;
            if (container == null) return;
            await Navigation.PushAsync(new InventoryContainerPage(container));
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
