using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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
        private CancellationTokenSource? _saveCts;
        private CancellationTokenSource? _recalcCts;

        public ObservableCollection<InventoryContainer> Containers { get; } = new ObservableCollection<InventoryContainer>();

        public ObservableCollection<InventoryContainer> FilteredContainers { get; } = new ObservableCollection<InventoryContainer>();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private double _totalWeight;

        [ObservableProperty]
        private double _carriedWeight;

        // Stabile Objekte – Bindings bleiben intakt, Werte werden in-place aktualisiert
        public CurrencyAccount TotalBank { get; } = new();
        public CurrencyAccount FormattedTotalValue { get; } = new();

        public InventoryViewModel(InventoryService service, PersistenceService persistence)
        {
            _service = service;
            _persistence = persistence;
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
            try
            {
                await _persistence.SaveInventoryAsync(Containers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving inventory: {ex.Message}");
            }
        }

        private void RequestDelayedSave()
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000, token);
                    if (token.IsCancellationRequested) return;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (!token.IsCancellationRequested)
                            await SaveDataAsync();
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
                    await Task.Delay(200, token);
                    if (token.IsCancellationRequested) return;

                    // Heavy calculation on background thread
                    var snapshot = Containers.ToArray(); // snapshot to avoid collection-modified
                    double totalWeight = 0;
                    double carriedWeight = 0;
                    long bankD = 0, bankS = 0, bankH = 0, bankK = 0;
                    double totalVal = 0;

                    foreach (var c in snapshot)
                    {
                        if (token.IsCancellationRequested) return;

                        double w = c.TotalWeight;
                        totalWeight += w;
                        if (c.IsCarried) carriedWeight += w;

                        bankD += c.Money.Dukaten;
                        bankS += c.Money.Silbertaler;
                        bankH += c.Money.Heller;
                        bankK += c.Money.Kreuzer;

                        totalVal += c.TotalValue;
                    }

                    var valueParts = CurrencyAccount.CalculateParts(totalVal);

                    // Push results to UI thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        TotalWeight = totalWeight;
                        CarriedWeight = carriedWeight;
                        TotalBank.UpdateFrom(bankD, bankS, bankH, bankK);
                        FormattedTotalValue.UpdateFrom(valueParts.dukaten, valueParts.silbertaler, valueParts.heller, valueParts.kreuzer);
                    });
                }
                catch (TaskCanceledException) { }
            });
        }

        /// <summary>
        /// Einzelner Aufruf für beide verzögerten Operationen – reduziert doppelte Timer-Starts.
        /// </summary>
        private void OnDataChanged()
        {
            RequestDelayedSave();
            RequestDelayedRecalculate();
        }

        [RelayCommand]
        private async Task SaveData()
        {
            _saveCts?.Cancel();
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                await SaveDataAsync();
                await Task.Delay(300);
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

            var options = new System.Collections.Generic.List<string> { editAction };
            if (!container.IsFixedTreasury)
                options.Add(deleteAction);

            var action = await Application.Current.MainPage.DisplayActionSheet($"Optionen: {container.Name}", "Abbrechen", null, options.ToArray());

            if (action == editAction)
                await EditContainer(container);
            else if (action == deleteAction)
                await DeleteContainer(container);
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
                    foreach (var item in itemsToMove)
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
                    Containers.Remove(container);
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
            // Container-Properties (Name, IsCarried, IncludeCoinWeight, etc.)
            container.PropertyChanged += OnContainerPropertyChanged;

            // Items collection
            container.Items.CollectionChanged += OnItemsCollectionChanged;

            // Money
            container.Money.PropertyChanged += OnMoneyPropertyChanged;

            // Existing items
            foreach (var item in container.Items)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        private void UnsubscribeContainer(InventoryContainer container)
        {
            container.PropertyChanged -= OnContainerPropertyChanged;
            container.Items.CollectionChanged -= OnItemsCollectionChanged;
            container.Money.PropertyChanged -= OnMoneyPropertyChanged;
            foreach (var item in container.Items)
                item.PropertyChanged -= OnItemPropertyChanged;
        }

        private void OnContainerPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only trigger recalc/save for properties that affect global totals or need persisting
            if (e.PropertyName is nameof(InventoryContainer.TotalWeight)
                or nameof(InventoryContainer.TotalValue)
                or nameof(InventoryContainer.IsCarried)
                or nameof(InventoryContainer.Name)
                or nameof(InventoryContainer.Details)
                or nameof(InventoryContainer.IncludeCoinWeight))
            {
                OnDataChanged();
            }
        }

        private void OnMoneyPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only react to actual coin changes, not derived (TotalWeight etc.)
            if (e.PropertyName is nameof(CurrencyAccount.Dukaten)
                or nameof(CurrencyAccount.Silbertaler)
                or nameof(CurrencyAccount.Heller)
                or nameof(CurrencyAccount.Kreuzer))
            {
                OnDataChanged();
            }
        }

        private void OnItemPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only react to value/weight/quantity changes, not Name, Details, etc.
            if (e.PropertyName is nameof(InventoryItem.TotalWeight)
                or nameof(InventoryItem.TotalValue)
                or nameof(InventoryItem.Quantity)
                or nameof(InventoryItem.WeightPerUnit)
                or nameof(InventoryItem.Value)
                or nameof(InventoryItem.Name)
                or nameof(InventoryItem.Details)
                or nameof(InventoryItem.IsConsumable))
            {
                OnDataChanged();
            }
        }

        private void OnItemsCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (InventoryItem item in e.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;

            if (e.OldItems != null)
                foreach (InventoryItem item in e.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;

            OnDataChanged();
        }

        private void Containers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (InventoryContainer c in e.NewItems)
                    SubscribeToContainerChanges(c);

            if (e.OldItems != null)
                foreach (InventoryContainer c in e.OldItems)
                    UnsubscribeContainer(c);

            RecalculateTotals();
            UpdateFilteredContainers();
            RequestDelayedSave();
        }

        private void RecalculateTotals()
        {
            // Synchronous version for initial load and collection changes
            double totalWeight = 0;
            double carriedWeight = 0;
            long bankD = 0, bankS = 0, bankH = 0, bankK = 0;
            double totalVal = 0;

            foreach (var c in Containers)
            {
                double w = c.TotalWeight;
                totalWeight += w;
                if (c.IsCarried) carriedWeight += w;

                bankD += c.Money.Dukaten;
                bankS += c.Money.Silbertaler;
                bankH += c.Money.Heller;
                bankK += c.Money.Kreuzer;

                totalVal += c.TotalValue;
            }

            TotalWeight = totalWeight;
            CarriedWeight = carriedWeight;
            TotalBank.UpdateFrom(bankD, bankS, bankH, bankK);

            var valueParts = CurrencyAccount.CalculateParts(totalVal);
            FormattedTotalValue.UpdateFrom(valueParts.dukaten, valueParts.silbertaler, valueParts.heller, valueParts.kreuzer);
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
            try { await task; } catch { }
        }
    }
}
