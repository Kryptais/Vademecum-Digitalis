using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VademecumDigitalis.Models
{
    public class InventoryItem : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private double _weightPerUnit = 0.0; // in Stein
        private int _quantity = 1;
        private string _details = string.Empty;
        private DateTime _acquiredDate = DateTime.UtcNow;
        private List<string> _tags = new List<string>();
        private System.Collections.ObjectModel.ObservableCollection<InventoryLogEntry> _log = new System.Collections.ObjectModel.ObservableCollection<InventoryLogEntry>();

        public Guid Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); } }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        public double WeightPerUnit
        {
            get => _weightPerUnit;
            set { if (Math.Abs(_weightPerUnit - value) > 0.0001) { _weightPerUnit = value; OnPropertyChanged(nameof(WeightPerUnit)); OnPropertyChanged(nameof(TotalWeight)); } }
        }

        public int Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(TotalWeight)); } }
        }

        public double TotalWeight => WeightPerUnit * Quantity;

        public string Details
        {
            get => _details;
            set { if (_details != value) { _details = value; OnPropertyChanged(nameof(Details)); } }
        }

        public DateTime AcquiredDate
        {
            get => _acquiredDate;
            set { if (_acquiredDate != value) { _acquiredDate = value; OnPropertyChanged(nameof(AcquiredDate)); } }
        }

        public List<string> Tags
        {
            get => _tags;
            set { if (_tags != value) { _tags = value; OnPropertyChanged(nameof(Tags)); } }
        }

        public System.Collections.ObjectModel.ObservableCollection<InventoryLogEntry> Log
        {
            get => _log;
            set { if (_log != value) { _log = value; OnPropertyChanged(nameof(Log)); } }
        }

        public void AddLog(string message)
        {
            Log.Add(new InventoryLogEntry { Message = message, Timestamp = DateTime.UtcNow });
            OnPropertyChanged(nameof(Log));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
