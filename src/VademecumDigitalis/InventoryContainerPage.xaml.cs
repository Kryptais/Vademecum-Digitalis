using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis
{
    public partial class InventoryContainerPage : ContentPage
    {
        public InventoryContainer Container { get; }
        
        // Items to display (either all or filtered)
        public System.Collections.ObjectModel.ObservableCollection<InventoryItem> FilteredItems { get; set; } = new System.Collections.ObjectModel.ObservableCollection<InventoryItem>();

        public InventoryContainerPage(InventoryContainer container)
        {
            InitializeComponent();
            Container = container;
            
            // Set binding context to page for filtered list access, but we need composite
            // Actually, keep BindingContext as Container, but we need access to FilteredItems.
            // Let's change BindingContext to this page and expose Container via property.
            
            // BUT: XAML bindings use things like "Name" (of container).
            // So setting BindingContext = this means we need {Binding Container.Name}.
            
            // Simpler approach: Set ItemsCollectionView.ItemsSource to FilteredItems.
            
            UpdateFilteredItems();
            
            BindingContext = Container;
            ItemsCollectionView.ItemsSource = FilteredItems;
            
            // Subscribe to container items changes to refresh list
            Container.Items.CollectionChanged += (s, e) => UpdateFilteredItems();
            
            // To update UI when item values change (not just list changing)
            Container.PropertyChanged += (s, e) => {
                 if (e.PropertyName == nameof(InventoryContainer.TotalValue) ||
                     e.PropertyName == nameof(InventoryContainer.TotalWeight))
                 {
                     // UpdateFilteredItems(); // not needed if items list is same
                     // Just let bindings work. XAML binds to Container.TotalWeight/TotalValue
                 }
            };
        }

        private void UpdateFilteredItems(string searchText = "")
        {
            FilteredItems.Clear();
            var query = Container.Items.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) || 
                                         (i.Details != null && i.Details.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
            }
            
            // Apply sorting if any (can implement state if needed, for now just default order or by name)
            
            foreach (var item in query)
            {
                FilteredItems.Add(item);
            }
        }

        private void OnSortByName(object sender, EventArgs e)
        {
            var sorted = FilteredItems.OrderBy(i => i.Name).ToList();
            FilteredItems.Clear();
            foreach (var i in sorted) FilteredItems.Add(i);
        }

        private void OnSortByQuantity(object sender, EventArgs e)
        {
            var sorted = FilteredItems.OrderByDescending(i => i.Quantity).ToList();
            FilteredItems.Clear();
            foreach (var i in sorted) FilteredItems.Add(i);
        }

        private void OnSortByWeight(object sender, EventArgs e)
        {
            var sorted = FilteredItems.OrderByDescending(i => i.TotalWeight).ToList();
            FilteredItems.Clear();
            foreach (var i in sorted) FilteredItems.Add(i);
        }

        private async void OnAddItem(object sender, EventArgs e)
        {
            // open modal page for item input
            var page = new InventoryAddItemPage();
            await Navigation.PushModalAsync(new NavigationPage(page));
            page.Disappearing += (s, ev) =>
            {
                if (page.ResultItem != null)
                {
                    Container.Items.Add(page.ResultItem);
                }
            };
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnEditItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                var page = new InventoryAddItemPage();
                page.SetEditingItem(item);
                await Navigation.PushModalAsync(new NavigationPage(page));
                page.Disappearing += (s, ev) =>
                {
                    if (page.ResultItem != null)
                    {
                        // already modified in page
                    }
                };
            }
        }

        private async void OnCopyItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                // Create a copy but with new ID
                var copy = new InventoryItem
                {
                    Name = item.Name + " (Kopie)",
                    Quantity = item.Quantity,
                    WeightPerUnit = item.WeightPerUnit,
                    Details = item.Details,
                    AcquiredDate = DateTime.UtcNow,
                    Tags = new System.Collections.Generic.List<string>(item.Tags)
                };
                
                // Add log entry
                copy.AddLog($"Kopie erstellt von: {item.Name} aus Container {Container.Name}");
                
                Container.Items.Add(copy);
                
                var svc = GetLogService();
                svc?.Append($"Item kopiert: {item.Name} -> {copy.Name}. Container: {Container.Name}");
                
                // Allow user to immediately edit the copy if desired
                bool editNow = await DisplayAlert("Kopie erstellt", $"Das Item '{copy.Name}' wurde erstellt. Möchtest du es bearbeiten?", "Ja", "Nein");
                if (editNow)
                {
                    var page = new InventoryAddItemPage();
                    page.SetEditingItem(copy);
                    await Navigation.PushModalAsync(new NavigationPage(page));
                }
            }
        }

        private async void OnMoveItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                // Find other containers
                var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                var parentVm = services?.GetService(typeof(VademecumDigitalis.ViewModels.InventoryViewModel)) as VademecumDigitalis.ViewModels.InventoryViewModel;
                if (parentVm == null) return;
                
                var choices = new System.Collections.Generic.List<string>();
                foreach (var c in parentVm.Containers)
                {
                    if (c != Container) choices.Add(c.Name);
                }
                
                if (choices.Count == 0)
                {
                    await DisplayAlert("Hinweis", "Keine anderen Container vorhanden.", "OK");
                    return;
                }
                
                var targetName = await DisplayActionSheet($"'{item.Name}' verschieben nach:", "Abbrechen", null, choices.ToArray());
                if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;
                
                var targetContainer = parentVm.Containers.FirstOrDefault(c => c.Name == targetName);
                if (targetContainer == null) return;

                // Move item
                Container.Items.Remove(item);
                targetContainer.Items.Add(item);
                
                // Log the move
                item.AddLog($"Verschoben von {Container.Name} nach {targetContainer.Name}");
                var svc = GetLogService();
                svc?.Append($"Item verschoben: {item.Name} von {Container.Name} nach {targetContainer.Name}");
                
                await DisplayAlert("Erfolg", $"'{item.Name}' wurde nach '{targetContainer.Name}' verschieben.", "OK");
            }
        }

        private async void OnIncreaseItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                string result = await DisplayPromptAsync("Hinzufügen", $"Wie viel '{item.Name}' hinzufügen?", "OK", "Abbrechen", "1", keyboard: Keyboard.Numeric);
                if (int.TryParse(result, out int amount) && amount > 0)
                {
                    string comment = await DisplayPromptAsync("Kommentar (optional)", "Grund für Hinzufügen:", "OK", "Überspringen");
                    
                    item.Quantity += amount;
                    
                    string logMsg = $"Hinzugefügt: {amount}x. {comment}".Trim();
                    item.AddLog(logMsg);
                    var svc = GetLogService();
                    svc?.Append($"Item erhöht: {item.Name} (+{amount}) in {Container.Name}. {comment}");
                }
            }
        }

        private async void OnDecreaseItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                if (item.Quantity <= 0)
                {
                     await DisplayAlert("Fehler", "Keine Anzahl vorhanden.", "OK");
                     return;
                }

                string result = await DisplayPromptAsync("Entfernen/Verbrauchen", $"Wie viel '{item.Name}' entfernen? (Max: {item.Quantity})", "OK", "Abbrechen", "1", keyboard: Keyboard.Numeric);
                if (int.TryParse(result, out int amount) && amount > 0)
                {
                    if (amount > item.Quantity)
                    {
                        await DisplayAlert("Fehler", $"Nicht genügend Anzahl vorhanden. Maximal {item.Quantity} möglich.", "OK");
                        return;
                    }

                    string comment = await DisplayPromptAsync("Kommentar (optional)", "Grund (z.B. Verbrauch):", "OK", "Überspringen");

                    item.Quantity -= amount;
                    
                    string logMsg = $"Entfernt/Verbraucht: {amount}x. {comment}".Trim();
                    item.AddLog(logMsg);
                    var svc = GetLogService();
                    svc?.Append($"Item reduziert: {item.Name} (-{amount}) in {Container.Name}. {comment}");
                }
            }
        }

        private void OnRemoveItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                        var confirmed = await DisplayAlert("Bestätigen", $"Soll {item.Name} wirklich entfernt werden?", "Ja", "Nein");
                        if (!confirmed) return;
                        var comment = await DisplayPromptAsync("Entfernen", "Grund / Kommentar:", "OK", "Abbrechen", "");
                        var svc = GetLogService();
                        svc?.Append($"Entfernt: {item.Quantity}x {item.Name} aus {Container.Name}. {comment}");
                        item.AddLog($"Entfernt: {item.Quantity}x {item.Name}. {comment}");
                        Container.Items.Remove(item);
                });
            }
        }

        private void OnShowItemLog(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                // show a simple display of logs
                var text = string.Join("\n", item.Log);
                MainThread.BeginInvokeOnMainThread(async () => await DisplayAlert($"Log: {item.Name}", text == string.Empty ? "(keine Einträge)" : text, "OK"));
            }
        }

        private InventoryLogService? GetLogService()
        {
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            return services?.GetService(typeof(VademecumDigitalis.Services.InventoryLogService)) as VademecumDigitalis.Services.InventoryLogService;
        }

        private async void OnTransferMoney(object sender, EventArgs e)
        {
            // show a dropdown (ActionSheet) of available target containers
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            var parentVm = services?.GetService(typeof(VademecumDigitalis.ViewModels.InventoryViewModel)) as VademecumDigitalis.ViewModels.InventoryViewModel;
            if (parentVm == null) return;
            var choices = new System.Collections.Generic.List<string>();
            foreach (var c in parentVm.Containers)
            {
                if (c != Container) choices.Add(c.Name);
            }
            if (choices.Count == 0)
            {
                await DisplayAlert("Hinweis", "Keine anderen Container vorhanden.", "OK");
                return;
            }
            var targetName = await DisplayActionSheet("Ziel-Container wählen", "Abbrechen", null, choices.ToArray());
            if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;
            var target = parentVm.Containers.FirstOrDefault(c => c.Name == targetName);
            if (target == null) return;

            // Open custom money transfer page instead of 4 sequential prompts
            var transferPage = new MoneyTransferPage(Container);
            await Navigation.PushModalAsync(transferPage);

            transferPage.Disappearing += async (s, args) =>
            {
                if (transferPage.Confirmed)
                {
                    int d = transferPage.Dukaten;
                    int s1 = transferPage.Silbertaler;
                    int h = transferPage.Heller;
                    int k = transferPage.Kreuzer;

                    // Simple check: do not transfer more than available
                    if (d > Container.Money.Dukaten || s1 > Container.Money.Silbertaler || h > Container.Money.Heller || k > Container.Money.Kreuzer)
                    {
                        MainThread.BeginInvokeOnMainThread(async() => await DisplayAlert("Fehler", "Nicht genügend Münzen vorhanden.", "OK"));
                        return;
                    }

                    if (d == 0 && s1 == 0 && h == 0 && k == 0) return; // nothing to do

                    var svc = new VademecumDigitalis.Services.InventoryService();
                    try
                    {
                        svc.TransferMoney(Container, target, d, s1, h, k);
                        MainThread.BeginInvokeOnMainThread(async() => await DisplayAlert("OK", "Transfer durchgeführt.", "OK"));
                    }
                    catch (Exception ex)
                    {
                        MainThread.BeginInvokeOnMainThread(async() => await DisplayAlert("Fehler", ex.Message, "OK"));
                    }
                }
            };
        }

        private async void OnTransferToTreasury(object sender, EventArgs e)
        {
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            var parentVm = services?.GetService(typeof(VademecumDigitalis.ViewModels.InventoryViewModel)) as VademecumDigitalis.ViewModels.InventoryViewModel;
            if (parentVm == null) return;

            var treasury = parentVm.Containers.FirstOrDefault(c => c.IsFixedTreasury);
            if (treasury == null)
            {
                 await DisplayAlert("Fehler", "Kein Tresor gefunden.", "OK");
                 return;
            }
            if (treasury == Container)
            {
                 await DisplayAlert("Hinweis", "Du befindest dich bereits im Tresor.", "OK");
                 return;
            }

            var confirm = await DisplayAlert("Transfer in Tresor", "Soll das gesamte Geld dieses Containers in den Tresor verschoben werden?", "Ja, alles", "Nein");
            if (!confirm) return;

            var svc = new VademecumDigitalis.Services.InventoryService();
            // Move all money
            svc.TransferMoney(Container, treasury, (int)Container.Money.Dukaten, (int)Container.Money.Silbertaler, (int)Container.Money.Heller, (int)Container.Money.Kreuzer);
            
            await DisplayAlert("Erfolg", "Das Geld wurde in den Tresor verschoben.", "OK");
        }

        private async void OnEditContainer(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet($"Inventar bearbeiten: {Container.Name}", "Abbrechen", null, "Namen ändern", "Details bearbeiten", "Münzgewicht-Einstellung", Container.IsFixedTreasury ? null : "Löschen");
            
            if (action == "Namen ändern")
            {
                string newName = await DisplayPromptAsync("Name", "Neuer Name:", initialValue: Container.Name);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    Container.Name = newName;
                }
            }
            else if (action == "Details bearbeiten")
            {
                string newDetails = await DisplayPromptAsync("Details", "Notizen zum Container:", initialValue: Container.Details);
                if (newDetails != null) Container.Details = newDetails;
            }
            else if (action == "Münzgewicht-Einstellung")
            {
                bool include = await DisplayAlert("Münzgewicht", "Soll das Gewicht der Münzen zum Gesamtgewicht zählen?", "Ja", "Nein");
                Container.IncludeCoinWeight = include;
            }
            else if (action == "Löschen" && !Container.IsFixedTreasury)
            {
                bool del = await DisplayAlert("Löschen", $"Soll der Container '{Container.Name}' wirklich gelöscht werden? Alle Inhalte gehen verloren.", "Ja, löschen", "Abbrechen");
                if (del)
                {
                    var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                    var parentVm = services?.GetService(typeof(VademecumDigitalis.ViewModels.InventoryViewModel)) as VademecumDigitalis.ViewModels.InventoryViewModel;
                    if (parentVm != null)
                    {
                        parentVm.Containers.Remove(Container);
                        await Navigation.PopAsync();
                    }
                }
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredItems(e.NewTextValue);
        }

        private async void OnUseItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                if (item.Quantity <= 0)
                {
                    await DisplayAlert("Info", "Keine Anzahl vorhanden.", "OK");
                    return;
                }

                item.Quantity -= 1;
                item.AddLog($"Item verwendet. Restmenge: {item.Quantity}");
                
                // Visual feedback
                // await DisplayAlert("Verwendet", $"{item.Name} wurde verwendet.", "OK");
                
                var svc = GetLogService();
                svc?.Append($"Verwendet: 1x {item.Name} aus {Container.Name}. (Rest: {item.Quantity})");
            }
        }
    }
}
