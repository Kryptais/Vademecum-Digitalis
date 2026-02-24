using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;

namespace VademecumDigitalis
{
    public partial class InventoryAddItemPage : ContentPage
    {
        public InventoryItem? ResultItem { get; private set; }
        private InventoryItem? _editingItem;

        public InventoryAddItemPage()
        {
            InitializeComponent();
        }

        public void SetEditingItem(InventoryItem item)
        {
            _editingItem = item;
            NameEntry.Text = item.Name;
            QuantityEntry.Text = item.Quantity.ToString();
            WeightEntry.Text = item.WeightPerUnit.ToString();
            DetailsEditor.Text = item.Details;
        }

        private async void OnCancel(object sender, EventArgs e)
        {
            ResultItem = null;
            await Navigation.PopModalAsync();
        }

        private async void OnOk(object sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Fehler", "Name erforderlich", "OK");
                return;
            }
            if (!int.TryParse(QuantityEntry.Text, out var qty)) qty = 1;
            if (!double.TryParse(WeightEntry.Text, out var w)) w = 0.0;

            if (_editingItem != null)
            {
                // check if anything changed
                var changed = _editingItem.Name != name || _editingItem.Quantity != qty || Math.Abs(_editingItem.WeightPerUnit - w) > 0.0001 || _editingItem.Details != (DetailsEditor.Text ?? string.Empty);
                _editingItem.Name = name;
                _editingItem.Quantity = qty;
                _editingItem.WeightPerUnit = w;
                _editingItem.Details = DetailsEditor.Text ?? string.Empty;
                if (changed)
                {
                    var comment = await DisplayPromptAsync("Kommentar (optional)", "Kommentar für Log:", "OK", "Abbrechen", "");
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        _editingItem.AddLog(comment);
                    }
                }
                ResultItem = _editingItem;
            }
            else
            {
                ResultItem = new InventoryItem
                {
                    Name = name,
                    Quantity = qty,
                    WeightPerUnit = w,
                    Details = DetailsEditor.Text ?? string.Empty,
                    AcquiredDate = DateTime.UtcNow
                };
                var comment = await DisplayPromptAsync("Kommentar (optional)", "Kommentar für Log:", "OK", "Abbrechen", "");
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    ResultItem.AddLog(comment);
                }
            }

            await Navigation.PopModalAsync();
        }
    }
}
