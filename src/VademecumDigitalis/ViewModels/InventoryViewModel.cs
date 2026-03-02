using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Threading; // Added for CancellationTokenSource
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
        private CancellationTokenSource? _saveCts; // Added for Debouncing
        private CancellationTokenSource? _recalcCts; // Added for Recalculation Debouncing

        public ObservableCollection<InventoryContainer> Containers { get; } = new ObservableCollection<InventoryContainer>();

        // Gefilterte Liste für die UI
        public ObservableCollection<InventoryContainer> FilteredContainers { get; } = new ObservableCollection<InventoryContainer>();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

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
            // Nur speichern, wenn nicht busy (optional, aber gut gegen Reentrancy wenn manuell getriggert)
             // Wir nutzen hier aber Fire&Forget oft, daher kein harter Lock.
             // PersistenceService sollte File-Locks handhaben.
             try
             {
                 IsBusy = true;
                 await _persistence.SaveInventoryAsync(Containers);
             }
             finally
             {
                 IsBusy = false;
             }
        }

        // Neue Methode für Debounced Saving
        private void RequestDelayedSave()
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    // Wartezeit: 2 Sekunden - genug Zeit um Tippen abzuschließen
                    await Task.Delay(2000, token);
                    if (token.IsCancellationRequested) return;

                    // Speichern auf dem MainThread antriggern (falls Zugriff auf ObservableCollection nötig, was save macht)
                    // SaveDataAsync setzt IsBusy, was UI updated -> MainThread erforderlich
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            await SaveDataAsync();
                        }
                    });
                }
                catch (TaskCanceledException) { /* expected */ }
            });
        }

        private void RequestDelayedRecalculate()
        {
            _recalcCts?.Cancel();
            _recalcCts = new CancellationTokenSource();
            var token = _recalcCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    // 500ms warten um UI zu entlasten beim schnellen Tippen
                    await Task.Delay(500, token);
                    if (token.IsCancellationRequested) return;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!token.IsCancellationRequested)
                            RecalculateTotals();
                    });
                }
                catch (TaskCanceledException) { }
            });
        }

        [RelayCommand]
        private async Task SaveData()
        {
            // Manuelles Speichern bricht laufende Delays ab und speichert sofort
            _saveCts?.Cancel();
            
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                await SaveDataAsync();
                
                // Kurzes Feedback (optional, aber meist hilfreich für den User zu sehen "es ist fertig")
                await Task.Delay(500); 
            }
            finally
            {
                IsBusy = false;
            }
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
            container.PropertyChanged += (s, e) => 
            {
                 RequestDelayedSave();
                 RequestDelayedRecalculate(); 
            };
            
            container.Items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InventoryItem item in e.NewItems) 
                        item.PropertyChanged += (s1, e1) => { RequestDelayedSave(); RequestDelayedRecalculate(); };
                }
                 RequestDelayedSave();
                 RequestDelayedRecalculate();
            };
            
            container.Money.PropertyChanged += (s, e) =>
            {
                RequestDelayedSave();
                RequestDelayedRecalculate();
            };

             foreach(var item in container.Items)
            {
                item.PropertyChanged += (s, e) => { RequestDelayedSave(); RequestDelayedRecalculate(); };
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
            RequestDelayedSave(); // Auch hier verzögern statt sofort
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
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
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
