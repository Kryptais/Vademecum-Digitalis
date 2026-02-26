using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace VademecumDigitalis
{
    public partial class InventoryDashboardPage : ContentPage
    {
        InventoryViewModel VM => BindingContext as InventoryViewModel ?? throw new System.InvalidOperationException("VM not resolved");
        
        public System.Collections.ObjectModel.ObservableCollection<InventoryContainer> FilteredContainers { get; private set; } = new System.Collections.ObjectModel.ObservableCollection<InventoryContainer>();
        private static bool _dataLoaded = false;

        public InventoryDashboardPage()
        {
            InitializeComponent();
            // resolve shared VM from DI via the current Application's MauiContext services
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            BindingContext = services?.GetService(typeof(InventoryViewModel)) as InventoryViewModel;

            UpdateGlobalBankLabel();
            UpdateTotalWeightsLabel();
            UpdateTotalValueLabel();
            UpdateFilteredContainers();

            // subscribe to containers collection changes to update bank and weights
            VM.Containers.CollectionChanged += Containers_CollectionChanged;

            // subscribe existing containers
            foreach (var c in VM.Containers)
            {
                SubscribeContainer(c);
            }
            
            // Trigger LoadDataAsync if first run
            if (!_dataLoaded && BindingContext is InventoryViewModel vm)
            {
                _dataLoaded = true;
                MainThread.BeginInvokeOnMainThread(async () => {
                    await vm.LoadDataAsync();
                    // re-init UI after load
                    UpdateGlobalBankLabel();
                    UpdateTotalWeightsLabel();
                    UpdateTotalValueLabel();
                    UpdateFilteredContainers();
                    foreach (var c in vm.Containers) SubscribeContainer(c);
                });
            }
        }
        
        // Ensure UI is updated when returning to this page
        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateGlobalBankLabel();
            UpdateTotalWeightsLabel();
            UpdateTotalValueLabel();
            // Also refresh list just in case
            UpdateFilteredContainers();
        }

        private void UpdateFilteredContainers(string searchText = "")
        {
            FilteredContainers.Clear();
            if (BindingContext is InventoryViewModel vm)
            {
                var query = vm.Containers.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // Search for containers that have at least one item matching the search text
                    // OR if the container name matches
                    query = query.Where(c => 
                        c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        c.Items.Any(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    );
                }
                foreach (var c in query) FilteredContainers.Add(c);
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredContainers(e.NewTextValue);
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
            UpdateTotalValueLabel();
            UpdateFilteredContainers();
        }

        private void SubscribeContainer(InventoryContainer c)
        {
            if (c == null) return;
            c.PropertyChanged += Container_PropertyChanged;
            // also ensure money changes bubble up — InventoryContainer already does this and raises TotalWeight
            c.Money.PropertyChanged += (s, e) => { UpdateGlobalBankLabel(); UpdateTotalWeightsLabel(); UpdateTotalValueLabel(); };
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
            // value change
            if (e.PropertyName == nameof(InventoryContainer.TotalValue))
            {
                UpdateTotalValueLabel();
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

        void UpdateTotalValueLabel()
        {
            // Gesamtwert aller Inventare
            double totalAll = VM.Containers.Sum(c => c.TotalValue);
            
            var parts = CurrencyAccount.CalculateParts(totalAll);
            
            DukatenTotalLabel.Text = parts.dukaten.ToString();
            DukatenTotalStack.IsVisible = parts.dukaten > 0;
            
            SilbertalerTotalLabel.Text = parts.silbertaler.ToString();
            SilbertalerTotalStack.IsVisible = parts.silbertaler > 0;
            
            HellerTotalLabel.Text = parts.heller.ToString();
            HellerTotalStack.IsVisible = parts.heller > 0;
            
            KreuzerTotalLabel.Text = parts.kreuzer.ToString();
            KreuzerTotalStack.IsVisible = parts.kreuzer > 0;
            
            // hide whole stack if 0? Or show "0 S"?
            if (totalAll == 0)
            {
                SilbertalerTotalLabel.Text = "0";
                SilbertalerTotalStack.IsVisible = true;
                AllInventoriesValueStack.IsVisible = true;
            }
            else
            {
                 AllInventoriesValueStack.IsVisible = true;
            }
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
                UpdateTotalValueLabel();
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

        private async void OnGlobalSearch(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GlobalItemSearchPage());
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (BindingContext is InventoryViewModel vm)
            {
                // Trigger command manually
                if (vm.SaveDataCommand.CanExecute(null))
                {
                    vm.SaveDataCommand.Execute(null);
                }
                
                // Visual feedback animation
                await SaveButton.ScaleTo(0.9, 50, Easing.CubicOut);
                await SaveButton.ScaleTo(1.0, 50, Easing.CubicIn);
                
                // Optional: Show toast or small alert
                // await DisplayAlert("Info", "Daten gespeichert.", "OK");
            }
        }

        private async void OnContainerActionClicked(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryContainer container)
            {
                string editAction = "Container bearbeiten (Name/Details)";
                string deleteAction = "Container löschen";
                string moveAction = "Alles verschieben nach..."; // Only if we delete? Or separate action?
                
                // Prompt user for action
                // Using actionsheet
                
                var options = new System.Collections.Generic.List<string>();
                options.Add(editAction);
                
                if (!container.IsFixedTreasury)
                {
                    options.Add(deleteAction);
                }
                
                var action = await DisplayActionSheet($"Optionen: {container.Name}", "Abbrechen", null, options.ToArray());
                
                if (action == editAction)
                {
                    // Existing edit logic from ContainerPage can be reused or replicated
                    // Or ask specifically what to edit
                    string subAction = await DisplayActionSheet($"Bearbeiten: {container.Name}", "Abbrechen", null, "Name ändern", "Details ändern", "Münzgewicht an/aus");
                    if (subAction == "Name ändern")
                    {
                        string newName = await DisplayPromptAsync("Name", "Neuer Name:", initialValue: container.Name);
                        if (!string.IsNullOrWhiteSpace(newName)) container.Name = newName;
                    }
                    else if (subAction == "Details ändern")
                    {
                        string newDetails = await DisplayPromptAsync("Details", "Details:", initialValue: container.Details);
                       if (newDetails != null) container.Details = newDetails;
                    }
                    else if (subAction == "Münzgewicht an/aus")
                    {
                        container.IncludeCoinWeight = !container.IncludeCoinWeight;
                    }
                }
                else if (action == deleteAction)
                {
                    string delOption = await DisplayActionSheet($"Löschen: {container.Name}", "Abbrechen", "Löschen & Inhalt vernichten", "Löschen & Inhalt verschieben");
                    
                    if (delOption == "Abbrechen" || delOption == null) return;
                    
                    if (delOption == "Löschen & Inhalt verschieben")
                    {
                        // Select target
                        var targets = VM.Containers.Where(c => c != container).Select(c => c.Name).ToArray();
                        if (targets.Length == 0)
                        {
                            await DisplayAlert("Fehler", "Kein Ziel-Container verfügbar.", "OK");
                            return; // Cannot move
                        }
                        
                        string targetName = await DisplayActionSheet("Ziel wählen", "Abbrechen", null, targets);
                        if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;
                        
                        var targetContainer = VM.Containers.FirstOrDefault(c => c.Name == targetName);
                        if (targetContainer != null)
                        {
                            // Move items
                            var itemsToMove = container.Items.ToList();
                            foreach(var item in itemsToMove)
                            {
                                container.Items.Remove(item);
                                targetContainer.Items.Add(item);
                            }
                            

                            // Move money
                            container.Money.TransferTo(targetContainer.Money, container.Money.Dukaten, container.Money.Silbertaler, container.Money.Heller, container.Money.Kreuzer);
                            

                            VM.Containers.Remove(container);
                        }
                    }
                    else if (delOption == "Löschen & Inhalt vernichten")
                    {
                        bool confirm = await DisplayAlert("Sicher?", "Wirklich alles vernichten?", "Ja, weg damit", "Nein");
                        if (confirm)
                        {
                            VM.Containers.Remove(container);
                        }
                    }
                }
            }
        }
    }
}
