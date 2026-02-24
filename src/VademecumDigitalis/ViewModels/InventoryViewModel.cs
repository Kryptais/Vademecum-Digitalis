using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _service = new InventoryService();

        public ObservableCollection<InventoryContainer> Containers { get; } = new ObservableCollection<InventoryContainer>();

        public RelayCommand MoveItemCommand { get; }
        public RelayCommand TransferMoneyCommand { get; }

        public InventoryViewModel()
        {
            MoveItemCommand = new RelayCommand(p => ExecuteMoveItem(p));
            TransferMoneyCommand = new RelayCommand(p => ExecuteTransferMoney(p));
        }

        private void ExecuteMoveItem(object? param)
        {
            // expecting a tuple-like object or a small DTO - keep simple for now
            if (param is not object[] arr || arr.Length < 4) return;
            var from = arr[0] as InventoryContainer;
            var to = arr[1] as InventoryContainer;
            var item = arr[2] as InventoryItem;
            var qty = (int)arr[3];
            if (from == null || to == null || item == null) return;
            _service.MoveItem(from, to, item, qty);
            OnPropertyChanged(nameof(Containers));
        }

        private void ExecuteTransferMoney(object? param)
        {
            if (param is not object[] arr || arr.Length < 6) return;
            var from = arr[0] as InventoryContainer;
            var to = arr[1] as InventoryContainer;
            var duk = (int)arr[2];
            var sil = (int)arr[3];
            var hel = (int)arr[4];
            var kre = (int)arr[5];
            if (from == null || to == null) return;
            _service.TransferMoney(from, to, duk, sil, hel, kre);
            OnPropertyChanged(nameof(Containers));
        }

        public double TotalCarriedWeight => Containers.Where(c => c.IsCarried).Sum(c => c.TotalWeight);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
