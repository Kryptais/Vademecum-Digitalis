using System.ComponentModel;

namespace VademecumDigitalis.Models
{
    public class CurrencyAccount : INotifyPropertyChanged
    {
        private long _dukaten;
        private long _silbertaler;
        private long _heller;
        private long _kreuzer;

        // approximate weight per coin in 'stein' (units used by InventoryItem.TotalWeight).
        // Adjust values if you have real weights.
        private const double WeightPerDukaten = 0.1;
        private const double WeightPerSilbertaler = 0.1;
        private const double WeightPerHeller = 0.1;
        private const double WeightPerKreuzer = 0.1;

        public long Dukaten
        {
            get => _dukaten;
            set
            {
                if (_dukaten != value)
                {
                    _dukaten = value;
                    OnPropertyChanged(nameof(Dukaten));
                    OnPropertyChanged(nameof(TotalWeight));
                }
            }
        }

        public long Silbertaler
        {
            get => _silbertaler;
            set
            {
                if (_silbertaler != value)
                {
                    _silbertaler = value;
                    OnPropertyChanged(nameof(Silbertaler));
                    OnPropertyChanged(nameof(TotalWeight));
                }
            }
        }

        public long Heller
        {
            get => _heller;
            set
            {
                if (_heller != value)
                {
                    _heller = value;
                    OnPropertyChanged(nameof(Heller));
                    OnPropertyChanged(nameof(TotalWeight));
                }
            }
        }

        public long Kreuzer
        {
            get => _kreuzer;
            set
            {
                if (_kreuzer != value)
                {
                    _kreuzer = value;
                    OnPropertyChanged(nameof(Kreuzer));
                    OnPropertyChanged(nameof(TotalWeight));
                }
            }
        }

        public double TotalWeight =>
            Dukaten * WeightPerDukaten +
            Silbertaler * WeightPerSilbertaler +
            Heller * WeightPerHeller +
            Kreuzer * WeightPerKreuzer;

        public void TransferTo(CurrencyAccount target, long dukaten, long silbertaler, long heller, long kreuzer)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            // Reduce from this account
            Dukaten -= dukaten;
            Silbertaler -= silbertaler;
            Heller -= heller;
            Kreuzer -= kreuzer;

            // Add to target account
            target.Dukaten += dukaten;
            target.Silbertaler += silbertaler;
            target.Heller += heller;
            target.Kreuzer += kreuzer;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
