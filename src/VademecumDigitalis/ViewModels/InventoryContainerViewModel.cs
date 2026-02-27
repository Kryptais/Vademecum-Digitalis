using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels
{
    // QueryProperty erlaubt es uns, den Container beim Navigieren zu übergeben.
    // Wir nutzen hier den Key "Container", damit Shell.GoToAsync(.., "Container", myContainer) funktioniert.
    [QueryProperty(nameof(Container), "Container")]
    public partial class InventoryContainerViewModel : ObservableObject
    {
        private readonly InventoryLogService _logService;

        // Das eigentliche Model. [ObservableProperty] generiert automatisch die Property 'Container' 
        // und notificiert bei Änderungen.
        [ObservableProperty]
        private InventoryContainer _container;

        // Für die Suchleiste
        [ObservableProperty]
        private string _searchText = string.Empty;

        // Die gefilterte Liste für die UI
        public ObservableCollection<InventoryItem> FilteredItems { get; } = new();

        public InventoryContainerViewModel(InventoryLogService logService)
        {
            _logService = logService;
        }

        // Wird vom Source Generator aufgerufen, wenn sich der Wert der Property 'Container' ändert
        partial void OnContainerChanged(InventoryContainer value)
        {
            if (value != null)
            {
                // UI Initialisieren
                UpdateFilteredItems();
                
                // Auf Änderungen am Container lauschen
                value.Items.CollectionChanged += (s, e) => UpdateFilteredItems();
                
                // Falls sich am Container was ändert, das die Liste beeinflusst (eher selten, 
                // aber falls Filterung von Container-Props abhängt):
                value.PropertyChanged += (s, e) => 
                {
                   // z.B. wenn sich der Name ändert, müssen wir nichts filtern. 
                   // Aber falls Items sortiert bleiben sollen, könnte man hier reagieren.
                };
            }
        }

        // Wird aufgerufen, wenn sich der Suchtext ändert
        partial void OnSearchTextChanged(string value)
        {
            UpdateFilteredItems();
        }

        private void UpdateFilteredItems()
        {
            if (Container == null) return;

            var items = Container.Items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                items = items.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         (i.Details != null && i.Details.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }

            // Wir wollen die Referenz auf FilteredItems nicht verlieren, aber den Inhalt tauschen
            FilteredItems.Clear();
            foreach (var item in items)
            {
                FilteredItems.Add(item);
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
            // Hier müssten wir eigentlich einen DialogService nutzen, 
            // aber für den ersten Schritt nutzen wir App.Current.MainPage
            var page = new InventoryAddItemPage();
            await Application.Current.MainPage.Navigation.PushModalAsync(new NavigationPage(page));
            
            // Wait for result via page property
            // We use Disappearing event or similar manual check, as PushModalAsync is fire-and-forget regarding result
            page.Disappearing += (s, ev) =>
            {
                if (page.ResultItem != null)
                {
                    Container.Items.Add(page.ResultItem);
                }
            };
        }

        [RelayCommand]
        private async Task EditItem(InventoryItem item)
        {
            if (item == null) return;
            var page = new InventoryAddItemPage();
            page.SetEditingItem(item);
            await Application.Current.MainPage.Navigation.PushModalAsync(new NavigationPage(page));
        }

        [RelayCommand]
        private async Task UseItem(InventoryItem item)
        {
            if (item == null || item.Quantity <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Info", "Keine Anzahl vorhanden.", "OK");
                return;
            }

            item.Quantity -= 1;
            string msg = $"Item verwendet. Restmenge: {item.Quantity}";
            item.AddLog(msg);
            _logService.Append($"Verwendet: 1x {item.Name} aus {Container.Name}. (Rest: {item.Quantity})");
        }

        [RelayCommand]
        private async Task IncreaseItem(InventoryItem item)
        {
            if (item == null) return;
            
            string result = await Application.Current.MainPage.DisplayPromptAsync("Hinzufügen", $"Wie viel '{item.Name}' hinzufügen?", "OK", "Abbrechen", "1", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int amount) && amount > 0)
            {
                 string comment = await Application.Current.MainPage.DisplayPromptAsync("Kommentar (optional)", "Grund für Hinzufügen:", "OK", "Überspringen");
                    
                item.Quantity += amount;
                string logMsg = $"Hinzugefügt: {amount}x. {comment}".Trim();
                item.AddLog(logMsg);
                 _logService.Append($"Item erhöht: {item.Name} (+{amount}) in {Container.Name}. {comment}");
            }
        }

        [RelayCommand]
        private async Task DecreaseItem(InventoryItem item)
        {
            if (item == null) return;
             if (item.Quantity <= 0)
            {
                 await Application.Current.MainPage.DisplayAlert("Fehler", "Keine Anzahl vorhanden.", "OK");
                 return;
            }

            string result = await Application.Current.MainPage.DisplayPromptAsync("Entfernen/Verbrauchen", $"Wie viel '{item.Name}' entfernen? (Max: {item.Quantity})", "OK", "Abbrechen", "1", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int amount) && amount > 0)
            {
                if (amount > item.Quantity)
                {
                    await Application.Current.MainPage.DisplayAlert("Fehler", $"Nicht genügend Anzahl vorhanden. Maximal {item.Quantity} möglich.", "OK");
                    return;
                }

                string comment = await Application.Current.MainPage.DisplayPromptAsync("Kommentar (optional)", "Grund (z.B. Verbrauch):", "OK", "Überspringen");

                item.Quantity -= amount;
                
                string logMsg = $"Entfernt/Verbraucht: {amount}x. {comment}".Trim();
                item.AddLog(logMsg);
                _logService.Append($"Item reduziert: {item.Name} (-{amount}) in {Container.Name}. {comment}");
            }
        }

        [RelayCommand]
        private async Task RemoveItem(InventoryItem item)
        {
            if (item == null) return;
            bool confirm = await Application.Current.MainPage.DisplayAlert("Bestätigen", $"Soll {item.Name} wirklich entfernt werden?", "Ja", "Nein");
            if (confirm)
            {
                 var comment = await Application.Current.MainPage.DisplayPromptAsync("Entfernen", "Grund / Kommentar:", "OK", "Abbrechen", "");
                 _logService.Append($"Entfernt: {item.Quantity}x {item.Name} aus {Container.Name}. {comment}");
                 Container.Items.Remove(item);
            }
        }
        
        [RelayCommand]
        private async Task CopyItem(InventoryItem item)
        {
             if (item == null) return;
             
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
            
            copy.AddLog($"Kopie erstellt von: {item.Name} aus Container {Container.Name}");
            Container.Items.Add(copy);
            _logService.Append($"Item kopiert: {item.Name} -> {copy.Name}. Container: {Container.Name}");

             bool editNow = await Application.Current.MainPage.DisplayAlert("Kopie erstellt", $"Das Item '{copy.Name}' wurde erstellt. Möchtest du es bearbeiten?", "Ja", "Nein");
            if (editNow)
            {
                 await EditItem(copy);
            }
        }

        [RelayCommand]
        private async Task MoveItem(InventoryItem item)
        {
            // Hier brauchen wir Zugriff auf die anderen Container (aus dem Parent VM?)
            // Das ist in MVVM manchmal trickreich ohne Mediator/Messenger.
            // Wir lösen es über den Service Provider oder eine direkte Abhängigkeit, 
            // oder senden eine Message, aber bleiben wir pragmatisch:
            
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var parentVm = services?.GetService(typeof(InventoryViewModel)) as InventoryViewModel;
            if (parentVm == null) return;
            
            var choices = new System.Collections.Generic.List<string>();
            foreach (var c in parentVm.Containers)
            {
                if (c != Container) choices.Add(c.Name);
            }
            
            if (choices.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Hinweis", "Keine anderen Container vorhanden.", "OK");
                return;
            }
            
            var targetName = await Application.Current.MainPage.DisplayActionSheet($"'{item.Name}' verschieben nach:", "Abbrechen", null, choices.ToArray());
            if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;
            
            var targetContainer = parentVm.Containers.FirstOrDefault(c => c.Name == targetName);
            if (targetContainer == null) return;

            Container.Items.Remove(item);
            targetContainer.Items.Add(item);
            
            item.AddLog($"Verschoben von {Container.Name} nach {targetContainer.Name}");
            _logService.Append($"Item verschoben: {item.Name} von {Container.Name} nach {targetContainer.Name}");
            
            await Application.Current.MainPage.DisplayAlert("Erfolg", $"'{item.Name}' wurde nach '{targetContainer.Name}' verschieben.", "OK");
        }

        [RelayCommand]
        private async Task ShowLog(InventoryItem item)
        {
             if (item == null) return;
             var text = string.Join("\n", item.Log);
             await Application.Current.MainPage.DisplayAlert($"Log: {item.Name}", text == string.Empty ? "(keine Einträge)" : text, "OK");
        }

        [RelayCommand]
        private async Task NavigateBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task TransferMoney()
        {
            // Zugriff auf Parent VM nötig für Zielauswahl
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var parentVm = services?.GetService(typeof(InventoryViewModel)) as InventoryViewModel;
            if (parentVm == null) return;

             var choices = new System.Collections.Generic.List<string>();
            foreach (var c in parentVm.Containers)
            {
                if (c != Container) choices.Add(c.Name);
            }
            if (choices.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Hinweis", "Keine anderen Container vorhanden.", "OK");
                return;
            }
            var targetName = await Application.Current.MainPage.DisplayActionSheet("Ziel-Container wählen", "Abbrechen", null, choices.ToArray());
            if (string.IsNullOrWhiteSpace(targetName) || targetName == "Abbrechen") return;
            var target = parentVm.Containers.FirstOrDefault(c => c.Name == targetName);
            if (target == null) return;


             // Open custom money transfer page instead of 4 sequential prompts
            var transferPage = new MoneyTransferPage(Container);
            await Application.Current.MainPage.Navigation.PushModalAsync(transferPage);

            transferPage.Disappearing += async (s, args) =>
            {
                if (transferPage.Confirmed)
                {
                    int d = transferPage.Dukaten;
                    int s1 = transferPage.Silbertaler;
                    int h = transferPage.Heller;
                    int k = transferPage.Kreuzer;

                    // Simple check
                    if (d > Container.Money.Dukaten || s1 > Container.Money.Silbertaler || h > Container.Money.Heller || k > Container.Money.Kreuzer)
                    {
                        // Needs MainThread dispatch? Depends on where callback runs.
                        // Usually Disappearing runs on UI thread.
                        await Application.Current.MainPage.DisplayAlert("Fehler", "Nicht genügend Münzen vorhanden.", "OK");
                        return;
                    }

                    if (d == 0 && s1 == 0 && h == 0 && k == 0) return; 

                    var svc = new VademecumDigitalis.Services.InventoryService();
                    try
                    {
                        svc.TransferMoney(Container, target, d, s1, h, k);
                        await Application.Current.MainPage.DisplayAlert("OK", "Transfer durchgeführt.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("Fehler", ex.Message, "OK");
                    }
                }
            };
        }

        [RelayCommand]
        private async Task TransferToTreasury()
        {
             var services = Application.Current?.Handler?.MauiContext?.Services;
            var parentVm = services?.GetService(typeof(InventoryViewModel)) as InventoryViewModel;
            if (parentVm == null) return;

            var treasury = parentVm.Containers.FirstOrDefault(c => c.IsFixedTreasury);
            if (treasury == null)
            {
                 await Application.Current.MainPage.DisplayAlert("Fehler", "Kein Tresor gefunden.", "OK");
                 return;
            }
            if (treasury == Container)
            {
                 await Application.Current.MainPage.DisplayAlert("Hinweis", "Du befindest dich bereits im Tresor.", "OK");
                 return;
            }

            var confirm = await Application.Current.MainPage.DisplayAlert("Transfer in Tresor", "Soll das gesamte Geld dieses Containers in den Tresor verschoben werden?", "Ja, alles", "Nein");
            if (!confirm) return;

            var svc = new InventoryService();
            // Move all money
            svc.TransferMoney(Container, treasury, (int)Container.Money.Dukaten, (int)Container.Money.Silbertaler, (int)Container.Money.Heller, (int)Container.Money.Kreuzer);
            
            await Application.Current.MainPage.DisplayAlert("Erfolg", "Das Geld wurde in den Tresor verschoben.", "OK");
        }
    }
}
