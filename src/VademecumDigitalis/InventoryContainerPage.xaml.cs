using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis
{
    public partial class InventoryContainerPage : ContentPage
    {
        public InventoryContainer Container { get; }

        public InventoryContainerPage(InventoryContainer container)
        {
            InitializeComponent();
            Container = container;
            BindingContext = Container;
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

        private void OnIncreaseItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                item.Quantity += 1;
                var svc = GetLogService();
                svc?.Append($"Item hinzugefügt: {item.Name} (+1). Container: {Container.Name}");
            }
        }

        private void OnDecreaseItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                if (item.Quantity > 0)
                {
                    item.Quantity -= 1;
                }
                var svc = GetLogService();
                svc?.Append($"Item reduziert: {item.Name} (-1). Container: {Container.Name}");
            }
        }

        private void OnConsumeItem(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is InventoryItem item)
            {
                // ask for comment
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var comment = await DisplayPromptAsync("Verbrauch", "Kommentar für Log:", "OK", "Abbrechen", "");
                        item.AddLog($"Verbraucht: {item.Quantity}x {item.Name}. {comment}");
                        var svc = GetLogService();
                        svc?.Append($"Verbraucht: {item.Quantity}x {item.Name} in {Container.Name}. {comment}");
                        item.Quantity = 0;
                });
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

            var duk = await DisplayPromptAsync("Dukaten", "Anzahl Dukaten:", "OK", "Abbrechen", "0");
            int.TryParse(duk, out var d);
            var sil = await DisplayPromptAsync("Silbertaler", "Anzahl Silbertaler:", "OK", "Abbrechen", "0");
            int.TryParse(sil, out var s);
            var hel = await DisplayPromptAsync("Heller", "Anzahl Heller:", "OK", "Abbrechen", "0");
            int.TryParse(hel, out var h);
            var kre = await DisplayPromptAsync("Kreuzer", "Anzahl Kreuzer:", "OK", "Abbrechen", "0");
            int.TryParse(kre, out var k);

            var svc = new VademecumDigitalis.Services.InventoryService();
            try
            {
                svc.TransferMoney(Container, target, d, s, h, k);
                await DisplayAlert("OK", "Transfer durchgeführt.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler", ex.Message, "OK");
            }
        }
    }
}
