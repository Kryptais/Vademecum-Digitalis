using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Messages;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels
{
    // QueryProperty allows us to pass the container when navigating.
    [QueryProperty(nameof(Container), "Container")]
    public partial class InventoryContainerViewModel : ObservableObject
    {
        private readonly InventoryLogService _logService;
        private readonly InventoryViewModel _parentViewModel;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private InventoryContainer _container;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<InventoryItem> FilteredItems { get; } = new();

        public InventoryContainerViewModel(InventoryLogService logService, InventoryViewModel parentViewModel, IDialogService dialogService)
        {
            _logService = logService;
            _parentViewModel = parentViewModel;
            _dialogService = dialogService;
        }

        partial void OnContainerChanged(InventoryContainer value)
        {
            if (value == null) return;

            UpdateFilteredItems();

            // Subscribe to collection changes -> refresh totals and filtered list
            value.Items.CollectionChanged += (s, e) =>
            {
                UpdateFilteredItems();
                value.RefreshTotals();

                if (e.NewItems != null)
                    foreach (InventoryItem item in e.NewItems)
                        item.PropertyChanged += OnItemPropertyChanged;

                if (e.OldItems != null)
                    foreach (InventoryItem item in e.OldItems)
                        item.PropertyChanged -= OnItemPropertyChanged;
            };

            // Subscribe to existing items
            foreach (var item in value.Items)
                item.PropertyChanged += OnItemPropertyChanged;

            // Subscribe to money changes – only react to the coin value properties,
            // not to derived properties (TotalWeight, TotalValueInSilver) to avoid cascading refreshes.
            value.Money.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(CurrencyAccount.Dukaten)
                    or nameof(CurrencyAccount.Silbertaler)
                    or nameof(CurrencyAccount.Heller)
                    or nameof(CurrencyAccount.Kreuzer))
                {
                    value.RefreshTotals();
                }
            };
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Only refresh totals when weight/value-relevant properties change
            if (e.PropertyName is nameof(InventoryItem.TotalWeight)
                or nameof(InventoryItem.TotalValue)
                or nameof(InventoryItem.Quantity)
                or nameof(InventoryItem.WeightPerUnit)
                or nameof(InventoryItem.Value))
            {
                Container?.RefreshTotals();
            }
        }

        partial void OnSearchTextChanged(string value) => UpdateFilteredItems();

        private void UpdateFilteredItems()
        {
            if (Container == null) return;

            IEnumerable<InventoryItem> query = Container.Items;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(i =>
                    i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (i.Details != null && i.Details.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }
            var newList = query.ToList();

            // Diff-based update: avoid a full Clear() to reduce UI flicker
            for (int i = FilteredItems.Count - 1; i >= 0; i--)
            {
                if (!newList.Contains(FilteredItems[i]))
                    FilteredItems.RemoveAt(i);
            }
            for (int i = 0; i < newList.Count; i++)
            {
                if (i < FilteredItems.Count && FilteredItems[i] == newList[i])
                    continue;
                int existing = FilteredItems.IndexOf(newList[i]);
                if (existing >= 0)
                    FilteredItems.Move(existing, i);
                else
                    FilteredItems.Insert(i, newList[i]);
            }
        }

        [RelayCommand]
        private void SortByName()
        {
            var sorted = FilteredItems.OrderBy(i => i.Name).ToList();
            FilteredItems.Clear();
            foreach (var i in sorted) FilteredItems.Add(i);
        }

        [RelayCommand]
        private void SortByQuantity()
        {
            var sorted = FilteredItems.OrderByDescending(i => i.Quantity).ToList();
            FilteredItems.Clear();
            foreach (var i in sorted) FilteredItems.Add(i);
        }

        [RelayCommand]
        private void SortByWeight()
        {
            var sorted = FilteredItems.OrderByDescending(i => i.TotalWeight).ToList();
            FilteredItems.Clear();
            foreach (var i in sorted) FilteredItems.Add(i);
        }

        [RelayCommand]
        private async Task AddItem()
        {
            var page = new InventoryAddItemPage();
            await _dialogService.PushModalAsync(new NavigationPage(page));

            page.Disappearing += (s, ev) =>
            {
                if (page.ResultItem != null)
                    Container.Items.Add(page.ResultItem);
            };
        }

        [RelayCommand]
        private async Task EditItem(InventoryItem item)
        {
            if (item == null) return;
            var page = new InventoryAddItemPage();
            page.SetEditingItem(item);
            await _dialogService.PushModalAsync(new NavigationPage(page));
        }

        [RelayCommand]
        private async Task UseItem(InventoryItem item)
        {
            if (item == null || item.Quantity <= 0)
            {
                await _dialogService.DisplayAlert("Info", "Keine Anzahl vorhanden.", "OK");
                return;
            }

            item.Quantity -= 1;
            item.AddLog($"Item verwendet. Restmenge: {item.Quantity}");
            _logService.Append($"Verwendet: 1x {item.Name} aus {Container.Name}. (Rest: {item.Quantity})");
        }

        [RelayCommand]
        private async Task IncreaseItem(InventoryItem item)
        {
            if (item == null) return;

            string? result = await _dialogService.DisplayPromptAsync(
                "Hinzufügen", $"Wie viel '{item.Name}' hinzufügen?", "OK", "Abbrechen", "1", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int amount) && amount > 0)
            {
                string? comment = await _dialogService.DisplayPromptAsync(
                    "Kommentar (optional)", "Grund für Hinzufügen:", "OK", "Überspringen");

                item.Quantity += amount;
                item.AddLog($"Hinzugefügt: {amount}x. {comment}".Trim());
                _logService.Append($"Item erhöht: {item.Name} (+{amount}) in {Container.Name}. {comment}");
            }
        }

        [RelayCommand]
        private async Task DecreaseItem(InventoryItem item)
        {
            if (item == null) return;
            if (item.Quantity <= 0)
            {
                await _dialogService.DisplayAlert("Fehler", "Keine Anzahl vorhanden.", "OK");
                return;
            }

            string? result = await _dialogService.DisplayPromptAsync(
                "Entfernen/Verbrauchen", $"Wie viel '{item.Name}' entfernen? (Max: {item.Quantity})",
                "OK", "Abbrechen", "1", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int amount) && amount > 0)
            {
                if (amount > item.Quantity)
                {
                    await _dialogService.DisplayAlert("Fehler", $"Nicht genügend Anzahl vorhanden. Maximal {item.Quantity} möglich.", "OK");
                    return;
                }

                string? comment = await _dialogService.DisplayPromptAsync(
                    "Kommentar (optional)", "Grund (z.B. Verbrauch):", "OK", "Überspringen");

                item.Quantity -= amount;
                item.AddLog($"Entfernt/Verbraucht: {amount}x. {comment}".Trim());
                _logService.Append($"Item reduziert: {item.Name} (-{amount}) in {Container.Name}. {comment}");
            }
        }

        [RelayCommand]
        private async Task RemoveItem(InventoryItem item)
        {
            if (item == null) return;
            bool confirm = await _dialogService.DisplayAlert(
                "Bestätigen", $"Soll {item.Name} wirklich entfernt werden?", "Ja", "Nein");
            if (confirm)
            {
                var comment = await _dialogService.DisplayPromptAsync("Entfernen", "Grund / Kommentar:", "OK", "Abbrechen", "");
                _logService.Append($"Entfernt: {item.Quantity}x {item.Name} aus {Container.Name}. {comment}");
                Container.Items.Remove(item);
            }
        }

        [RelayCommand]
        private async Task CopyItem(InventoryItem item)
        {
            if (item == null) return;

            var copy = new InventoryItem
            {
                Name = item.Name + " (Kopie)",
                Quantity = item.Quantity,
                WeightPerUnit = item.WeightPerUnit,
                Details = item.Details,
                AcquiredDate = DateTime.UtcNow,
                Tags = new System.Collections.Generic.List<string>(item.Tags)
            };

            copy.AddLog($"Kopie erstellt von: {item.Name} aus Container {Container.Name}");
            Container.Items.Add(copy);
            _logService.Append($"Item kopiert: {item.Name} -> {copy.Name}. Container: {Container.Name}");

            bool editNow = await _dialogService.DisplayAlert(
                "Kopie erstellt", $"Das Item '{copy.Name}' wurde erstellt. Möchtest du es bearbeiten?", "Ja", "Nein");
            if (editNow)
                await EditItem(copy);
        }

        [RelayCommand]
        private async Task MoveItem(InventoryItem item)
        {
            var choices = _parentViewModel.Containers
                .Where(c => c != Container)
                .Select(c => c.Name)
                .ToList();

            if (choices.Count == 0)
            {
                await _dialogService.DisplayAlert("Hinweis", "Keine anderen Container vorhanden.", "OK");
                return;
            }

            var targetName = await _dialogService.DisplayActionSheet(
                $"'{item.Name}' verschieben nach:", "Abbrechen", null, choices.ToArray());
            if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;

            var targetContainer = _parentViewModel.Containers.FirstOrDefault(c => c.Name == targetName);
            if (targetContainer == null) return;

            Container.Items.Remove(item);
            targetContainer.Items.Add(item);

            item.AddLog($"Verschoben von {Container.Name} nach {targetContainer.Name}");
            _logService.Append($"Item verschoben: {item.Name} von {Container.Name} nach {targetContainer.Name}");

            WeakReferenceMessenger.Default.Send(new ItemMovedMessage(item, Container, targetContainer));

            await _dialogService.DisplayAlert("Erfolg", $"'{item.Name}' wurde nach '{targetContainer.Name}' verschoben.", "OK");
        }

        [RelayCommand]
        private async Task ShowLog(InventoryItem item)
        {
            if (item == null) return;
            var text = string.Join("\n", item.Log);
            await _dialogService.DisplayAlert($"Log: {item.Name}", string.IsNullOrEmpty(text) ? "(keine Einträge)" : text, "OK");
        }

        [RelayCommand]
        private async Task NavigateBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task TransferMoney()
        {
            var choices = _parentViewModel.Containers
                .Where(c => c != Container)
                .Select(c => c.Name)
                .ToList();

            if (choices.Count == 0)
            {
                await _dialogService.DisplayAlert("Hinweis", "Keine anderen Container vorhanden.", "OK");
                return;
            }

            var targetName = await _dialogService.DisplayActionSheet("Ziel-Container wählen", "Abbrechen", null, choices.ToArray());
            if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;

            var target = _parentViewModel.Containers.FirstOrDefault(c => c.Name == targetName);
            if (target == null) return;

            var transferPage = new MoneyTransferPage(Container);
            await _dialogService.PushModalAsync(transferPage);

            transferPage.Disappearing += async (s, args) =>
            {
                if (!transferPage.Confirmed) return;

                int d = transferPage.Dukaten;
                int s1 = transferPage.Silbertaler;
                int h = transferPage.Heller;
                int k = transferPage.Kreuzer;

                if (d > Container.Money.Dukaten || s1 > Container.Money.Silbertaler ||
                    h > Container.Money.Heller || k > Container.Money.Kreuzer)
                {
                    await _dialogService.DisplayAlert("Fehler", "Nicht genügend Münzen vorhanden.", "OK");
                    return;
                }

                if (d == 0 && s1 == 0 && h == 0 && k == 0) return;

                var svc = new InventoryService();
                try
                {
                    svc.TransferMoney(Container, target, d, s1, h, k);
                    WeakReferenceMessenger.Default.Send(new MoneyTransferredMessage(Container, target));
                    await _dialogService.DisplayAlert("OK", "Transfer durchgeführt.", "OK");
                }
                catch (Exception ex)
                {
                    await _dialogService.DisplayAlert("Fehler", ex.Message, "OK");
                }
            };
        }

        [RelayCommand]
        private async Task TransferToTreasury()
        {
            var treasury = _parentViewModel.Containers.FirstOrDefault(c => c.IsFixedTreasury);
            if (treasury == null)
            {
                await _dialogService.DisplayAlert("Fehler", "Kein Tresor gefunden.", "OK");
                return;
            }
            if (treasury == Container)
            {
                await _dialogService.DisplayAlert("Hinweis", "Du befindest dich bereits im Tresor.", "OK");
                return;
            }

            var confirm = await _dialogService.DisplayAlert(
                "Transfer in Tresor", "Soll das gesamte Geld dieses Containers in den Tresor verschoben werden?", "Ja, alles", "Nein");
            if (!confirm) return;

            var svc = new InventoryService();
            svc.TransferMoney(Container, treasury,
                (int)Container.Money.Dukaten, (int)Container.Money.Silbertaler,
                (int)Container.Money.Heller, (int)Container.Money.Kreuzer);

            WeakReferenceMessenger.Default.Send(new MoneyTransferredMessage(Container, treasury));
            await _dialogService.DisplayAlert("Erfolg", "Das Geld wurde in den Tresor verschoben.", "OK");
        }
    }
}
