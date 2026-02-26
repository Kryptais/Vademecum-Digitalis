using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _service = new InventoryService();
        private readonly PersistenceService _persistence = new PersistenceService();

        public ObservableCollection<InventoryContainer> Containers { get; } = new ObservableCollection<InventoryContainer>();

        public RelayCommand MoveItemCommand { get; }
        public RelayCommand TransferMoneyCommand { get; }
        public RelayCommand SaveDataCommand { get; }

        public InventoryViewModel()
        {
            MoveItemCommand = new RelayCommand(p => ExecuteMoveItem(p));
            TransferMoneyCommand = new RelayCommand(p => ExecuteTransferMoney(p));
            SaveDataCommand = new RelayCommand(async p => await ExecuteSaveData());
            
            // Replaced immediate initialization with loading logic
            // We'll call LoadDataAsync from async context later or constructor wrapper
            
            // Subscribe to save changes
            Containers.CollectionChanged += Containers_CollectionChanged;
        }
        
        // This should be called once after VM creation, e.g. from App.xaml.cs or Page OnAppearing
        public async Task LoadDataAsync()
        {
            var loaded = await _persistence.LoadInventoryAsync();
            Containers.Clear();
            
            if (loaded != null && loaded.Any())
            {
                foreach (var c in loaded)
                {
                    Containers.Add(c);
                    // Re-subscribe to inner changes if needed
                    SubscribeToContainerChanges(c);
                }
            }
            else
            {
                // Create default Treasury if none exists (first run)
                var treasury = new InventoryContainer
                {
                    Name = "Tresor",
                    IsFixedTreasury = true,
                    IsCarried = false,
                    IncludeCoinWeight = true,
                    Details = "Der zentrale Tresor für Ersparnisse."
                };
                Containers.Add(treasury);
                SubscribeToContainerChanges(treasury);
                await SaveDataAsync();
            }
        }
        
        private async Task SaveDataAsync()
        {
             await _persistence.SaveInventoryAsync(Containers);
        }

        private async Task ExecuteSaveData()
        {
            await SaveDataAsync();
            // Optional: You might want to signal completion if the UI needs to show a "Saved" message
            // For now specific UI feedback is handled in the View if needed, or we just rely on it working.
            // If we want a toast, we might need a service for that or use a messenger.
            // But for simple "Button clicked" feedback, the view can handle the click event too.
            // Let's keep the logic here simple.
        }

        private void SubscribeToContainerChanges(InventoryContainer container)
        {
            container.PropertyChanged += async (s, e) => 
            {
                 // Autosave on property changes
                 await SaveDataAsync();
            };
            container.Items.CollectionChanged += async (s, e) =>
            {
                 await SaveDataAsync();
            };
            container.Money.PropertyChanged += async (s, e) =>
            {
                await SaveDataAsync();
            };
            
            // Also need to subscribe to items inside for deep changes?
            // Yes, ideally. For now we listen to container level.
            // If items properties change (Quant, Name), we should attach there too.
            foreach(var item in container.Items)
            {
                item.PropertyChanged += async (s, e) => await SaveDataAsync();
            }
            container.Items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InventoryItem item in e.NewItems) 
                        item.PropertyChanged += async (s1, e1) => await SaveDataAsync();
                }
            };
        }

        private async void Containers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InventoryContainer c in e.NewItems)
                {
                    SubscribeToContainerChanges(c);
                }
            }
            
            await SaveDataAsync();
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
            // Save happens via collection/property changes
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
             // Save happens via collection/property changes
        }

        public double TotalCarriedWeight => Containers.Where(c => c.IsCarried).Sum(c => c.TotalWeight);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
