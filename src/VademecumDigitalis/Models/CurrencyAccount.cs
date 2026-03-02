using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Threading;
using System.Timers;

namespace VademecumDigitalis.Models
{
    public class CurrencyAccount : ObservableObject
    {
        // approximate weight per coin in 'stein' (units used by InventoryItem.TotalWeight).
        private const double WeightPerDukaten = 0.1;
        private const double WeightPerSilbertaler = 0.1;
        private const double WeightPerHeller = 0.1;
        private const double WeightPerKreuzer = 0.1;

        private long _dukaten;
        private long _silbertaler;
        private long _heller;
        private long _kreuzer;

        // Timer für Debouncing (ersetzt durch CancellationTokenSource für besseres Threading-Verhalten in UI)
        private CancellationTokenSource? _debounceCts;

        public CurrencyAccount()
        {
        }

        public long Dukaten
        {
            get => _dukaten;
            set
            {
                if (_dukaten == value) return;
                
                if (SetProperty(ref _dukaten, value))
                {
                    OnCoinInputChanged();
                }
            }
        }

        public long Silbertaler
        {
            get => _silbertaler;
            set
            {
                if (_silbertaler == value) return;

                if (SetProperty(ref _silbertaler, value))
                {
                    OnCoinInputChanged();
                }
            }
        }

        public long Heller
        {
            get => _heller;
            set
            {
                if (_heller == value) return;

                if (SetProperty(ref _heller, value))
                {
                    OnCoinInputChanged();
                }
            }
        }

        public long Kreuzer
        {
            get => _kreuzer;
            set
            {
                if (_kreuzer == value) return;

                if (SetProperty(ref _kreuzer, value))
                {
                    OnCoinInputChanged();
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn sich irgendeine Münzanzahl ändert (User-Input).
        /// Startet/Restartet den Timer.
        /// </summary>
        private void OnCoinInputChanged()
        {
            // Vorherigen Timer abbrechen (Debouncing)
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            // Neuer Task
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000, token);
                    if (token.IsCancellationRequested) return;

                    // Events feuern
                    NotifyDependentProperties();
                }
                catch (TaskCanceledException)
                {
                    // Erwartet bei Abbruch/Neustart
                }
            });
        }

        /// <summary>
        /// Feuert Events für alle abhängigen Eigenschaften auf einmal.
        /// </summary>
        private void NotifyDependentProperties()
        {
            OnPropertyChanged(nameof(TotalWeight));
            OnPropertyChanged(nameof(TotalValueInSilver)); 
            OnPropertyChanged(nameof(TotalValueInDukaten));
        }

        public double TotalWeight =>
            Dukaten * WeightPerDukaten +
            Silbertaler * WeightPerSilbertaler +
            Heller * WeightPerHeller +
            Kreuzer * WeightPerKreuzer;
        
        // Approximate value in Silbertaler
        public double TotalValueInSilver => (Dukaten * 10) + Silbertaler + (Heller / 10.0) + (Kreuzer / 100.0);
        
        // Approximate value in Dukaten
        public double TotalValueInDukaten => TotalValueInSilver / 10.0;

        /// <summary>
        /// Konvertiert einen Silbertaler-Wert in einen formatierten String (D S H K).
        /// Beispiel: 12.55 Silbertaler -> "1 D 2 S 5 H 5 K"
        /// </summary>
        public static string FormatValue(double valueInSilver)
        {
            var parts = CalculateParts(valueInSilver);
            var strings = new System.Collections.Generic.List<string>();
            
            if (parts.dukaten > 0) strings.Add($"{parts.dukaten} D");
            if (parts.silbertaler > 0) strings.Add($"{parts.silbertaler} S");
            if (parts.heller > 0) strings.Add($"{parts.heller} H");
            if (parts.kreuzer > 0) strings.Add($"{parts.kreuzer} K");
            
            if (strings.Count == 0) return "0 S";

            return string.Join(" ", strings);
        }

        public static (long dukaten, long silbertaler, long heller, long kreuzer) CalculateParts(double valueInSilver)
        {
            long totalKreuzer = (long)Math.Round(valueInSilver * 100);

            if (totalKreuzer == 0) return (0, 0, 0, 0);

            long dukaten = totalKreuzer / 1000;
            long rest = totalKreuzer % 1000;

            long silbertaler = rest / 100;
            rest = rest % 100;

            long heller = rest / 10;
            long kreuzer = rest % 10;
            
            return (dukaten, silbertaler, heller, kreuzer);
        }

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

    }
}
