using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly InventoryService _service;
        private readonly PersistenceService _persistence;

        public ObservableCollection<InventoryContainer> Containers { get; } = new ObservableCollection<InventoryContainer>();

        // Gefilterte Liste für die UI
        public ObservableCollection<InventoryContainer> FilteredContainers { get; } = new ObservableCollection<InventoryContainer>();

        [ObservableProperty]
        private string _searchText = string.Empty;

        // Gesamtwerte für die UI
        [ObservableProperty]
        private double _totalWeight;

        [ObservableProperty]
        private double _carriedWeight;

        [ObservableProperty]
        private CurrencyAccount _totalBank = new();

        [ObservableProperty]
        private CurrencyAccount _formattedTotalValue = new();

        public InventoryViewModel(InventoryService service) 
        {
            _service = service;
            // PersistenceService könnte auch injiziert werden, hier instanziieren wir ihn direkt der Einfachheit halber oder nutzen den aus dem alten Code
            _persistence = new PersistenceService(); 

            Containers.CollectionChanged += Containers_CollectionChanged;
        }

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilteredContainers();
        }

        public async Task LoadDataAsync()
        {
            var loaded = await _persistence.LoadInventoryAsync();
            Containers.Clear();
            
            if (loaded != null && loaded.Any())
            {
                foreach (var c in loaded)
                {
                    Containers.Add(c);
                    SubscribeToContainerChanges(c);
                }
            }
            else
            {
                var treasury = new InventoryContainer
                {
                    Name = "Tresor",
                    IsFixedTreasury = true,
                    IsCarried = false,
                    IncludeCoinWeight = true,
                    Details = "Der zentrale Tresor für Ersparnisse."
                };
                Containers.Add(treasury);
                SubscribeToContainerChanges(treasury);
                await SaveDataAsync();
            }
            RecalculateTotals();
            UpdateFilteredContainers();
        }
        
        private async Task SaveDataAsync()
        {
             await _persistence.SaveInventoryAsync(Containers);
        }

        [RelayCommand]
        private async Task SaveData()
        {
            await SaveDataAsync();
            
            // Optional: Visuelles Feedback via VM? 
            // Hier könnte man eine Property "IsSaving" toggeln, die in der UI einen Spinner zeigt.
        }

        [RelayCommand]
        private async Task CreateNewContainer()
        {
            string result = await Application.Current.MainPage.DisplayPromptAsync("Neues Inventar", "Name des Containers:", "OK", "Abbrechen", "Neuer Container");
            if (!string.IsNullOrWhiteSpace(result))
            {
                var c = new InventoryContainer { Name = result };
                Containers.Add(c); 
            }
        }

        [RelayCommand]
        private async Task ShowContainerOptions(InventoryContainer container)
        {
            if (container == null) return;
            
            string editAction = "Container bearbeiten (Name/Details)";
            string deleteAction = "Container löschen";
            
             var options = new System.Collections.Generic.List<string>();
            options.Add(editAction);
            
            if (!container.IsFixedTreasury)
            {
                options.Add(deleteAction);
            }
            
            var action = await Application.Current.MainPage.DisplayActionSheet($"Optionen: {container.Name}", "Abbrechen", null, options.ToArray());

            if (action == editAction)
            {
                 await EditContainer(container);
            }
            else if (action == deleteAction)
            {
                await DeleteContainer(container);
            }
        }

        private async Task DeleteContainer(InventoryContainer container)
        {
            if (container == null || container.IsFixedTreasury) return;

             string delOption = await Application.Current.MainPage.DisplayActionSheet($"Löschen: {container.Name}", "Abbrechen", "Löschen & Inhalt vernichten", "Löschen & Inhalt verschieben");
                    
            if (delOption == "Abbrechen" || delOption == null) return;
            
            if (delOption == "Löschen & Inhalt verschieben")
            {
                var targets = Containers.Where(c => c != container).Select(c => c.Name).ToArray();
                if (targets.Length == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Fehler", "Kein Ziel-Container verfügbar.", "OK");
                    return;
                }
                
                string targetName = await Application.Current.MainPage.DisplayActionSheet("Ziel wählen", "Abbrechen", null, targets);
                if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;
                
                var targetContainer = Containers.FirstOrDefault(c => c.Name == targetName);
                if (targetContainer != null)
                {
                     var itemsToMove = container.Items.ToList();
                    foreach(var item in itemsToMove)
                    {
                        container.Items.Remove(item);
                        targetContainer.Items.Add(item);
                    }
                    container.Money.TransferTo(targetContainer.Money, container.Money.Dukaten, container.Money.Silbertaler, container.Money.Heller, container.Money.Kreuzer);
                    
                    Containers.Remove(container);
                }
            }
            else if (delOption == "Löschen & Inhalt vernichten")
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Sicher?", "Wirklich alles vernichten?", "Ja, weg damit", "Nein");
                if (confirm)
                {
                    Containers.Remove(container);
                }
            }
        }

        private async Task EditContainer(InventoryContainer container)
        {
            if (container == null) return;

             string subAction = await Application.Current.MainPage.DisplayActionSheet($"Bearbeiten: {container.Name}", "Abbrechen", null, "Name ändern", "Details ändern", "Münzgewicht an/aus");
            if (subAction == "Name ändern")
            {
                string newName = await Application.Current.MainPage.DisplayPromptAsync("Name", "Neuer Name:", initialValue: container.Name);
                if (!string.IsNullOrWhiteSpace(newName)) container.Name = newName;
            }
            else if (subAction == "Details ändern")
            {
                string newDetails = await Application.Current.MainPage.DisplayPromptAsync("Details", "Details:", initialValue: container.Details);
                if (newDetails != null) container.Details = newDetails;
            }
            else if (subAction == "Münzgewicht an/aus")
            {
                container.IncludeCoinWeight = !container.IncludeCoinWeight;
            }
        }

        [RelayCommand]
        private async Task NavigateToContainer(InventoryContainer container)
        {
            if (container == null) return;
            
            // Resolve Page via DI
            var page = Application.Current.Handler.MauiContext.Services.GetService<InventoryContainerPage>();
            var vm = page.BindingContext as InventoryContainerViewModel;
            if (vm != null) vm.Container = container;
            
            await Application.Current.MainPage.Navigation.PushAsync(page);
        }

        [RelayCommand]
        private async Task NavigateToSearch()
        {
            var page = Application.Current.Handler.MauiContext.Services.GetService<GlobalItemSearchPage>();
            await Application.Current.MainPage.Navigation.PushAsync(page);
        }

        private void SubscribeToContainerChanges(InventoryContainer container)
        {
            container.PropertyChanged += async (s, e) => 
            {
                 await SaveDataAsync();
                 RecalculateTotals(); 
            };
            
            container.Items.CollectionChanged += async (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InventoryItem item in e.NewItems) 
                        item.PropertyChanged += async (s1, e1) => { await SaveDataAsync(); RecalculateTotals(); };
                }
                 await SaveDataAsync();
                 RecalculateTotals();
            };
            
            container.Money.PropertyChanged += async (s, e) =>
            {
                await SaveDataAsync();
                RecalculateTotals();
            };

             foreach(var item in container.Items)
            {
                item.PropertyChanged += async (s, e) => { await SaveDataAsync(); RecalculateTotals(); };
            }
        }

        private void UnsubscribeContainer(InventoryContainer container)
        {
             // Optional: Cleanup logic
        }

        private void Containers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InventoryContainer c in e.NewItems)
                {
                    SubscribeToContainerChanges(c);
                }
            }
             if (e.OldItems != null)
            {
                foreach (InventoryContainer c in e.OldItems)
                {
                    UnsubscribeContainer(c);
                }
            }
            
            RecalculateTotals();
            UpdateFilteredContainers();
            SaveDataAsync().FireAndForgetSafeAsync();
        }

        private void RecalculateTotals()
        {
            TotalWeight = Containers.Sum(c => c.TotalWeight);
            CarriedWeight = Containers.Where(c => c.IsCarried).Sum(c => c.TotalWeight);
            
            long d = 0, s = 0, h = 0, k = 0;
            foreach (var c in Containers)
            {
                d += c.Money.Dukaten;
                s += c.Money.Silbertaler;
                h += c.Money.Heller;
                k += c.Money.Kreuzer;
            }
            TotalBank = new CurrencyAccount { Dukaten = d, Silbertaler = s, Heller = h, Kreuzer = k };

            double totalVal = Containers.Sum(c => c.TotalValue);
            var parts = CurrencyAccount.CalculateParts(totalVal);
             FormattedTotalValue = new CurrencyAccount { Dukaten = parts.dukaten, Silbertaler = parts.silbertaler, Heller = parts.heller, Kreuzer = parts.kreuzer };
        }

        private void UpdateFilteredContainers()
        {
            FilteredContainers.Clear();
             var query = Containers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(c => 
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Items.Any(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                );
            }
            foreach (var c in query) FilteredContainers.Add(c);
        }
    }
    
    public static class TaskExtensions
    {
        public static async void FireAndForgetSafeAsync(this Task task)
        {
            try
            {
                await task;
            }
            catch { }
        }
    }
}
