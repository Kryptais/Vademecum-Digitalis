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

        // New: Total value of all items in this container (simple sum in Silbertaler, does not include actual money coins?)
        // Let's include items value only.
        public double TotalValue => Items.Sum(i => i.TotalValue) + Money.TotalValueInSilver;

        public string FormattedTotalValue => CurrencyAccount.FormatValue(TotalValue);

        // Backing field für das fixe Anzeige-Objekt
        private readonly CurrencyAccount _cachedTotalValueDisplay = new CurrencyAccount();
        
        // Gibt immer das GLEICHE Objekt zurück, damit Bindings stabil bleiben.
        public CurrencyAccount TotalValueAsCurrency 
        {
            get
            {
                UpdateTotalCurrencyDisplay();
                return _cachedTotalValueDisplay;
            }
        }

        private void UpdateTotalCurrencyDisplay()
        {
            // Berechne die Werte basierend auf dem aktuellen TotalValue
            var parts = CurrencyAccount.CalculateParts(TotalValue);
            
            // Setze die Werte direkt auf dem bestehenden Objekt -> feuert PropertyChanged nur für die Zahlen
            _cachedTotalValueDisplay.Dukaten = parts.dukaten;
            _cachedTotalValueDisplay.Silbertaler = parts.silbertaler;
            _cachedTotalValueDisplay.Heller = parts.heller;
            _cachedTotalValueDisplay.Kreuzer = parts.kreuzer;
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
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            
            // If TotalValue changed, update dependent properties
            if (name == nameof(TotalValue))
            {
                // Aktualisiere das Anzeige-Objekt sofort
                UpdateTotalCurrencyDisplay();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedTotalValue)));
                // Hinweis: Da TotalValueAsCurrency immerDasselbe Objekt liefert, 
                // ist ein PropertyChanged hier streng genommen für das "Objekt" nicht nötig, 
                // aber nützlich falls jemand auf das Property selbst lauscht.
                // Wichtiger ist, dass _cachedTotalValueDisplay SEINE PropertyChanged events feuert (macht es automatisch).
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalValueAsCurrency)));
            }
        }
    }
}
