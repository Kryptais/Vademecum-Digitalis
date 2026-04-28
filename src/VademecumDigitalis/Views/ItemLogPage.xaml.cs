using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis
{
    public partial class ItemLogPage : ContentPage
    {
        private InventoryItem _item;
        private InventoryLogService? _logSvc;

        public ItemLogPage(InventoryItem item)
        {
            InitializeComponent();
            _item = item;
            TitleLabel.Text = $"Log: {item.Name}";
            LogCollection.ItemsSource = _item.Log;
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _logSvc = services?.GetService(typeof(InventoryLogService)) as InventoryLogService;
        }

        private async void OnAddLog(object sender, EventArgs e)
        {
            var comment = await DisplayPromptAsync("Log hinzufügen", "Kommentar:", "OK", "Abbrechen", "");
            if (string.IsNullOrWhiteSpace(comment)) return;
            _item.AddLog(comment);
            _logSvc?.Append($"Item log: {_item.Name} - {comment}");
        }
    }
}
