using CommunityToolkit.Mvvm.ComponentModel;

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

        public CurrencyAccount()
        {
        }

        public long Dukaten
        {
            get => _dukaten;
            set
            {
                if (SetProperty(ref _dukaten, value))
                    NotifyDependentProperties();
            }
        }

        public long Silbertaler
        {
            get => _silbertaler;
            set
            {
                if (SetProperty(ref _silbertaler, value))
                    NotifyDependentProperties();
            }
        }

        public long Heller
        {
            get => _heller;
            set
            {
                if (SetProperty(ref _heller, value))
                    NotifyDependentProperties();
            }
        }

        public long Kreuzer
        {
            get => _kreuzer;
            set
            {
                if (SetProperty(ref _kreuzer, value))
                    NotifyDependentProperties();
            }
        }

        /// <summary>
        /// Feuert Events für alle abhängigen berechneten Eigenschaften.
        /// Wird sofort aufgerufen – Debouncing gehört in die ViewModel-Schicht (Save/Recalc).
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

        /// <summary>
        /// Setzt die Werte dieses Accounts aus den Teilen – ohne neue Instanz zu erstellen.
        /// Nützlich um existierende gebundene Objekte zu aktualisieren.
        /// </summary>
        public void UpdateFrom(long dukaten, long silbertaler, long heller, long kreuzer)
        {
            // Setze Felder direkt um nur nötige Events zu feuern
            bool changed = false;
            if (_dukaten != dukaten) { _dukaten = dukaten; OnPropertyChanged(nameof(Dukaten)); changed = true; }
            if (_silbertaler != silbertaler) { _silbertaler = silbertaler; OnPropertyChanged(nameof(Silbertaler)); changed = true; }
            if (_heller != heller) { _heller = heller; OnPropertyChanged(nameof(Heller)); changed = true; }
            if (_kreuzer != kreuzer) { _kreuzer = kreuzer; OnPropertyChanged(nameof(Kreuzer)); changed = true; }
            if (changed) NotifyDependentProperties();
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
