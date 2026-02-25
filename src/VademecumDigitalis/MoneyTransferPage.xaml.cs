using System;
using Microsoft.Maui.Controls;
using VademecumDigitalis.Models;

namespace VademecumDigitalis
{
    public partial class MoneyTransferPage : ContentPage
    {
        public int Dukaten { get; private set; }
        public int Silbertaler { get; private set; }
        public int Heller { get; private set; }
        public int Kreuzer { get; private set; }

        public bool Confirmed { get; private set; }

        public MoneyTransferPage(InventoryContainer source)
        {
            InitializeComponent();
            BindingContext = source; // Bind to source to show available funds if needed
        }

        private async void OnCancel(object sender, EventArgs e)
        {
            Confirmed = false;
            await Navigation.PopModalAsync();
        }

        private async void OnOk(object sender, EventArgs e)
        {
            int.TryParse(DukatenEntry.Text, out var d);
            int.TryParse(SilbertalerEntry.Text, out var s);
            int.TryParse(HellerEntry.Text, out var h);
            int.TryParse(KreuzerEntry.Text, out var k);

            Dukaten = d;
            Silbertaler = s;
            Heller = h;
            Kreuzer = k;
            
            Confirmed = true;
            await Navigation.PopModalAsync();
        }
    }
}
