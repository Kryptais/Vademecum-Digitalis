using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace VademecumDigitalis.Models
{
    public class InventoryContainer : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private bool _isCarried = true;
        private bool _includeCoinWeight = true;
        private string _details = string.Empty;
        private ObservableCollection<InventoryItem> _items = new ObservableCollection<InventoryItem>();

        // Cached computed values – avoid LINQ on every property access
        private double _cachedTotalWeight;
        private double _cachedTotalValue;
        private CancellationTokenSource? _refreshCts;

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
                    RefreshTotalsImmediate();
                }
            }
        }

        public bool IsCarried
        {
            get => _isCarried;
            set
            {
                if (_isCarried != value)
                {
                    _isCarried = value;
                    OnPropertyChanged(nameof(IsCarried));
                }
            }
        }

        public bool IncludeCoinWeight
        {
            get => _includeCoinWeight;
            set
            {
                if (_includeCoinWeight != value)
                {
                    _includeCoinWeight = value;
                    OnPropertyChanged(nameof(IncludeCoinWeight));
                    RefreshTotalsImmediate();
                }
            }
        }

        public string Details
        {
            get => _details;
            set { if (_details != value) { _details = value; OnPropertyChanged(nameof(Details)); } }
        }

        public bool IsFixedTreasury { get; set; } = false;

        /// <summary>
        /// Cached total weight – updated via RefreshTotals, not recalculated on every access.
        /// </summary>
        public double TotalWeight => _cachedTotalWeight;

        /// <summary>
        /// Cached total value – updated via RefreshTotals, not recalculated on every access.
        /// </summary>
        public double TotalValue => _cachedTotalValue;

        public string FormattedTotalValue => CurrencyAccount.FormatValue(_cachedTotalValue);

        // Stable cached display object for bindings
        private readonly CurrencyAccount _cachedTotalValueDisplay = new CurrencyAccount();
        public CurrencyAccount TotalValueAsCurrency => _cachedTotalValueDisplay;

        /// <summary>
        /// Recalculates cached totals immediately (synchronous).
        /// Use for initial load or after bulk operations.
        /// </summary>
        public void RefreshTotalsImmediate()
        {
            RecalculateInternal();
            NotifyTotalProperties();
        }

        /// <summary>
        /// Schedules a debounced recalculation (50ms).
        /// Use when reacting to rapid input changes (e.g. typing money values).
        /// </summary>
        public void RefreshTotals()
        {
            _refreshCts?.Cancel();
            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            // Use a very short debounce – just enough to batch multiple property changes
            // from a single logical edit (e.g. CurrencyAccount fires Dukaten + TotalWeight + TotalValueInSilver)
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(50, token);
                    if (token.IsCancellationRequested) return;

                    // Calculate on background
                    RecalculateInternal();

                    // Push notification to UI thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!token.IsCancellationRequested)
                            NotifyTotalProperties();
                    });
                }
                catch (TaskCanceledException) { /* expected */ }
            });
        }

        private void RecalculateInternal()
        {
            double itemWeight = 0;
            double itemValue = 0;
            // Snapshot the items to avoid collection-modified issues on background thread
            var items = _items;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                itemWeight += item.TotalWeight;
                itemValue += item.TotalValue;
            }

            double coinWeight = IncludeCoinWeight ? Money.TotalWeight : 0;
            _cachedTotalWeight = itemWeight + coinWeight;
            _cachedTotalValue = itemValue + Money.TotalValueInSilver;

            // Update display currency in-place
            var parts = CurrencyAccount.CalculateParts(_cachedTotalValue);
            _cachedTotalValueDisplay.UpdateFrom(parts.dukaten, parts.silbertaler, parts.heller, parts.kreuzer);
        }

        private void NotifyTotalProperties()
        {
            OnPropertyChanged(nameof(TotalWeight));
            OnPropertyChanged(nameof(TotalValue));
            OnPropertyChanged(nameof(FormattedTotalValue));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
