using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;

namespace VademecumDigitalis
{
    public partial class CoinDisplayView : ContentView
    {
        public static readonly BindableProperty CurrencyProperty =
            BindableProperty.Create(nameof(Currency), typeof(CurrencyAccount), typeof(CoinDisplayView));

        public CurrencyAccount? Currency
        {
            get => (CurrencyAccount?)GetValue(CurrencyProperty);
            set => SetValue(CurrencyProperty, value);
        }

        public CoinDisplayView()
        {
            InitializeComponent();
        }
    }
}
