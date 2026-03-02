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
        private string _details = string.Empty;
        private ObservableCollection<InventoryItem> _items = new ObservableCollection<InventoryItem>();

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
                    _items = value;
                    OnPropertyChanged(nameof(Items));
                    RefreshTotals();
                }
            }
        }

        public bool IsCarried
        {
            get => _isCarried;
            set { if (_isCarried != value) { _isCarried = value; OnPropertyChanged(nameof(IsCarried)); OnPropertyChanged(nameof(TotalWeight)); } }
        }

        // Whether coin weight is counted for this container (default true)
        public bool IncludeCoinWeight
        {
            get => _includeCoinWeight;
            set
            {
                if (_includeCoinWeight != value)
                {
                    _includeCoinWeight = value;
                    OnPropertyChanged(nameof(IncludeCoinWeight));
                    RefreshTotals();
                }
            }
        }

        public string Details
        {
            get => _details;
            set { if (_details != value) { _details = value; OnPropertyChanged(nameof(Details)); } }
        }

        public bool IsFixedTreasury { get; set; } = false;

        // TotalWeight respects IncludeCoinWeight
        public double TotalWeight => Items.Sum(i => i.TotalWeight) + (IncludeCoinWeight ? Money.TotalWeight : 0);

        // Total value of all items plus coins in this container (in Silbertaler)
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

        /// <summary>
        /// Fires PropertyChanged for all computed total properties so the UI updates immediately.
        /// Call this from the ViewModel whenever items or money change.
        /// </summary>
        public void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalWeight));
            OnPropertyChanged(nameof(TotalValue));
            OnPropertyChanged(nameof(FormattedTotalValue));
            OnPropertyChanged(nameof(TotalValueAsCurrency));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
