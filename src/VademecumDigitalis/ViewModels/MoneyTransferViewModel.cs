using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.ViewModels
{
    public partial class MoneyTransferViewModel : ObservableObject
    {
        private readonly InventoryContainer _sourceContainer;

        // Eingabefelder
        [ObservableProperty]
        private string _dukatenText = string.Empty;

        [ObservableProperty]
        private string _silbertalerText = string.Empty;
        
        [ObservableProperty]
        private string _hellerText = string.Empty;
        
        [ObservableProperty]
        private string _kreuzerText = string.Empty;

        // Anzeige des Quellcontainers
        public string SourceName => _sourceContainer?.Name ?? "";
        
        // Ergebnis
        public bool Confirmed { get; private set; }
        public int Dukaten { get; private set; }
        public int Silbertaler { get; private set; }
        public int Heller { get; private set; }
        public int Kreuzer { get; private set; }

        public event EventHandler? RequestClose;

        public MoneyTransferViewModel(InventoryContainer source)
        {
            _sourceContainer = source;
        }

        [RelayCommand]
        private void Confirm()
        {
            int.TryParse(DukatenText, out var d);
            int.TryParse(SilbertalerText, out var s);
            int.TryParse(HellerText, out var h);
            int.TryParse(KreuzerText, out var k);

            Dukaten = d;
            Silbertaler = s;
            Heller = h;
            Kreuzer = k;
            
            Confirmed = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            Confirmed = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
