using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace VademecumDigitalis.Models
{
    public class InventoryContainer : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private bool _isCarried = true;
        private bool _includeCoinWeight = true;
        
        // Backing field for Items property
        private ObservableCollection<InventoryItem> _items = new ObservableCollection<InventoryItem>();

        public InventoryContainer()
        {
            // Initiales Setup für Money
            Money.PropertyChanged += (s, e) => UpdateTotalsFromMoney(e.PropertyName);

            // Initiales Setup für die Standard-Liste
            SubscribeToItems(_items);
        }

        private void UpdateTotalsFromMoney(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            // Relevant für Gewicht?
            if (propertyName == nameof(CurrencyAccount.TotalWeight) ||
                propertyName == nameof(CurrencyAccount.Dukaten) ||
                propertyName == nameof(CurrencyAccount.Silbertaler) ||
                propertyName == nameof(CurrencyAccount.Heller) ||
                propertyName == nameof(CurrencyAccount.Kreuzer))
            {
                OnPropertyChanged(nameof(TotalWeight));
            }

            // Relevant für Wert?
            if (propertyName == nameof(CurrencyAccount.TotalValueInSilver) || 
                propertyName == nameof(CurrencyAccount.Dukaten) ||
                propertyName == nameof(CurrencyAccount.Silbertaler) ||
                propertyName == nameof(CurrencyAccount.Heller) ||
                propertyName == nameof(CurrencyAccount.Kreuzer))
            {
                OnPropertyChanged(nameof(TotalValue));
            }
        }

        private void SubscribeToItems(ObservableCollection<InventoryItem> items)
        {
            // react on items collection changes -> update TotalWeight and subscribe to item property changes
            items.CollectionChanged += Items_CollectionChanged;

            // subscribe existing items (if any)
            foreach (var it in items)
            {
                if (it is INotifyPropertyChanged npc) npc.PropertyChanged += Item_PropertyChanged;
            }
        }

        private void UnsubscribeFromItems(ObservableCollection<InventoryItem> items)
        {
            items.CollectionChanged -= Items_CollectionChanged;
            foreach(var it in items)
            {
                 if (it is INotifyPropertyChanged npc) npc.PropertyChanged -= Item_PropertyChanged;
            }
        }

        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
                OnPropertyChanged(nameof(TotalWeight));
                OnPropertyChanged(nameof(TotalValue)); // Also update TotalValue on add/remove!
                OnPropertyChanged(nameof(FormattedTotalValue)); // Make sure formatted updates
                OnPropertyChanged(nameof(TotalValueAsCurrency)); // Make sure currency parts update

                if (e.NewItems != null)
                {
                    foreach (var it in e.NewItems)
                    {
                        if (it is INotifyPropertyChanged npc) npc.PropertyChanged += Item_PropertyChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (var it in e.OldItems)
                    {
                        if (it is INotifyPropertyChanged npc) npc.PropertyChanged -= Item_PropertyChanged;
                    }
                }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // item weight changed -> update container weight
            if (string.IsNullOrEmpty(e.PropertyName) || 
                e.PropertyName == nameof(InventoryItem.TotalWeight) ||
                e.PropertyName == nameof(InventoryItem.WeightPerUnit) || // NEU: Auch auf WeightPerUnit lauschen
                e.PropertyName == nameof(InventoryItem.Quantity))       // NEU: Auch auf Quantity explizit lauschen
            {
                OnPropertyChanged(nameof(TotalWeight));
            }

            // item value changed -> update container value
            if (string.IsNullOrEmpty(e.PropertyName) || 
                e.PropertyName == nameof(InventoryItem.TotalValue) ||
                e.PropertyName == nameof(InventoryItem.Quantity) || 
                e.PropertyName == nameof(InventoryItem.Value))
            {
                OnPropertyChanged(nameof(TotalValue));
            }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        public CurrencyAccount Money { get; set; } = new CurrencyAccount();

        public ObservableCollection<InventoryItem> Items 
        { 
            get => _items; 
            set
            {
                if (_items != value)
                {
                    if (_items != null) UnsubscribeFromItems(_items);
                    _items = value;
                    if (_items != null) SubscribeToItems(_items);
                    OnPropertyChanged(nameof(Items));
                    // Update totals immediately
                    OnPropertyChanged(nameof(TotalWeight));
                    OnPropertyChanged(nameof(TotalValue));
                }
            }
        }

        public bool IsCarried
        {
            get => _isCarried;
            set { if (_isCarried != value) { _isCarried = value; OnPropertyChanged(nameof(IsCarried)); OnPropertyChanged(nameof(TotalWeight)); } }
        }

        // New: whether coin weight is counted for this container (default true)
        public bool IncludeCoinWeight
        {
            get => _includeCoinWeight;
            set
            {
                if (_includeCoinWeight != value)
                {
                    _includeCoinWeight = value;
                    OnPropertyChanged(nameof(IncludeCoinWeight));
                    OnPropertyChanged(nameof(TotalWeight));
                    OnPropertyChanged(nameof(TotalValue));
                }
            }
        }

        public string Details
        {
            get => _details;
            set { if (_details != value) { _details = value; OnPropertyChanged(nameof(Details)); } }
        }
        private string _details = string.Empty;

        public bool IsFixedTreasury { get; set; } = false;

        // TotalWeight respects IncludeCoinWeight
        public double TotalWeight => Items.Sum(i => i.TotalWeight) + (IncludeCoinWeight ? Money.TotalWeight : 0);

        // New: Total value of all items in this container (simple sum in Silbertaler, does not include actual money coins?)
        // Let's include items value only.
        public double TotalValue => Items.Sum(i => i.TotalValue) + Money.TotalValueInSilver;

        public string FormattedTotalValue => CurrencyAccount.FormatValue(TotalValue);

        public CurrencyAccount TotalValueAsCurrency => CalculateTotalCurrency();

        private CurrencyAccount CalculateTotalCurrency()
        {
            var parts = CurrencyAccount.CalculateParts(TotalValue);
            return new CurrencyAccount 
            { 
                Dukaten = parts.dukaten, 
                Silbertaler = parts.silbertaler, 
                Heller = parts.heller, 
                Kreuzer = parts.kreuzer 
            };
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            
            // If TotalValue changed, update dependent properties
            if (name == nameof(TotalValue))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedTotalValue)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalValueAsCurrency)));
            }
        }
    }
}
