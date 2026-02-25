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

        public InventoryContainer()
        {
            // react on money changes -> update TotalWeight
            Money.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CurrencyAccount.TotalWeight) ||
                    e.PropertyName == nameof(CurrencyAccount.Dukaten) ||
                    e.PropertyName == nameof(CurrencyAccount.Silbertaler) ||
                    e.PropertyName == nameof(CurrencyAccount.Heller) ||
                    e.PropertyName == nameof(CurrencyAccount.Kreuzer))
                {
                    OnPropertyChanged(nameof(TotalWeight));
                }
            };

            // react on items collection changes -> update TotalWeight and subscribe to item property changes
            Items.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalWeight));
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
            };

            // subscribe existing items (if any)
            foreach (var it in Items)
            {
                if (it is INotifyPropertyChanged npc) npc.PropertyChanged += Item_PropertyChanged;
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // item weight changed -> update container weight
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(InventoryItem.TotalWeight))
            {
                OnPropertyChanged(nameof(TotalWeight));
            }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        public CurrencyAccount Money { get; set; } = new CurrencyAccount();

        public ObservableCollection<InventoryItem> Items { get; set; } = new ObservableCollection<InventoryItem>();

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
