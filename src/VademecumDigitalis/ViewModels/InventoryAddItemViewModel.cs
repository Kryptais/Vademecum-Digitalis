using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.ViewModels
{
    // Wir nutzen QueryProperty, in Zukunft könnten wir das Item über Shell-Navigation übergeben.
    // Für die Modal-Navigation nutzen wir aktuell eine SetEditingItem Methode oder Property Injection.
    public partial class InventoryAddItemViewModel : ObservableObject
    {
        private InventoryItem? _editingItem;
        
        // Properties für die View
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private string _quantityText = "1";
        
        [ObservableProperty]
        private string _weightText = "0.0";
        
        [ObservableProperty]
        private string _valueText = "0.0";
        
        [ObservableProperty]
        private bool _isConsumable;
        
        [ObservableProperty]
        private string _details = string.Empty;

        // Das Ergebnis, auf das der Aufrufer (z.B. ContainerPage) warten/prüfen kann
        public InventoryItem? ResultItem { get; private set; }

        // Event (oder Action), um dem View mitzuteilen, dass geschlossen werden soll
        public event EventHandler? RequestClose;

        public void SetEditingItem(InventoryItem item)
        {
            _editingItem = item;
            if (item != null)
            {
                Name = item.Name;
                QuantityText = item.Quantity.ToString();
                WeightText = item.WeightPerUnit.ToString();
                ValueText = item.Value.ToString();
                IsConsumable = item.IsConsumable;
                Details = item.Details;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            var name = Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await Application.Current.MainPage.DisplayAlert("Fehler", "Name erforderlich", "OK");
                return;
            }
            
            if (!int.TryParse(QuantityText, out var qty)) qty = 1;
            if (!double.TryParse(WeightText, out var w)) w = 0.0;
            if (!double.TryParse(ValueText, out var v)) v = 0.0;
            
            if (_editingItem != null)
            {
                // Edit existing
                bool changed = _editingItem.Name != name || 
                               _editingItem.Quantity != qty || 
                               Math.Abs(_editingItem.WeightPerUnit - w) > 0.0001 || 
                               _editingItem.Details != (Details ?? string.Empty) ||
                               Math.Abs(_editingItem.Value - v) > 0.0001 || 
                               _editingItem.IsConsumable != IsConsumable;

                _editingItem.Name = name;
                _editingItem.Quantity = qty;
                _editingItem.WeightPerUnit = w;
                _editingItem.Value = v;
                _editingItem.IsConsumable = IsConsumable;
                _editingItem.Details = Details ?? string.Empty;

                if (changed)
                {
                    var comment = await Application.Current.MainPage.DisplayPromptAsync("Kommentar (optional)", "Kommentar für Log:", "OK", "Abbrechen", "");
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        _editingItem.AddLog(comment);
                    }
                }
                ResultItem = _editingItem;
            }
            else
            {
                // Create new
                ResultItem = new InventoryItem
                {
                    Name = name,
                    Quantity = qty,
                    WeightPerUnit = w,
                    Value = v,
                    IsConsumable = IsConsumable,
                    Details = Details ?? string.Empty,
                    AcquiredDate = DateTime.UtcNow
                };
                var comment = await Application.Current.MainPage.DisplayPromptAsync("Kommentar (optional)", "Kommentar für Log:", "OK", "Abbrechen", "");
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    ResultItem.AddLog(comment);
                }
            }
            
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            ResultItem = null;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
